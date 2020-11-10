using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Diagnostics;
using NLog;
using STak.TakEngine;

namespace STak.TakEngine.AI
{
    public class Minimax
    {
        private readonly IBasicGame      m_game;
        private readonly IBoardEvaluator m_evaluator;
        private readonly int             m_maximizingPlayerId;

        public const int MinValue = Int32.MinValue+1;
        public const int MaxValue = Int32.MaxValue-1;


        public Minimax(IBasicGame game, IBoardEvaluator evaluator)
        {
            ArgumentValidator.EnsureNotNull(game, nameof(game));

            m_game                = game;
            m_evaluator           = evaluator;
            m_maximizingPlayerId  = game.ActivePlayer.Id;
        }


        public IMove Analyze(int depth, bool randomize, int randomizationSeed, bool parallelize,
                                            int cpuCoreUsagePercentage, CancellationToken token)
        {
            var analyzer = new StaticMinimaxAnalyzer(m_game, m_evaluator, m_maximizingPlayerId);
            return analyzer.Analyze(depth, randomize, randomizationSeed, parallelize, cpuCoreUsagePercentage, token);
        }


        private abstract class MinimaxAnalyzer
        {
            protected IBasicGame      m_game;
            protected IBoardEvaluator m_evaluator;
            protected int             m_maximizingPlayerId;


            public abstract IMove Analyze(int depth, bool randomize, int randomizationSeed, bool parallelize,
                                                         int cpuCoreUsagePercentage, CancellationToken token);


            protected MinimaxAnalyzer(IBasicGame game, IBoardEvaluator evaluator, int maximizingPlayerId)
            {
                m_game               = game;
                m_evaluator          = evaluator;
                m_maximizingPlayerId = maximizingPlayerId;
            }


            protected static void UpdateEvaluationResult(EvaluationResult bestResult, EvaluationResult currentResult,
                                                                                                bool maximize)
            {
                lock (bestResult)
                {
                    if (maximize && currentResult.Value > bestResult.Value)
                    {
                        bestResult.Value = currentResult.Value;
                        bestResult.Move  = currentResult.Move;
                    }
                    else if (! maximize && currentResult.Value < bestResult.Value)
                    {
                        bestResult.Value = currentResult.Value;
                        bestResult.Move  = currentResult.Move;
                    }
                }
            }


            protected static List<Cell> GetBoardCells(IBoard board, bool randomize, int randomizationSeed)
            {
                if (randomize && randomizationSeed == 0)
                {
                    randomizationSeed = Environment.TickCount;
                }

                var query = from rank in Enumerable.Range(0, board.Size)
                            from file in Enumerable.Range(0, board.Size)
                            select new Cell(file, rank);

                List<Cell> cells = Enumerable.ToList(query);

                if (randomize)
                {
                    cells.Shuffle(new Random(randomizationSeed));
                }

                return cells;
            }


            [Conditional("DEBUG_BITBOARD")]
            protected static void SaveBitBoard(BitBoard bitBoard, AnalyzerTaskData taskData, int depth)
            {
                if (taskData.BitBoards.Count < depth)
                {
                    for (int i = taskData.BitBoards.Count; i <= depth; ++i)
                    {
                        taskData.BitBoards.Add(null);
                    }
                }

                taskData.BitBoards[depth] = bitBoard.Clone();
            }


            [Conditional("DEBUG_BITBOARD")]
            protected static void ValidateSavedBitBoard(BitBoard bitBoard, AnalyzerTaskData taskData, int depth)
            {
                Debug.Assert(bitBoard.Equals(taskData.BitBoards[depth]));
            }


            protected class AnalyzerTaskData
            {
                public List<BitBoard> BitBoards = new List<BitBoard>();

                public AnalyzerTaskData()
                {
                }

                public AnalyzerTaskData(AnalyzerTaskData taskData)
                {
                    BitBoards = new List<BitBoard>(taskData.BitBoards.Select(b => b?.Clone()));
                }
            }


            protected class EvaluationResult
            {
                public IMove Move;
                public int   Value;

                public EvaluationResult(IMove move, int value)
                {
                    Move  = move;
                    Value = value;
                }
            }
        }


        private class StaticMinimaxAnalyzer : MinimaxAnalyzer
        {
            private static readonly NLog.Logger s_logger = LogManager.GetCurrentClassLogger();

            private int m_stoneMoveCount;
            private int m_stackMoveCount;


            public StaticMinimaxAnalyzer(IBasicGame game, IBoardEvaluator evaluator, int maximizingPlayerId)
                : base(game, evaluator, maximizingPlayerId)
            {
            }


            public override IMove Analyze(int depth, bool randomize, int randomizationSeed, bool parallelize,
                                                         int cpuCoreUsagePercentage, CancellationToken token)
            {
                if (m_game.IsCompleted)
                {
                    return null;
                }

                IBoard        board      = m_game.BitBoard;
                int           turn       = m_game.ActiveTurn;
                int           playerId   = m_game.ActivePlayer.Id;
                PlayerReserve reserve    = m_game.Reserves[m_game.ActiveReserve];

                s_logger.Debug($"Analyzing move for turn {turn}, player {playerId}, reserve {m_game.ActiveReserve}.");

                var metrics = new PerformanceMetrics();
                metrics.Start();

                List<Cell>  cells = GetBoardCells(board, randomize, randomizationSeed);
                List<IMove> moves = MoveEnumerator.EnumerateMoves(board, turn, playerId, reserve, cells);
                EvaluationResult evaluationResult = new EvaluationResult(null, MinValue);

                ParallelOptions options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = parallelize ? -1 : 1, // this might be updated below
                    CancellationToken = token
                };

                if (parallelize && cpuCoreUsagePercentage != 0)
                {
                    if (cpuCoreUsagePercentage < 0 || cpuCoreUsagePercentage > 100)
                    {
                        throw new ArgumentException($"The {nameof(cpuCoreUsagePercentage)} value must be "
                                                                                 + "between zero and 100.");
                    }
                    options.MaxDegreeOfParallelism = Environment.ProcessorCount * cpuCoreUsagePercentage / 100;
                }

                try
                {
                    Parallel.ForEach(moves, options, (move) =>
                    {
                        if (move is StoneMove) { Interlocked.Increment(ref m_stoneMoveCount); }
                        if (move is StackMove) { Interlocked.Increment(ref m_stackMoveCount); }

                        // Each task uses its own bitboard-only copy of the game.
                        IBasicGame game = new BasicGame(m_game, true);

                        // Store away the board state for later validation.
                        // Validation is done only in when compiled with DEBUG defined.
                        AnalyzerTaskData taskData = new AnalyzerTaskData();
                        SaveBitBoard(game.BitBoard, taskData, depth);

                        // Make the move.
                        game.MakeMove(Player.None, move);

                        // Recursively analyze the move to determine the best outcome from this move.
                        int value = Analyze(game, false, depth-1, MinValue, MaxValue, cells, taskData, token);
                     // int value = - NegaMax(game, false, depth-1, MinValue, MaxValue, cells, taskData, token);

                        // Undo the move.
                        game.UndoMove(Player.None);

                        // Validate the board matches the state prior to the move.
                        ValidateSavedBitBoard(game.BitBoard, taskData, depth);

                        // Update the overall result if this result is the best one yet.
                        EvaluationResult currentResult = new EvaluationResult(move, value);
                        UpdateEvaluationResult(evaluationResult, currentResult, true);
                    });
                }
                catch (OperationCanceledException)
                {
                    if (evaluationResult.Move == null && moves.Count > 0)
                    {
                        evaluationResult.Move = moves[0];
                    }
                }

                var move = evaluationResult.Move;
                metrics.Stop(move, m_stoneMoveCount, m_stackMoveCount);
                metrics.Log();

                return move;
            }


            private int Analyze(IBasicGame game, bool maximize, int depth, int alpha, int beta, List<Cell> cells,
                                                              AnalyzerTaskData taskData, CancellationToken token)
            {
                // Evaluate and return if cancelled, or at full search depth, or game has been completed.
                if (token.IsCancellationRequested || depth == 0 || game.Result.WinType != WinType.None)
                {
                    return m_evaluator.Evaluate(game, m_maximizingPlayerId);
                }

                IBoard        board    = game.BitBoard;
                int           turn     = game.ActiveTurn;
                int           playerId = game.ActivePlayer.Id;
                PlayerReserve reserve  = game.Reserves[game.ActiveReserve];

                EvaluationResult bestResult = new EvaluationResult(null, 0)
                {
                    Value = maximize ? alpha : beta
                };

                foreach (IMove move in MoveEnumerator.EnumerateMoves(board, turn, playerId, reserve, cells))
                {
                    if (move is StoneMove) { Interlocked.Increment(ref m_stoneMoveCount); }
                    if (move is StackMove) { Interlocked.Increment(ref m_stackMoveCount); }

                    SaveBitBoard(game.BitBoard, taskData, depth);
                    game.MakeMove(Player.None, move);

                    int nextAlpha = maximize ? bestResult.Value : alpha;
                    int nextBeta  = maximize ? beta : bestResult.Value;

                    // Recurse, reversing maximize and decrementing depth.
                    int value = Analyze(game, ! maximize, depth-1, nextAlpha, nextBeta, cells, taskData, token);

                    game.UndoMove(Player.None);
                    ValidateSavedBitBoard(game.BitBoard, taskData, depth);

                    EvaluationResult currentResult = new EvaluationResult(move, value);
                    UpdateEvaluationResult(bestResult, currentResult, maximize);

                    if (maximize) { alpha = Math.Max(alpha, bestResult.Value); }
                    else          { beta  = Math.Min(beta,  bestResult.Value); }

                    if (beta <= alpha || token.IsCancellationRequested)
                    {
                        break;
                    }
                }

                return bestResult.Value;
            }


            private int NegaMax(IBasicGame game, bool maximize, int depth, int alpha, int beta, List<Cell> cells,
                                                              AnalyzerTaskData taskData, CancellationToken token)
            {
                // Evaluate and return if cancelled, or at full search depth, or game has been completed.
                if (token.IsCancellationRequested || depth == 0 || game.Result.WinType != WinType.None)
                {
                    return m_evaluator.Evaluate(game, m_maximizingPlayerId) * (maximize ? 1 : -1);
                }

                IBoard        board    = game.BitBoard;
                int           turn     = game.ActiveTurn;
                int           playerId = game.ActivePlayer.Id;
                PlayerReserve reserve  = game.Reserves[game.ActiveReserve];

                EvaluationResult bestResult = new EvaluationResult(null, alpha);

                foreach (IMove move in MoveEnumerator.EnumerateMoves(board, turn, playerId, reserve, cells))
                {
                    if (move is StoneMove) { Interlocked.Increment(ref m_stoneMoveCount); }
                    if (move is StackMove) { Interlocked.Increment(ref m_stackMoveCount); }

                    SaveBitBoard(game.BitBoard, taskData, depth);
                    game.MakeMove(Player.None, move);

                    // Recurse, reversing maximize and decrementing depth.
                    int value = - NegaMax(game, ! maximize, depth-1, -beta, -alpha, cells, taskData, token);

                    game.UndoMove(Player.None);
                    ValidateSavedBitBoard(game.BitBoard, taskData, depth);

                    EvaluationResult currentResult = new EvaluationResult(move, value);
                    UpdateEvaluationResult(bestResult, currentResult, true);

                    if (bestResult.Value >= beta || token.IsCancellationRequested)
                    {
                        break;
                    }

                    alpha = Math.Max(alpha, bestResult.Value);
                }

                return bestResult.Value;
            }


            private class PerformanceMetrics
            {
                private static int      s_totalResultMovesComputed;
                private static long     s_totalStoneMovesEvaluated;
                private static long     s_totalStackMovesEvaluated;
                private static TimeSpan s_totalEvaluationTime;

                private readonly object    m_syncLock;
                private readonly Stopwatch m_stopwatch;
                private          TimeSpan  m_runTimeSpan;
                private          TimeSpan  m_averageTime;
                private          int       m_stoneMoveCount;
                private          int       m_stackMoveCount;
                private          long      m_averageStoneMovesEvaluated;
                private          long      m_averageStackMovesEvaluated;
                private          long      m_averageMovesEvaluated;
                private          IMove     m_move;


                public PerformanceMetrics()
                {
                    m_stopwatch = new Stopwatch();
                    m_syncLock  = new object();
                }


                public void Start()
                {
                    m_stopwatch.Start();
                }


                public void Stop(IMove move, int stoneMoveCount, int stackMoveCount)
                {
                    m_stopwatch.Stop();

                    m_runTimeSpan    = m_stopwatch.Elapsed;
                    m_stoneMoveCount = stoneMoveCount;
                    m_stackMoveCount = stackMoveCount;
                    m_move           = move;

                    lock (m_syncLock)
                    {
                        s_totalResultMovesComputed++;
                        s_totalStoneMovesEvaluated += m_stoneMoveCount;
                        s_totalStackMovesEvaluated += m_stackMoveCount;
                        s_totalEvaluationTime      += m_runTimeSpan;
                    }

                    m_averageTime = s_totalEvaluationTime / s_totalResultMovesComputed;

                    m_averageStoneMovesEvaluated = (s_totalStoneMovesEvaluated / s_totalResultMovesComputed);
                    m_averageStackMovesEvaluated = (s_totalStackMovesEvaluated / s_totalResultMovesComputed);
                    m_averageMovesEvaluated      = (s_totalStoneMovesEvaluated + s_totalStackMovesEvaluated)
                                                                                / s_totalResultMovesComputed;
                }


                public void Log()
                {
                    s_logger.Info("Minimax move analysis yielded move {0}.  Time taken: {1:00}:{2:00}:{3:00}",
                                              m_move.ToString(), m_runTimeSpan.Minutes, m_runTimeSpan.Seconds,
                                                                                   m_runTimeSpan.Milliseconds);

                    s_logger.Info("Minimax (current turn) moves evaluated: Stone={0:n0}, Stack={1:n0}, All={2:n0}.",
                                           m_stoneMoveCount, m_stackMoveCount, m_stoneMoveCount + m_stackMoveCount);

                    s_logger.Info("Average (since app start) moves evaluated: Stone={0:n0}, Stack={1:n0}, All={2:n0}",
                                 m_averageStoneMovesEvaluated, m_averageStackMovesEvaluated, m_averageMovesEvaluated);

                    s_logger.Info("Total (since app start) moves evaluated: Stone={0:n0}, Stack={1:n0}, All={2:n0}",
                                s_totalStoneMovesEvaluated, s_totalStackMovesEvaluated, s_totalStoneMovesEvaluated +
                                                                                        s_totalStackMovesEvaluated);

                    s_logger.Info("Average (since app start) move evaluations per second: {0:n0}",
                        (s_totalStoneMovesEvaluated + s_totalStackMovesEvaluated) / s_totalEvaluationTime.TotalSeconds);

                    s_logger.Info("Total moves computed: {0:n0}, average computation time: {1:00}:{2:00}:{3:00}",
                                        s_totalResultMovesComputed, m_averageTime.Minutes, m_averageTime.Seconds,
                                                                                      m_averageTime.Milliseconds);
                }
            }
        }
    }


    public static class Randomizer
    {
        public static void Shuffle<T>(this IList<T> list, Random rng)
        {
            ArgumentValidator.EnsureNotNull(list, nameof(list));
            ArgumentValidator.EnsureNotNull(rng, nameof(rng));

            for(var i = 0; i < list.Count; i++)
            {
                list.Swap(i, rng.Next(i, list.Count));
            }
        }


        private static void Swap<T>(this IList<T> list, int i, int j)
        {
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
