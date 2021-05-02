using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.CommandLine;
using System.CommandLine.Invocation;
using STak.TakEngine;
using STak.TakEngine.AI;
using STak.TakEngine.Extensions;

namespace STak.TakTool
{
    class Program
    {
        private static Option[] MoveOptions = new Option[]
        {
            new Option<string> ("--ai-name",          () => "Dinkum Thinkum",     "The name of the AI to make the move."),
            new Option<int>    ("--ai-search-depth",  () => 4,                    "The search depth used by the AI."),
            new Option<int>    ("--ai-thinking-time", () => 0,                    "The maximum per-move thinking time."),
            new Option<int>    ("--board-size",       () => 5,                    "The size of the game board."),
            new Option<bool>   ("--verbose",          () => false,                "Produce verbose output.")
        };

        private static Option[] BattleOptions = new Option[]
        {
            new Option<string> ("--name",              () => "DT_vs_TE",           "The name to assign the battle."),

            new Option<string> ("--ai1-name",          () => "Dinkum Thinkum",     "Name of player 1 AI."),
            new Option<int>    ("--ai1-search-depth",  () => 4,                    "Search depth used by player 1 AI."),
            new Option<int>    ("--ai1-thinking-time", () => 0,                    "Maximum thinking time for player 1 AI."),

            new Option<string> ("--ai2-name",          () => "The Experiment",     "Name of player 2 AI."),
            new Option<int>    ("--ai2-search-depth",  () => 4,                    "Search depth used by player 2 AI."),
            new Option<int>    ("--ai2-thinking-time", () => 0,                    "Maximum thinking time for player 1 AI."),

            new Option<int>    ("--board-size",        () => 5,                    "Size of the game board."),
            new Option<int>    ("--game-count",        () => 1,                    "Number of games in the competition."),
            new Option<int>    ("--random-seed",       () => 0,                    "Random number generator seed value."),
            new Option<bool>   ("--force",             () => false,                "Force overwrite of existing result files."),
            new Option<bool>   ("--verbose",           () => false,                "Produce verbose output.")
        };


        static void Main(string[] args)
        {
            var root   = new RootCommand("root");
            var battle = new Command("battle");
            var move   = new Command("move");

            foreach (var option in BattleOptions) { battle.Add(option); }
            foreach (var option in MoveOptions  ) { move  .Add(option); }

            battle.Handler = CommandHandler.Create((BattleOptionsBag options) => DoBattle(options));
            move  .Handler = CommandHandler.Create((MoveOptionsBag   options) => DoMove  (options));

            root.Add(battle);
            root.Add(move);

            root.Handler = CommandHandler.Create(() => {});
            root.Invoke(args);
        }


        private static void DoBattle(BattleOptionsBag options)
        {
            // PrintProperties(options, BindingFlags.Public | BindingFlags.Instance);

            var battleName         = options.Name;

            var ai1Name            = options.AI1Name;
            var ai1SearchDepth     = options.AI1SearchDepth;
            var ai1MaxThinkingTime = options.AI1ThinkingTime;

            var ai2Name            = options.AI2Name;
            var ai2SearchDepth     = options.AI2SearchDepth;
            var ai2MaxThinkingTime = options.AI2ThinkingTime;

            var boardSize          = options.BoardSize;
            var gameCount          = options.GameCount;

            var seed               = options.RandomSeed;
            var force              = options.Force;
            var verbose            = options.Verbose;

            VerboseWriter.Verbose = verbose;

            TakAI.LoadPlugins();

            string battleDir = battleName;

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
            var battle = new Battle(battleName, battleDir, ai1Name, ai2Name, force);

            Stopwatch[] stopwatches = { new Stopwatch(), new Stopwatch() };

            VerboseWriter.Write($"Starting {gameCount} game battle on {boardSize}x{boardSize} board:"
                                                                       + $" {ai1Name} vs. {ai2Name}.");

            for (int gameNumber = 1; gameNumber <= gameCount; ++gameNumber)
            {
                var prototype = new GamePrototype(player1, player2, boardSize);
                var game      = new BasicGame(prototype);

                var moveRecords = new List<MoveRecord>();

                VerboseWriter.Write($"Starting game {gameNumber}/{gameCount} - {ai1Name} vs. {ai2Name}.");

                game.Initialize();
                game.Start();

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


        private static void DoMove(MoveOptionsBag options)
        {
            // PrintProperties(options, BindingFlags.Public | BindingFlags.Instance);

            var aiName          = options.AIName;
            var searchDepth     = options.SearchDepth;
            var maxThinkingTime = options.ThinkingTime;
            var boardState      = options.BoardState;
            var verbose         = options.Verbose;

            VerboseWriter.Verbose = verbose;

            TakAI.LoadPlugins();

            var ai = TakAI.GetAI(aiName);
            ai.Options = AIConfiguration<TakAIOptions>.Get(aiName);

            // Negative values signify (positive) milliseconds; positive values signify seconds; zero means infinite.
            // Note: -1 is equivalent to Timeout.InfiniteTimeSpan, used in the .NET Core timer code.

            maxThinkingTime = (maxThinkingTime > 0) ?  maxThinkingTime * 1000
                            : (maxThinkingTime < 0) ? -maxThinkingTime
                                                    : -1;

            ai.Options.MaximumThinkingTime = maxThinkingTime;

            if (searchDepth > 0)
            {
                ai.Options.TreeEvaluationDepth = searchDepth;
            }

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


        private static void PrintProperties(object options, BindingFlags bindingFlags)
        {
            var maximumLength = 0;

            foreach (var property in options.GetType().GetProperties(bindingFlags))
            {
                maximumLength = Math.Max(maximumLength, property.Name.Length);
            }

            foreach (var property in options.GetType().GetProperties(bindingFlags))
            {
                var name  = property.Name;
                var value = property.GetValue(options);

                Console.WriteLine($"{name.PadRight(maximumLength)} => {value}");
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
            public string[]           Players  { get; private set; }
            public string             Location { get; private set; }
            public List<BattleResult> Results  { get; private set; } = new List<BattleResult>();


            private Battle()
            {
            }


            public Battle(string name, string location, string player1Name, string player2Name, bool force = false)
            {
                Name     = name;
                Location = location;
                Players  = new [] { player1Name, player2Name };

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
                    int ties = 0;
                    var wins = new Dictionary<string, int>();
                    string pathName;

                    wins[Players[Player.One]] = 0;
                    wins[Players[Player.Two]] = 0;

                    for (int i = 0; i < Results.Count; ++i)
                    {
                        var result = Results[i];
                        pathName = Path.Combine(Location, $"{Name}-Battle-{(i+1):D2}.ptn");
                        File.WriteAllText(pathName, PtnParser.ToString(result.GameRecord));

                        if (result.Result.Winner == Player.One || result.Result.Winner == Player.Two)
                        {
                            wins[Players[result.Result.Winner]]++;
                        }
                        else
                        {
                            ties++;
                        }
                    }

                    string player1Name = Players[Player.One];
                    string player2Name = Players[Player.Two];

                    int player1Wins   = wins[player1Name];
                    int player2Wins   = wins[player2Name];
                    int player1Losses = Results.Count - (player1Wins + ties);
                    int player2Losses = Results.Count - (player2Wins + ties);

                    string winnerName = (player1Wins > player2Wins) ? player1Name
                                      : (player2Wins > player1Wins) ? player2Name
                                                                    : "Draw (Tie)";

                    string message = $"Overall winner: {winnerName}\n"
                                   + $"{player1Name} wins/losses/draw: {player1Wins}/{player1Losses}/{ties}\n"
                                   + $"{player2Name} wins/losses/draw: {player2Wins}/{player2Losses}/{ties}\n";

                    pathName = Path.Combine(Location, $"{Name}-Results.txt");
                    File.WriteAllText(pathName, message);
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


        private class OptionsBag
        {
        }


        private class MoveOptionsBag : OptionsBag
        {
            public string AIName       { get; set; }
            public int    SearchDepth  { get; set; }
            public int    ThinkingTime { get; set; }
            public string BoardState   { get; set; }
            public bool   Verbose      { get; set; }
        }


        private class BattleOptionsBag : OptionsBag
        {
            public string Name            { get; set; }

            public string AI1Name         { get; set; }
            public int    AI1SearchDepth  { get; set; }
            public int    AI1ThinkingTime { get; set; }

            public string AI2Name         { get; set; }
            public int    AI2SearchDepth  { get; set; }
            public int    AI2ThinkingTime { get; set; }

            public int    BoardSize       { get; set; }
            public int    GameCount       { get; set; }
            public int    RandomSeed      { get; set; }
            public bool   Force           { get; set; }
            public bool   Verbose         { get; set; }
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
