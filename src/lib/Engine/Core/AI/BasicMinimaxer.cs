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
    public class BasicMinimaxer : Minimaxer
    {
        public BasicMinimaxer(IMoveEnumerator enumerator, IBoardEvaluator evaluator)
            : base(enumerator, evaluator)
        {
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

            int  depth       = options.TreeEvaluationDepth;
            int  cpuUsage    = options.CpuCoreUsagePercentage;
            int  randomSeed  = options.RandomizationSeed;
            bool randomize   = options.EvaluateCellsRandomly;
            bool parallelize = options.EvaluateMovesInParallel;

            s_logger.Debug($"Looking for best move for turn {turn}, player {playerId}, reserve {reserveId}.");

            var metrics = new MinimaxMetrics(playerId, depth);
            metrics.Start();

            List<Cell>  cells = GetBoardCells(board, randomize, randomSeed);
            List<IMove> moves = m_enumerator.EnumerateMoves(board, turn, playerId, reserveId, reserve, cells);
            EvaluationResult evaluationResult = new EvaluationResult(null, MinValue);

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

            try
            {
                int alpha = MinValue;
                int beta  = MaxValue;

                Parallel.ForEach(moves, parallelOptions, (move) =>
                {
                    var partialMetrics = new MinimaxMetrics(playerId, depth);

                    if (move is StoneMove) { partialMetrics.StoneMoveCount[0]++; }
                    if (move is StackMove) { partialMetrics.StackMoveCount[0]++; }

                    // Each task uses its own bitboard-only copy of the game.
                    IBasicGame gameCopy = new BasicGame(game, true);

                    // Store away the board state for later validation.
                    // Validation is done only in when compiled with DEBUG defined.
                    AnalyzerTaskData taskData = new AnalyzerTaskData();
                    SaveBitBoard(gameCopy.BitBoard, taskData, depth);

                    // Make the move.
                    gameCopy.MakeMove(Player.None, move);

                    // Recursively analyze the move to determine the best outcome from this move.
                    int value = - Analyze(gameCopy, playerId, false, depth-1, -beta, -alpha, cells, partialMetrics,
                                                                                                   taskData, token);

                    // Undo the move.
                    gameCopy.UndoMove(Player.None);

                    // Validate the board matches the state prior to the move.
                    ValidateSavedBitBoard(gameCopy.BitBoard, taskData, depth);

                    // Update the overall result if this result is the best one yet.
                    EvaluationResult currentResult = new EvaluationResult(move, value);
                    UpdateEvaluationResult(ref evaluationResult, currentResult);
                    alpha = Math.Max(alpha, evaluationResult.Value);

                    metrics.Add(partialMetrics);
                });
            }
            catch (OperationCanceledException)
            {
                if (evaluationResult.Move == null && moves.Count > 0)
                {
                    evaluationResult.Move = moves[0];
                }
            }

            metrics.Stop();

            var metricsList = new MinimaxMetrics[] { metrics };
            m_metricsCollection.Add(metricsList);
            MinimaxMetrics.Log(metricsList);

            return evaluationResult.Move;
        }


        private int Analyze(IBasicGame game, int maximizer, bool maximize, int depth, int alpha, int beta,
                                      List<Cell> cells, MinimaxMetrics metrics, AnalyzerTaskData taskData,
                                                                                  CancellationToken token)
        {
            // Evaluate and return if cancelled, or at full search depth, or game has been completed.
            if (token.IsCancellationRequested || depth == 0 || game.Result.WinType != WinType.None)
            {
                return m_evaluator.Evaluate(game, maximizer) * (maximize ? 1 : -1);
            }

            IBoard        board     = game.BitBoard;
            int           turn      = game.ActiveTurn;
            int           playerId  = game.ActivePlayer.Id;
            int           reserveId = game.ActiveReserve;
            PlayerReserve reserve   = game.Reserves[reserveId];

            EvaluationResult bestResult = new EvaluationResult(null, alpha);
            List<IMove> moves = m_enumerator.EnumerateMoves(board, turn, playerId, reserveId, reserve, cells);

            foreach (var move in moves)
            {
                if (move is StoneMove) { metrics.StoneMoveCount[0]++; }
                if (move is StackMove) { metrics.StackMoveCount[0]++; }

                SaveBitBoard(game.BitBoard, taskData, depth);
                game.MakeMove(Player.None, move);

                // Recurse, reversing maximize and decrementing depth.
                int value = - Analyze(game, maximizer, ! maximize, depth-1, -beta, -alpha, cells, metrics, taskData,
                                                                                                              token);

                game.UndoMove(Player.None);
                ValidateSavedBitBoard(game.BitBoard, taskData, depth);

                EvaluationResult currentResult = new EvaluationResult(move, value);
                UpdateEvaluationResult(ref bestResult, currentResult, false);

                if (bestResult.Value >= beta || token.IsCancellationRequested)
                {
                    break;
                }

                alpha = Math.Max(alpha, bestResult.Value);
            }

            return bestResult.Value;
        }
    }
}
