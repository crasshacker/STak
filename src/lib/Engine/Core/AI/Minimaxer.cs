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
    public abstract class Minimaxer
    {
        protected static readonly NLog.Logger s_logger = LogManager.GetCurrentClassLogger();

        protected readonly MetricsCollection m_metricsCollection;
        protected readonly IBoardEvaluator   m_evaluator;
        protected readonly IMoveEnumerator   m_enumerator;
        protected          object            m_syncLock;

        public const int MinValue = TakAI.LossValue;
        public const int MaxValue = TakAI.WinValue;


        public Minimaxer(IMoveEnumerator enumerator, IBoardEvaluator evaluator)
        {
            m_evaluator         = evaluator;
            m_enumerator        = enumerator;
            m_metricsCollection = new();
            m_syncLock          = new();
        }


        public abstract IMove ChooseMove(IBasicGame game, TakAIOptions options, CancellationToken token);


        public virtual void Initialize()
        {
            m_metricsCollection.Clear();
        }


        public void LogResults()
        {
            m_metricsCollection.Log();
        }


        protected void UpdateEvaluationResult(ref EvaluationResult bestResult, EvaluationResult currentResult,
                                                                                      bool requireLock = true)
        {
            if (requireLock)
            {
                Monitor.Enter(m_syncLock);
            }

            try
            {
                if (currentResult.Value > bestResult.Value || bestResult.Move == null)
                {
                    bestResult.Value = currentResult.Value;
                    bestResult.Move  = currentResult.Move;
                }
            }
            finally
            {
                if (requireLock)
                {
                    Monitor.Exit(m_syncLock);
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
            public List<BitBoard> BitBoards = new();

            public AnalyzerTaskData()
            {
            }

            public AnalyzerTaskData(AnalyzerTaskData taskData)
            {
                BitBoards = new List<BitBoard>(taskData.BitBoards.Select(b => b?.Clone()));
            }
        }


        protected class MinimaxMetrics
        {
            private readonly object    m_syncLock;
            private readonly int       m_playerId;
            private readonly int       m_maxDepth;
            private readonly Stopwatch m_stopwatch;

            public int[]    StoneMoveCount;
            public int[]    StackMoveCount;
            public int[]    EvaluationCount;
            public int[]    NullWindowPassCount;
            public int[]    NullWindowFailCount;
            public int[]    TableSearchCount;
            public int[]    TableStoreCount;
            public int[]    TableFoundCount;
            public int[]    TableMatchCount;
            public TimeSpan RunTimeSpan;


            public MinimaxMetrics(int playerId, int maxDepth)
            {
                m_playerId  = playerId;
                m_maxDepth  = maxDepth;
                m_stopwatch = new Stopwatch();
                m_syncLock  = new object();

                StoneMoveCount      = new int[maxDepth];
                StackMoveCount      = new int[maxDepth];
                EvaluationCount     = new int[maxDepth];
                NullWindowPassCount = new int[maxDepth];
                NullWindowFailCount = new int[maxDepth];
                TableSearchCount    = new int[maxDepth];
                TableStoreCount     = new int[maxDepth];
                TableFoundCount     = new int[maxDepth];
                TableMatchCount     = new int[maxDepth];
            }


            public void Start()
            {
                m_stopwatch.Start();
            }


            public void Stop()
            {
                m_stopwatch.Stop();
                RunTimeSpan = m_stopwatch.Elapsed;
            }


            public void Add(MinimaxMetrics metrics)
            {
                lock (m_syncLock)
                {
                    for (int i = 0; i < m_maxDepth; ++i)
                    {
                        if (i < metrics.m_maxDepth)
                        {
                            StoneMoveCount[i]      += metrics.StoneMoveCount[i];
                            StackMoveCount[i]      += metrics.StackMoveCount[i];
                            EvaluationCount[i]     += metrics.EvaluationCount[i];
                            NullWindowPassCount[i] += metrics.NullWindowPassCount[i];
                            NullWindowFailCount[i] += metrics.NullWindowFailCount[i];
                            TableSearchCount[i]    += metrics.TableSearchCount[i];
                            TableStoreCount[i]     += metrics.TableStoreCount[i];
                            TableFoundCount[i]     += metrics.TableFoundCount[i];
                            TableMatchCount[i]     += metrics.TableMatchCount[i];
                        }

                        RunTimeSpan += metrics.RunTimeSpan;
                    }
                }
            }


            public static MinimaxMetrics Aggregate(MinimaxMetrics[] metricsList)
            {
                int playerId = metricsList[0].m_playerId;
                int maxDepth = metricsList.Max(m => m.m_maxDepth);

                var aggregatedMetrics = new MinimaxMetrics(playerId, maxDepth);

                foreach (var metrics in metricsList)
                {
                    aggregatedMetrics.Add(metrics);
                }

                return aggregatedMetrics;
            }


            public static void Log(MinimaxMetrics[] metricsList)
            {
                int playerId = metricsList[0].m_playerId;
                int maxDepth = metricsList.Max(m => m.m_maxDepth);

                int maxValue = metricsList.Where(m => m.m_maxDepth == maxDepth).Select(m => m.EvaluationCount.Max()).First();
                int columnSize = String.Format("{0,0}", maxValue).Length + 2;
                var totalTime = TimeSpan.Zero;

                string overallHeader = $"***** Minimax metrics for player {playerId+1}:";

                foreach (var metrics in metricsList)
                {
                    int nameLength = 30;

                    string header = $"Max Depth {metrics.m_maxDepth}:".PadRight(nameLength);
                    string stones = "  Stone Moves Made"              .PadRight(nameLength);
                    string stacks = "  Stack Moves Made"              .PadRight(nameLength);
                    string evals  = "  Boards Evaluated"              .PadRight(nameLength);
                    string nullP  = "  Null Window (Pass)"            .PadRight(nameLength);
                    string nullF  = "  Null Window (Fail)"            .PadRight(nameLength);
                    string store  = "  TT Stores"                     .PadRight(nameLength);
                    string search = "  TT Searches"                   .PadRight(nameLength);
                    string found  = "  TT Matches"                    .PadRight(nameLength);
                    string match  = "  TT Usable"                     .PadRight(nameLength);
                    string time   = "  Execution Time"                .PadRight(nameLength);

                    totalTime += metrics.RunTimeSpan;
                    time += String.Format(@"{0:mm\:ss\.fff}", metrics.RunTimeSpan).PadLeft(columnSize);

                    for (int i = 0; i < metrics.m_maxDepth; ++i)
                    {
                        stones += metrics.StoneMoveCount[i]      .ToString().PadLeft(columnSize);
                        stacks += metrics.StackMoveCount[i]      .ToString().PadLeft(columnSize);
                        evals  += metrics.EvaluationCount[i]     .ToString().PadLeft(columnSize);
                        nullP  += metrics.NullWindowPassCount[i] .ToString().PadLeft(columnSize);
                        nullF  += metrics.NullWindowFailCount[i] .ToString().PadLeft(columnSize);
                        store  += metrics.TableStoreCount[i]     .ToString().PadLeft(columnSize);
                        search += metrics.TableSearchCount[i]    .ToString().PadLeft(columnSize);
                        found  += metrics.TableFoundCount[i]     .ToString().PadLeft(columnSize);
                        match  += metrics.TableMatchCount[i]     .ToString().PadLeft(columnSize);
                    }

                    s_logger.Info(header);
                    s_logger.Info(stones);
                    s_logger.Info(stacks);
                    s_logger.Info(evals);
                    s_logger.Info(nullP);
                    s_logger.Info(nullF);
                    s_logger.Info(store);
                    s_logger.Info(search);
                    s_logger.Info(found);
                    s_logger.Info(match);
                    s_logger.Info(time);
                    s_logger.Info("");
                }

                s_logger.Info(@"Total time required for move selection: {0:mm\:ss\.fff}", totalTime);
            }


            public void Log()
            {
                s_logger.Info("");
                s_logger.Info($"##### Start of Minimax performance summary for player {m_playerId+1} #####");

                for (int i = 0; i < m_maxDepth; ++i)
                {
                    s_logger.Info($"### Depth {i+1} ###");
                    s_logger.Info("Minimax null window pass/fail counts: {0}/{1}", NullWindowPassCount[i],
                                                                                   NullWindowFailCount[i]);

                    s_logger.Info("Minimax transposition table store/search/found/match: {0}/{1}/{2}/{3}",
                                              TableStoreCount[i], TableSearchCount[i], TableFoundCount[i],
                                                                                       TableMatchCount[i]);

                    s_logger.Info("Minimax transposition table found and matched percentages: {0:0.##}/{1:0.##}",
                                                                100.0 * TableFoundCount[i] / TableSearchCount[i],
                                                                100.0 * TableMatchCount[i] / TableSearchCount[i]);

                    s_logger.Info("Minimax moves executed: Stone={0:n0}, Stack={1:n0}, All={2:n0}.",
                                                               StoneMoveCount[i],  StackMoveCount[i],
                                                               StoneMoveCount[i] + StackMoveCount[i]);
                }

                s_logger.Info("### Overall results ###");

                s_logger.Info("Minimax total moves evaluated: {0:n0}", EvaluationCount[m_maxDepth-1]);

                s_logger.Info("Minimax move computation time: {0:00}:{1:00}:{2:00}", RunTimeSpan.Minutes,
                                                           RunTimeSpan.Seconds, RunTimeSpan.Milliseconds);

                s_logger.Info("##### End of Minimax performance summary #####");
                s_logger.Info("");
            }
        }


        protected class MetricsCollection
        {
            private List<MinimaxMetrics[]> m_metricsList;

            public int MoveCount => m_metricsList.Count;

            public MetricsCollection()
            {
                m_metricsList = new();
            }


            public void Add(IEnumerable<MinimaxMetrics> metrics) => m_metricsList.Add(metrics.ToArray());

            public void Clear() => m_metricsList.Clear();


            public void Log()
            {
                var moveMetrics = m_metricsList.Select(m => MinimaxMetrics.Aggregate(m)).ToList();

                var runtime = TimeSpan.Zero;
                foreach (var metrics in moveMetrics)
                {
                    runtime += metrics.RunTimeSpan;
                }

                string timePerMove = String.Format(@"{0:mm\:ss\.fff}",
                        TimeSpan.FromTicks(runtime.Ticks / moveMetrics.Count));
                string totalTime   = String.Format(@"{0:hh\:mm\:ss\.fff}", runtime);

                s_logger.Info("");
                s_logger.Info("Aggregated performance metrics:");
                s_logger.Info($"  Stone Moves Made:    {moveMetrics.Sum(m => m.StoneMoveCount.Sum())}");
                s_logger.Info($"  Stack Moves Made:    {moveMetrics.Sum(m => m.StackMoveCount.Sum())}");
                s_logger.Info($"  Boards Evaluated:    {moveMetrics.Sum(m => m.EvaluationCount.Sum())}");
                s_logger.Info($"  Null Window (Pass):  {moveMetrics.Sum(m => m.NullWindowPassCount.Sum())}");
                s_logger.Info($"  Null Window (Fail):  {moveMetrics.Sum(m => m.NullWindowFailCount.Sum())}");
                s_logger.Info($"  TT Stores:           {moveMetrics.Sum(m => m.TableStoreCount.Sum())}");
                s_logger.Info($"  TT Searches:         {moveMetrics.Sum(m => m.TableSearchCount.Sum())}");
                s_logger.Info($"  TT Matches:          {moveMetrics.Sum(m => m.TableFoundCount.Sum())}");
                s_logger.Info($"  TT Usable:           {moveMetrics.Sum(m => m.TableMatchCount.Sum())}");
                s_logger.Info($"  Moves Executed:      {moveMetrics.Count}");
                s_logger.Info($"  Average Move Time:   {timePerMove}");
                s_logger.Info($"  Total Move Time:     {totalTime}");
                s_logger.Info("");
            }
        }
    }
}
