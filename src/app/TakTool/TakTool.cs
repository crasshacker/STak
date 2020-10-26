using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using CommandLine;
using STak.TakEngine;
using STak.TakEngine.AI;
using STak.TakEngine.Extensions;

namespace STak.TakTool
{
    public class Program
    {
        static int Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<MoveOptions, BattleOptions>(args)
                                       .WithParsed<MoveOptions>(options   => ProcessMoveCommand(options))
                                       .WithParsed<BattleOptions>(options => ProcessBattleCommand(options))
                                       .WithNotParsed(errors              => ProcessErrors(errors));

            return result is NotParsed<object> ? 1 : 0;
        }


        private static void ProcessMoveCommand(MoveOptions options)
        {
            var aiName          = options.AIName;
            var searchDepth     = options.SearchDepth;
            var maxThinkingTime = options.MaxThinkingTime;
            var boardState      = options.BoardState;
            var verbose         = options.Verbose;

            VerboseWriter.Verbose = verbose;

            TakAI.LoadPlugins();

            var ai = TakAI.GetAI(aiName);
            ai.Options = AIConfiguration<TakAIOptions>.Get(aiName);

            // Negative values represent (positive) milliseconds; positive values represent seconds.
            maxThinkingTime = (maxThinkingTime < 0) ? -maxThinkingTime : maxThinkingTime * 1000;

            if (searchDepth     > 0) { ai.Options.TreeEvaluationDepth = searchDepth;     }
            if (maxThinkingTime > 0) { ai.Options.MaximumThinkingTime = maxThinkingTime; }

            string ptn = File.Exists(boardState) ? File.ReadAllText(boardState) : boardState;
            var record = PtnParser.ParseText(ptn);

            if (! record.Headers.ContainsKey("Player1")) { record.Headers["Player1"] = "Player One"; }
            if (! record.Headers.ContainsKey("Player2")) { record.Headers["Player2"] = "Player Two"; }

            var prototype = new GamePrototype(record);
            var game = new BasicGame(prototype, true);

            Stopwatch stopwatch = null;

            if (verbose)
            {
                VerboseWriter.Write($"Starting search; timeout is {maxThinkingTime/1000} seconds.");
                stopwatch = new Stopwatch();
                stopwatch.Start();
            }

            if (game.IsCompleted)
            {
                VerboseWriter.Write("Error: Game has already ended!");
            }

            using var cancelTokenSource = new CancellationTokenSource(maxThinkingTime);
            var move = ai.ChooseNextMove(game, cancelTokenSource.Token);
            Console.WriteLine(move);

            if (verbose)
            {
                string verb = cancelTokenSource.IsCancellationRequested ? "Cancelled" : "Completed";
                VerboseWriter.Write($"{verb} in {stopwatch.Elapsed.ToString()} seconds.");
                stopwatch.Stop();
            }
        }


        private static void ProcessBattleCommand(BattleOptions options)
        {
            var battleName         = options.Name;

            var ai1Info            = options.AI1;
            var ai1Name            = options.AI1Name;
            var ai1SearchDepth     = options.AI1SearchDepth;
            var ai1MaxThinkingTime = options.AI1MaxThinkingTime;

            var ai2Info            = options.AI2;
            var ai2Name            = options.AI2Name;
            var ai2SearchDepth     = options.AI2SearchDepth;
            var ai2MaxThinkingTime = options.AI2MaxThinkingTime;

            var boardSize          = options.BoardSize;
            var gameCount          = options.GameCount;

            var seed               = options.Seed;
            var force              = options.Force;
            var verbose            = options.Verbose;

            VerboseWriter.Verbose = verbose;

            TakAI.LoadPlugins();

            string battleDir = battleName;

            if (ai1Info != null)
            {
                var parts = Regex.Split(ai1Info, @"\s*,\s*");
                if (parts.Length != 3) { Barf("Invalid ai1 option value."); }
                ai1Name            = parts[0];
                ai1SearchDepth     = Int32.Parse(parts[1]);
                ai1MaxThinkingTime = Int32.Parse(parts[2]);
            }

            if (ai2Info != null)
            {
                var parts = Regex.Split(ai2Info, @"\s*,\s*");
                if (parts.Length != 3) { Barf("Invalid ai2 option value."); }
                ai2Name            = parts[0];
                ai2SearchDepth     = Int32.Parse(parts[1]);
                ai2MaxThinkingTime = Int32.Parse(parts[2]);
            }

            var ai1 = TakAI.GetAI(ai1Name);
            var ai2 = TakAI.GetAI(ai2Name);

            ai1.Options = AIConfiguration<TakAIOptions>.Get(ai1Name);
            ai2.Options = AIConfiguration<TakAIOptions>.Get(ai2Name);

            if (ai1SearchDepth > 0) { ai1.Options.TreeEvaluationDepth = ai1SearchDepth; }
            if (ai2SearchDepth > 0) { ai2.Options.TreeEvaluationDepth = ai2SearchDepth; }

            // Negative values signify (positive) milliseconds; positive values signify seconds; zero means infinite.
            // Note: -1 is equivalent to Timeout.InfiniteTimeSpan, used in the .NET Core timer code.

            ai1MaxThinkingTime = (ai1MaxThinkingTime > 0) ?  ai1MaxThinkingTime * 1000
                               : (ai1MaxThinkingTime < 0) ? -ai1MaxThinkingTime
                                                          : -1;
            ai2MaxThinkingTime = (ai2MaxThinkingTime > 0) ?  ai2MaxThinkingTime * 1000
                               : (ai2MaxThinkingTime < 0) ? -ai2MaxThinkingTime
                                                          : -1;

            ai1.Options.MaximumThinkingTime = ai1MaxThinkingTime;
            ai2.Options.MaximumThinkingTime = ai2MaxThinkingTime;

            ai1.Options.RandomizationSeed = seed;
            ai2.Options.RandomizationSeed = seed;

            if (seed != 0)
            {
                ai1.Options.EvaluateMovesInParallel = false;
                ai2.Options.EvaluateMovesInParallel = false;
                ai1.Options.EvaluateCellsRandomly   = false;
                ai2.Options.EvaluateCellsRandomly   = false;
            }

            var player1 = new Player(ai1Name, ai1);
            var player2 = new Player(ai2Name, ai2);

            Stopwatch stopwatch = new Stopwatch();
            var battle = new Battle(battleName, battleDir, force);

            Stopwatch[] stopwatches = { new Stopwatch(), new Stopwatch() };

            VerboseWriter.Write($"Starting {gameCount} game battle on {boardSize}x{boardSize} board:"
                                                                       + $" {ai1Name} vs. {ai2Name}.");

            for (int gameNumber = 1; gameNumber <= gameCount; ++gameNumber)
            {
                var prototype = new GamePrototype(player1, player2, boardSize);
                var game      = new BasicGame(prototype);

                var moveRecords = new List<MoveRecord>();

                VerboseWriter.Write($"Starting game {gameNumber}/{gameCount} - {ai1Name} vs. {ai2Name}.");

                for (int ply = 0; ! game.IsCompleted; ply++)
                {
                    int playerId = ply % 2;
                    var ai = playerId == 0 ? ai1 : ai2;
                    var start = stopwatches[playerId].Elapsed;
                    stopwatches[playerId].Start();

                    int timeout = (playerId == Player.One) ? ai1MaxThinkingTime : ai2MaxThinkingTime;
                    using var cancelTokenSource = new CancellationTokenSource(timeout);
                    var move = ai.ChooseNextMove(game, cancelTokenSource.Token);

                    var elapsed = stopwatches[playerId].Elapsed - start;
                    bool cancelled = cancelTokenSource.IsCancellationRequested;
                    var moveRecord = new MoveRecord(ply, move, elapsed, cancelled);
                    VerboseWriter.Write(moveRecord.ToString());
                    moveRecords.Add(moveRecord);

                    game.MakeMove(move);
                    stopwatches[playerId].Stop();
                }

                var ai1Time = stopwatches[0].Elapsed;
                var ai2Time = stopwatches[1].Elapsed;

                string ptn = PtnParser.ToString(game, "TakTool", false, true, true);
                var result = new BattleResult(ai1Name, ai1Time, ai2Name, ai2Time, ptn, moveRecords);
                battle.AddResult(result);
                battle.Save();

                stopwatches[0].Reset();
                stopwatches[1].Reset();

                VerboseWriter.Write($"\nGame {gameNumber} results:\n{result}\n");
            }
        }


        private static void Barf(string message)
        {
            VerboseWriter.Write(message);
            Environment.Exit(1);
        }


        private class Battle
        {
            public string             Name     { get; private set; }
            public string             Location { get; private set; }
            public List<BattleResult> Results  { get; private set; } = new List<BattleResult>();


            private Battle()
            {
            }


            public Battle(string name, string location, bool force = false)
            {
                Name     = name;
                Location = location;

                if (Name != null)
                {
                    if (Directory.Exists(location))
                    {
                        if (! force)
                        {
                            Barf($"A battle at location {location} already exists.");
                        }
                        Directory.Delete(location, true);
                    }
                    try
                    {
                        Directory.CreateDirectory(location);
                    }
                    catch (Exception ex)
                    {
                        Barf($"Could not create directory {location}: {ex.Message}");
                    }
                }
            }


            public void AddResult(BattleResult result) => Results.Add(result);


            public void Save()
            {
                if (Name != null)
                {
                    for (int i = 0; i < Results.Count; ++i)
                    {
                        var result = Results[i];
                        string pathName = Path.Combine(Location, $"{Name}-Battle-{(i+1):D2}.ptn");
                        File.WriteAllText(pathName, PtnParser.ToString(result.GameRecord));
                    }
                }
            }
        }


        private class BattleResult
        {
            public string                  AI1Name     { get; private set; }
            public string                  AI2Name     { get; private set; }
            public TimeSpan                AI1Time     { get; private set; }
            public TimeSpan                AI2Time     { get; private set; }
            public GameRecord              GameRecord  { get; private set; }
            public IEnumerable<MoveRecord> MoveRecords { get; private set; }

            public GameResult Result => GameRecord.Result;


            public BattleResult(string ai1Name, TimeSpan ai1Time, string ai2Name, TimeSpan ai2Time, string record,
                                                                              IEnumerable<MoveRecord> moveRecords)
            {
                AI1Name     = ai1Name;
                AI1Time     = ai1Time;
                AI2Name     = ai2Name;
                AI2Time     = ai2Time;
                GameRecord  = PtnParser.ParseText(record);
                MoveRecords = moveRecords;
            }


            public override string ToString()
            {
                string winner = Result.Winner == Player.One ? $"Player 1 ({AI1Name})"
                              : Result.Winner == Player.Two ? $"Player 2 ({AI2Name})"
                                                            : "None (draw)";

                return $"Player 1: {AI1Name}\n"
                     + $"Player 2: {AI2Name}\n"
                     + $"Winner:   {winner}\n"
                     + $"Type:     {Result.WinType}\n"
                     + $"Score:    {Result.Score}\n"
                     + $"AI1 Time: {AI1Time}\n"
                     + $"AI2 Time: {AI2Time}";
            }


            public string ToString(bool verbose)
            {
                string me = ToString() + "\n";

                if (verbose)
                {
                    me += "\n";
                    foreach (var moveRecord in MoveRecords)
                    {
                        me += moveRecord.ToString() + "\n";
                    }
                }

                return me;
            }
        }


        private class MoveRecord
        {
            public int      Ply       { get; set; }
            public IMove    Move      { get; set; }
            public TimeSpan Duration  { get; set; }
            public bool     Cancelled { get; set; }


            public MoveRecord(int ply, IMove move, TimeSpan duration, bool cancelled)
            {
                Ply       = ply;
                Move      = move;
                Duration  = duration;
                Cancelled = cancelled;
            }


            public override string ToString()
            {
                int playerId = (Ply % 2 == 0) ? Player.One : Player.Two;
                string elapsed = Duration.ToString(@"hh\:mm\:ss\.ff");
                string cancelled = Cancelled ? "yes," : "no, ";

                return $"Ply: {Ply,3:D}, Turn: {(Ply/2+1),3:D}, Player: {playerId+1}, Time: {elapsed},"
                                                              + $" Cancelled: {cancelled} Move: {Move}";
            }
        }


        private static void ProcessErrors(IEnumerable<Error> errors)
        {
            // No action needed - errors are printed by default.`
        }


        [Verb("move", HelpText="Choose a good move, print the PTN to stdout.")]
        public class MoveOptions
        {
            [Option(Default="Dinkum Thinkum")]
            public string AIName { get; set; }

            [Option(Default=3)]
            public int SearchDepth { get; set; }

            [Option(Default=-1)]
            public int MaxThinkingTime { get; set; }

            [Option(Required=true)]
            public string BoardState { get; set; }

            [Option(Default=false)]
            public bool Verbose { get; set; }
        }


        [Verb("battle", HelpText="Battle two AIs against one another in a series of games.")]
        public class BattleOptions
        {
            [Option(Default=null)]
            public string Name { get; set; }

            [Option(SetName="shorthand", Default=null)]
            public string AI1 { get; set; }

            [Option(SetName="longhand", Default="Dinkum Thinkum")]
            public string AI1Name { get; set; }

            [Option(SetName="longhand", Default=3)]
            public int AI1SearchDepth { get; set; }

            [Option(SetName="longhand", Default=0)]
            public int AI1MaxThinkingTime { get; set; }

            [Option(SetName="shorthand", Default=null)]
            public string AI2 { get; set; }

            [Option(SetName="longhand", Default="The Experiment")]
            public string AI2Name { get; set; }

            [Option(SetName="longhand", Default=3)]
            public int AI2SearchDepth { get; set; }

            [Option(SetName="longhand", Default=0)]
            public int AI2MaxThinkingTime { get; set; }

            [Option(Default=5, Required=true)]
            public int BoardSize { get; set; }

            [Option(Default=1)]
            public int GameCount { get; set; }

            [Option(Default=0)]
            public int Seed { get; set; }

            [Option(Default=false)]
            public bool Force { get; set; }

            [Option(Default=false)]
            public bool Verbose { get; set; }
        }


        private static class VerboseWriter
        {
            private static object s_syncLock = new object();

            public static bool Verbose = true;

            public static void Write(string message, bool force = false)
            {
                if (Verbose || force)
                {
                    lock (s_syncLock)
                    {
                        var now = DateTime.Now.ToString("hh:mm:ss.fff");
                        foreach (string line in message.Split("\n"))
                        {
                            Console.WriteLine($"[ {now} ] {line}");
                        }
                    }
                }
            }
        }
    }
}
