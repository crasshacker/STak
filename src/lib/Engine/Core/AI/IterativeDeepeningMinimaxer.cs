using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using NLog;
using STak.TakEngine;

namespace STak.TakEngine.AI
{
    public class IterativeDeepeningMinimaxer : Minimaxer
    {
        private TranspositionTable m_transpositionTable;


        public IterativeDeepeningMinimaxer(IMoveEnumerator enumerator, IBoardEvaluator evaluator)
            : base(enumerator, evaluator)
        {
            m_transpositionTable = new();
        }


        public override void Initialize()
        {
            base.Initialize();
            m_transpositionTable.Clear();
        }


        public override IMove ChooseMove(IBasicGame game, TakAIOptions options, CancellationToken token)
        {
            if (game.IsCompleted)
            {
                return null;
            }

            Initialize();

            IBoard        board     = game.BitBoard;
            int           turn      = game.ActiveTurn;
            int           playerId  = game.ActivePlayer.Id;
            int           reserveId = game.ActiveReserve;
            PlayerReserve reserve   = game.Reserves[reserveId];

            int  maxDepth         = options.TreeEvaluationDepth;
            int  cpuUsage         = options.CpuCoreUsagePercentage;
            int  randomSeed       = options.RandomizationSeed;
            bool randomize        = options.EvaluateCellsRandomly;
            bool parallelize      = options.EvaluateMovesInParallel;

            s_logger.Debug($"Analyzing move for turn {turn}, player {playerId}, reserve {reserveId}.");

            ParallelOptions parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = parallelize ? -1 : 1, // this might be updated below
                CancellationToken = token
            };

            if (parallelize && cpuUsage != 0)
            {
                if (cpuUsage < 0 || cpuUsage > 100)
                {
                    throw new ArgumentException($"The {nameof(cpuUsage)} value must be between zero and 100.");
                }
                parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount * cpuUsage / 100;
            }

            List<Cell>  cells = GetBoardCells(board, randomize, randomSeed);
            List<IMove> moves = m_enumerator.EnumerateMoves(board, turn, playerId, reserveId, reserve, cells);
            moves.Insert(0, moves[0]); // We'll replace this with the best first move in the loop below.

            var pvPrimary = new Variation(maxDepth);
            var metricsList = new MinimaxMetrics[maxDepth];
            EvaluationResult evaluationResult = new EvaluationResult(null, MinValue);

            try
            {
                for (int depth = 1; depth <= maxDepth; ++depth)
                {
                    int entryCount = m_transpositionTable.Count;
                    m_transpositionTable.AgeAndRemoveDeadEntries();
                    s_logger.Info($"Pruned transposition table: {entryCount} => {m_transpositionTable.Count}.");

                    var pvCurrent = new Variation(maxDepth);

                    metricsList[depth-1] = new MinimaxMetrics(playerId, depth);
                    var metrics = metricsList[depth-1];
                    metrics.Start();
                    var currentResult = ProcessMoves(game, depth, moves, cells, pvPrimary, pvCurrent, parallelOptions,
                                                                                             options, metrics, token);
                    metrics.Stop();

                    if (pvCurrent.Count > 0)
                    {
                        evaluationResult = currentResult;
                        moves[0] = pvCurrent.Results[0].Move;
                        pvPrimary.Set(pvCurrent);
                    }

                    s_logger.Info($"Principal Variation (depth={depth}): {pvCurrent}");
                }
                s_logger.Info($"Principal Variation (final): {pvPrimary}");
            }
            catch (OperationCanceledException)
            {
            }

            m_metricsCollection.Add(metricsList);
            MinimaxMetrics.Log(metricsList);

            var move = evaluationResult.Move ?? moves[0];
            string moveType = (move is StoneMove) ? "StoneMove" : "StackMove";
            s_logger.Info($"AI Player {playerId+1} chooses {moveType} {move} for turn {turn}.");

            return move;
        }


        private EvaluationResult ProcessMoves(IBasicGame game, int depth, List<IMove> moves, List<Cell> cells,
                                    Variation pvPrimary, Variation pvCurrent, ParallelOptions parallelOptions,
                                        TakAIOptions options, MinimaxMetrics metrics, CancellationToken token)
        {
            EvaluationResult bestResult = new(null, MinValue);

            int alpha = MinValue;
            int beta  = MaxValue;

            int maximizer = game.ActivePlayer.Id;
            int maxDepth  = options.TreeEvaluationDepth;

            Parallel.ForEach(moves, parallelOptions, (move) =>
            {
                var partialMetrics = new MinimaxMetrics(maximizer, depth);
                var pv = new Variation(maxDepth);

                if (move is StoneMove) { partialMetrics.StoneMoveCount[0]++; }
                if (move is StackMove) { partialMetrics.StackMoveCount[0]++; }

                // Each task uses its own bitboard-only copy of the game.
                IBasicGame gameCopy = new BasicGame(game, true);

                // Store away the board state for later validation.
                // Validation is done only in when compiled with DEBUG_BITBOARD defined.
                AnalyzerTaskData taskData = new AnalyzerTaskData();
                SaveBitBoard(gameCopy.BitBoard, taskData, depth);

                // Make the move.
                gameCopy.MakeMove(Player.None, move);

                // Recursively analyze the move to determine the best outcome from this move.
                int value = - Analyze(gameCopy, maximizer, false, depth, depth-1, -beta, -alpha, cells, pvPrimary, pv,
                                                                             options, taskData, partialMetrics, token);

                // Undo the move.
                gameCopy.UndoMove(Player.None);

                // Validate the board matches the state prior to the move.
                ValidateSavedBitBoard(gameCopy.BitBoard, taskData, depth);

                EvaluationResult currentResult = new EvaluationResult(move, value);

                lock (m_syncLock)
                {
                    if (currentResult.Value > bestResult.Value)
                    {
                        pvCurrent.Set(currentResult, pv);
                    }
                }

                UpdateEvaluationResult(ref bestResult, currentResult);
                alpha = Math.Max(alpha, bestResult.Value);

                metrics.Add(partialMetrics);
            });

            return bestResult;
        }


        private int Analyze(IBasicGame game, int maximizer, bool maximize, int maxDepth, int depth, int alpha,
                                         int beta, List<Cell> cells, Variation pvPrimary, Variation pvCurrent,
                                                              TakAIOptions options, AnalyzerTaskData taskData,
                                                              MinimaxMetrics metrics, CancellationToken token)
        {
            int depthIndex = maxDepth - (depth+1);

            // Evaluate and return if cancelled, or at full search depth, or game has been completed.
            if (token.IsCancellationRequested || depth == 0 || game.Result.WinType != WinType.None)
            {
                pvCurrent.Clear();
                metrics.EvaluationCount[depthIndex]++;
                return m_evaluator.Evaluate(game, maximizer) * (maximize ? 1 : -1);
            }

            bool searchNullWindow      = options.SearchNullWindow;
            bool useTranspositionTable = options.UseTranspositionTable;

            if (useTranspositionTable)
            {
                metrics.TableSearchCount[depthIndex]++;
                var transposition = m_transpositionTable[game.BitBoard];
                if (transposition != null)
                {
                    metrics.TableFoundCount[depthIndex]++;
                    bool usable = (((transposition.Depth >= depth)
                                && ((transposition.BoundType == BoundType.Exact)
                                 || (transposition.BoundType == BoundType.Lower && transposition.Value >= beta)
                                 || (transposition.BoundType == BoundType.Upper && transposition.Value <= alpha)))
                                 || (transposition.BoundType == BoundType.Exact && (transposition.Value == TakAI.WinValue
                                                                                 || transposition.Value == TakAI.LossValue)));

                    if (usable)
                    {
                        // pvCurrent.Set(transposition.Variation);
                        metrics.TableMatchCount[depthIndex]++;
                        return transposition.Value;
                    }
                };
            }

            // See if we're on the principal variation path.  If we are we'll add the PV move at the
            // depth we're working at to the beginning of the move list, hoping that it is indeed the
            // best move available at this point.  Note that we don't bother to remove this move from
            // elsewhere in the list produced by MoveEnumerator.EnumerateMoves, so it's possible that
            // we'll end up processing the same move twice.  No big deal (the performance hit is tiny).

            bool onPV = true;
            IMove bestMove = null;

            for (int i = 0; i < maxDepth-depth; ++i)
            {
                if (i < pvPrimary.Count && ! pvPrimary.Results[i].Move.Equals(game.ExecutedMoves[^(i+1)]))
                {
                    onPV = false;
                    break;
                }
            }
            if (onPV)
            {
                bestMove = pvPrimary.Results[maxDepth-depth].Move;
            }

            IBoard        board     = game.BitBoard;
            int           turn      = game.ActiveTurn;
            int           playerId  = game.ActivePlayer.Id;
            int           reserveId = game.ActiveReserve;
            PlayerReserve reserve   = game.Reserves[reserveId];

            var moves = m_enumerator.EnumerateMoves(board, turn, playerId, reserveId, reserve, cells, bestMove);
            var bestResult = new EvaluationResult(null, alpha);
            var pv = new Variation(maxDepth);

            bool firstMove   = true;
            bool raisedAlpha = false;

            foreach (IMove move in moves)
            {
                if (move is StoneMove) { metrics.StoneMoveCount[depthIndex]++; }
                if (move is StackMove) { metrics.StackMoveCount[depthIndex]++; }

                SaveBitBoard(game.BitBoard, taskData, depth);
                game.MakeMove(Player.None, move);

                int value = 0;

                if (firstMove || ! searchNullWindow)
                {
                    value = - Analyze(game, maximizer, ! maximize, maxDepth, depth-1, -beta, -alpha, cells, pvPrimary,
                                                                                pv, options, taskData, metrics, token);
                }
                else
                {
                    var pvTemp = pv.Clone();
                    value = - Analyze(game, maximizer, ! maximize, maxDepth, depth-1, -alpha-1, -alpha, cells,
                                                         pvPrimary, pvTemp, options, taskData, metrics, token);

                    if (alpha < value && value < beta)
                    {
                        metrics.NullWindowFailCount[depthIndex]++;
                        // Would it be better/correct to pass -value rather than -alpha here?
                        value = - Analyze(game, maximizer, ! maximize, maxDepth, depth-1, -beta, -alpha, cells,
                                                              pvPrimary, pv, options, taskData, metrics, token);
                    }
                    else
                    {
                        metrics.NullWindowPassCount[depthIndex]++;
                        pv = pvTemp;
                    }
                }

                game.UndoMove(Player.None);
                ValidateSavedBitBoard(game.BitBoard, taskData, depth);

                int oldAlpha = alpha;
                alpha = Math.Max(alpha, value);

                EvaluationResult currentResult = new EvaluationResult(move, value);
                UpdateEvaluationResult(ref bestResult, currentResult, false);

                if (alpha > oldAlpha)
                {
                    raisedAlpha = true;
                    pvCurrent.Set(currentResult, pv);
                }

                if (alpha >= beta || token.IsCancellationRequested)
                {
                    break;
                }

                firstMove = false;
            }

            if (useTranspositionTable)
            {
                if (pvCurrent.Count > 0)
                {
                    metrics.TableStoreCount[depthIndex]++;
                    Variation variation;
                    variation = pvCurrent.Clone();
                    m_transpositionTable[game.BitBoard.Clone()] = new Transposition()
                    {
                        Depth      = depth,
                        Value      = alpha,
                        BoundType  = ! raisedAlpha ? BoundType.Upper
                                   : alpha >= beta ? BoundType.Lower
                                                   : BoundType.Exact,
                        Variation  = variation,
                        TimeToLive = 2
                    };
                }
            }

            return bestResult.Value;
        }
    }
}
