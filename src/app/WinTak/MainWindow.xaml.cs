using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Media;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Microsoft.Extensions.Configuration;
using NLog;
using NLog.Common;
using StickyWindows;
using StickyWindows.WPF;
using STak.TakEngine;
using STak.TakEngine.AI;
using STak.TakEngine.Actors;
using STak.TakEngine.Trackers;
using STak.TakEngine.Management;
using STak.TakEngine.Extensions;

using STak.TakHub.Client;
using STak.TakHub.Client.Trackers;
using STak.TakHub.Interop;

namespace STak.WinTak
{
    public partial class MainWindow : Window, IDispatcher
    {
        private static readonly NLog.Logger s_logger = LogManager.GetCurrentClassLogger();

        private const string c_takUserGuideDocument         = "UserGuide.html";
        private const string c_projectStatusDocument        = "ProjectStatus.html";
        private const string c_akkaConfigFileName           = "WinTak.hocon";
        private const string c_defaultGameFileName          = "Tak";
        private const string c_defaultGameFileNameExtension = ".ptn";
        private const string c_defaultGameFileNameFilter    = "Portable Tak Notation files (*.ptn)|*.ptn|All files|*.*";

        private static bool     AutoReload               => UIAppConfig.AutoReload;
        private static int      MoveAnimationTime        => UIAppConfig.Appearance.Animation.MoveAnimationTime;
        private static int      HintAnimationPauseTime   => UIAppConfig.Appearance.Animation.HintAnimationPauseTime;
        private static string   DetachGameUponCompletion => UIAppConfig.Behavior.DetachGameUponCompletion;
        private static bool     AllowUnsafeOperations    => UIAppConfig.Behavior.AllowUnsafeOperations;
        private static bool     UseVerbosePtnNotation    => UIAppConfig.Behavior.UseVerbosePtnNotation;
        private static bool     UseMirroringGame         => UIAppConfig.Framework.UseMirroringGame;
        private static bool     UseActorSystem           => UIAppConfig.Framework.UseActorSystem;
        private static string   ActorSystemAddress       => UIAppConfig.Framework.ActorSystemAddress;
        private static string[] WinGameSounds            => UIAppConfig.Sounds.WinGame;
        private static string[] LoseGameSounds           => UIAppConfig.Sounds.LoseGame;

        private static MainWindow s_mainWindow;

        private bool m_canExecuteNewCommand;
        private bool m_canExecuteOpenCommand;
        private bool m_canExecuteSaveCommand;
        private bool m_canExecuteSaveAsCommand;

        private readonly WinTakHubClient      m_takHubClient;
        private readonly GameManager          m_gameManager;
        private readonly DebugLogWindowTarget m_debugLogWindowTarget;
        private readonly LogWindow            m_debugLogWindow;
        private readonly LogWindow            m_bitBoardLogWindow;
        private readonly LogWindow            m_gameMoveLogWindow;
        private readonly GameMoveLogger       m_gameMoveLogger;
        private readonly BitBoardLogger       m_bitBoardLogger;
        private          IGame                m_game;
        private          GamePrototype        m_gamePrototype;
        private          string               m_saveFileName;
        private          string               m_savedGameDirectory;
        private          TakHubWindow         m_takHubWindow;
        private          ChatWindow           m_chatWindow;
        private          PlaybackController   m_playbackController;
        private          bool                 m_isWindowClosing;
        private          bool                 m_announceResult;
        private          Player               m_hintPlayer;
        private          IMove                m_hintMove;

        public static readonly RoutedCommand ConnectToHubCommand      = new RoutedCommand();
        public static readonly RoutedCommand DisconnectFromHubCommand = new RoutedCommand();
        public static readonly RoutedCommand CopyPtnCommand           = new RoutedCommand();
        public static readonly RoutedCommand PastePtnCommand          = new RoutedCommand();
        public static readonly RoutedCommand AppearanceCommand        = new RoutedCommand();
        public static readonly RoutedCommand EnableAudioCommand       = new RoutedCommand();
        public static readonly RoutedCommand AdvancedOptionsCommand   = new RoutedCommand();
        public static readonly RoutedCommand ShowMoveHintCommand      = new RoutedCommand();
        public static readonly RoutedCommand ResetViewCommand         = new RoutedCommand();
        public static readonly RoutedCommand ShowTakHubWindowCommand  = new RoutedCommand();
        public static readonly RoutedCommand ShowChatWindowCommand    = new RoutedCommand();
        public static readonly RoutedCommand GameMoveLogCommand       = new RoutedCommand();
        public static readonly RoutedCommand BitBoardLogCommand       = new RoutedCommand();
        public static readonly RoutedCommand DebugLogCommand          = new RoutedCommand();
        public static readonly RoutedCommand CancelCommand            = new RoutedCommand();
        public static readonly RoutedCommand UserGuideCommand         = new RoutedCommand();
        public static readonly RoutedCommand ProjectStatusCommand     = new RoutedCommand();
        public static readonly RoutedCommand AboutTakCommand          = new RoutedCommand();

        public static MainWindow Instance => s_mainWindow;

        public bool        IsDispatchNeeded  => ! Dispatcher.CheckAccess();
        public bool        IsWindowClosing   => m_isWindowClosing;
        public GameManager GameManager       => m_gameManager;
        public TableView   TableView         => m_tableView;
        public LogWindow   DebugLogWindow    => m_debugLogWindow;
        public LogWindow   BitBoardLogWindow => m_bitBoardLogWindow;
        public LogWindow   GameMoveLogWindow => m_gameMoveLogWindow;
        public bool        IsGameActive      => m_game != null && ! m_game.WasCompleted &&
                                               (m_game.ExecutedMoves.Count + m_game.RevertedMoves.Count) > 0;


        public bool AudioEnabled
        {
            get => (LogicalTreeHelper.FindLogicalNode(this, "EnableAudio") as MenuItem).IsChecked;
            set => (LogicalTreeHelper.FindLogicalNode(this, "EnableAudio") as MenuItem).IsChecked = value;
        }


        public bool ChatWindowEnabled
        {
            get => (LogicalTreeHelper.FindLogicalNode(this, "ShowChatWindow") as MenuItem).IsChecked;
            set => (LogicalTreeHelper.FindLogicalNode(this, "ShowChatWindow") as MenuItem).IsChecked = value;
        }


        static MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(GlobalExceptionHandler);
        }


        public MainWindow()
        {
            s_mainWindow = this;

            App.InitializeLogging();

            InitializeScheme();
            InitializeComponent();

            // Tell AI players how to fetch the animation time.  Yes, this is ugly.
            Player.MoveAnimationTime = (p) => MoveAnimationTime;

            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            this.ContentRendered += ContentRenderedEventHandler;
            this.Closing += ClosingEventHandler;

            Rect rect = Properties.Settings.Default.WindowRect;

            if (rect.Width == 0 || rect.Height == 0)
            {
                double screenWidth  = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;

                double width  = screenWidth  * 0.50;
                double height = screenHeight * 0.65;
                double left   = (screenWidth  - width ) / 2;
                double top    = (screenHeight - height) / 2;

                rect = new Rect(left, top, width, height);
            }

            this.Left   = rect.Left;
            this.Top    = rect.Top;
            this.Width  = rect.Width;
            this.Height = rect.Height;

            m_debugLogWindow    = new LogWindow(this, "DebugWindow",    "Debug Log");
            m_bitBoardLogWindow = new LogWindow(this, "BitBoardWindow", "Bitboard Log");
            m_gameMoveLogWindow = new LogWindow(this, "GameMoveWindow", "Game Move Log");

            // Have the debug log window listen for keystrokes that change the logging level.
            m_debugLogWindow.PreviewKeyDown += DebugWindowKeyDownHandler;

            // Configure the debug log window as an NLog target, and register it.
            m_debugLogWindowTarget = new DebugLogWindowTarget(m_debugLogWindow);
            m_debugLogWindowTarget.Register();

            // We write to these log windows directly, rather than via NLog.
            m_bitBoardLogger = new BitBoardLogger(m_bitBoardLogWindow);
            m_gameMoveLogger = new GameMoveLogger(m_gameMoveLogWindow);

            m_gameManager  = new SingletonGameManager();
            m_takHubClient = CreateHubClient();

            // When a new kibitzer shows up at a table and it's us, kibitz the game.
            m_takHubClient.Tracker.KibitzerAdded += HandleKibitzerAdded;

            // Keep TakHub up to date with player's preferred animation time/speed.
            MoveAnimation.AnimationTimeChanged += HandleMoveAnimationTimeChanged;

            InitializeAIOptions();

            if (UseActorSystem)
            {
                InitializeActorSystem();
            }
        }


        public void SaveScheme()
        {
            Properties.Settings.Default.BackgroundColor    = Scheme.Current.BackgroundColor;
            Properties.Settings.Default.BoardTextureFile   = Scheme.Current.BoardTextureFile;
            Properties.Settings.Default.P1StoneTextureFile = Scheme.Current.P1StoneTextureFile;
            Properties.Settings.Default.P2StoneTextureFile = Scheme.Current.P2StoneTextureFile;
            Properties.Settings.Default.WindowRect         = new Rect(Left, Top, Width, Height);
            Properties.Settings.Default.Save();
        }


        public bool? AskWhetherToQuitCurrentGame()
        {
            bool? quit = (m_game != null) ? (bool?) true : (bool?) null;

            if (IsGameActive)
            {
                MessageBoxResult result = MessageBox.Show("Would you like to quit the current game?",
                                       "Alert", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                quit = result == MessageBoxResult.Yes;
            }

            return quit;
        }


        public void AbandonCurrentGame(string _ = null)
        {
            if (m_game is HubGame)
            {
                // NOTE: DeactivateGame will return a new, unmanaged game instance.
                // TODO: Should we instead enter playback mode in the remote game?
                m_game = DeactivateGame(m_game);
            }
        }


        public void QuitCurrentGame(bool forceDetach = false)
        {
            if (m_game != null)
            {
                if (forceDetach || ShouldDetachGame(m_game, DetachGameUponCompletion))
                {
                    // NOTE: DeactivateGame might return a new, unmanaged game instance.
                    m_game = DeactivateGame(m_game);
                }
                m_gameManager.DestroyGame(m_game.Id);
            }
        }


        public async Task KibitzGame(GamePrototype prototype)
        {
            // Create a game to use to kibitz the remote players.
            m_game = CreateGame(CreateGameBuilder(true), prototype);

            // Initialize the local mirror of the remote game.
            var localGame = GetEventRaisingTracker(m_game).Game;
            localGame.Start();
            localGame.Initialize();

            // Normally registration occurs when the hub sends us a GameCreated message, but in this case
            // the remote game already exists so the hub sends no notification, so we do it ourselves.
            m_takHubClient.RegisterGame(m_game);

            // We'll complete initialization in HandleGameCreated.
            await m_takHubClient.KibitzGame(prototype.Id);
        }


        private async void HandleKibitzerAdded(object sender, KibitzerAddedEventArgs e)
        {
            if (m_takHubClient.UserName == e.KibitzerName)
            {
                var game = m_gameManager.GetGame(e.Prototype.Id);
                Debug.Assert(game == m_game, "Game mismatch (algorithm failure).");

                // Set up the table in the initial game state (no moves yet made).  Note that we pass false as
                // the animate argument to InitializeTable, because we need to avoid awaiting (giving up conrol
                // of the UI thread).  This is so that we don't process any incoming notifications from the hub
                // until everything has been synced up with the server-side game's state properly.
                await TableView.InitializeTable(game, false);

                // Update the local mirror of the game, and the model and view, with the game state.
                m_tableView.SynchronizeToGameState(GetEventRaisingTracker(game).Game, e.Prototype.Moves);
            }
        }


        private GameBuilder CreateGameBuilder(GameBuilderType builderType, bool mirrorGame, IDispatcher dispatcher,
                                                                                    GameHubClient hubClient = null)
        {
            var trackerBuilder = new TrackerBuilder();

            var builder = builderType switch
            {
                GameBuilderType.LocalDirect => (GameBuilder) new GameBuilder(trackerBuilder, mirrorGame, dispatcher),
                GameBuilderType.LocalActor  => (GameBuilder) new ActorGameBuilder(trackerBuilder, mirrorGame, dispatcher),
                GameBuilderType.Remote      => (GameBuilder) new HubGameBuilder(trackerBuilder, hubClient, dispatcher),
                _                           => null
            };

            builder.GameConstructed += OnGameConstructed;
            builder.GameDestructed  += OnGameDestructed;

            return builder;
        }


        private GameBuilder CreateGameBuilder(bool isRemote = false)
        {
            var builderType = isRemote       ? GameBuilderType.Remote
                            : UseActorSystem ? GameBuilderType.LocalActor
                                             : GameBuilderType.LocalDirect;

            return CreateGameBuilder(builderType, UseMirroringGame, this, isRemote ? m_takHubClient : null);
        }


        public IGame CreateGame(GameBuilder gameBuilder, GamePrototype prototype)
        {
            QuitCurrentGame();
            return m_gameManager.CreateGame(gameBuilder, prototype);
        }


        public void InitializeGame(IGame game)
        {
            m_game               = game;
            m_gamePrototype      = game.Prototype;
            m_playbackController = new PlaybackController(game);

            ObserveGame(game, GetEventRaisingTracker(game));
            SetControlState();
            Activate();
        }


        public void DestructGame(IGame game)
        {
            if (game is HubGame)
            {
                // TODO - Figure out a way to await the QuitGame call "synchronously."
                InvokeAsync(async () => { await m_takHubClient.QuitGame(game.Id).ConfigureAwait(false); });
            }

            UnobserveGame(game, GetEventRaisingTracker(game));
        }


        public void AlertGameAbandoned(Guid gameId, string abandonerName)
        {
            if (m_game.Id == gameId)
            {
                MessageBox.Show(this, $"Player {abandonerName} has left the game.", "Alert", MessageBoxButton.OK,
                                                                                     MessageBoxImage.Exclamation);
                AbandonCurrentGame(abandonerName);
            }
        }


        private static EventRaisingGameActivityTracker GetEventRaisingTracker(IGame game)
        {
            return new TrackerBuilder().GetEventRaisingTracker(game);
        }


        public IGame GetMirroringGame(Guid gameId)
        {
            return m_gameManager.GetGame(gameId);
        }


        public void Invoke(Action action)
        {
            Dispatcher.Invoke(action);
        }


        public void InvokeAsync(Action action)
        {
            Dispatcher.InvokeAsync(action);
        }


        public void ObserveGame(IGame game, IEventBasedGameActivityTracker tracker)
        {
            //
            // IMPORTANT NOTE:
            //
            // We add the handlers to players and views *before* adding handlers for the main window.  The reason
            // for this ordering is due to the way move redos are implemented.  When the player is playing against
            // an AI opponent and he re-executes a move, the EventRaisingTracker.OnMoveRedone method is eventually
            // called, which in turn triggers a TurnStarted event.  The MainWindow handler for TurnStarted then
            // sees that a RedoMove for the AI opponent is pending, so it executes it.  This also eventually gets
            // into the EventRaisingTracker.OnMoveRedone method, and completes execution of this method before the
            // call made into that method by the player has completed.  This results in the TurnStarted event for
            // the AI player to be triggered before the event for the event associated with the player's RedoMove.
            //
            // There's undoubtedly a cleaner, more obvious solution to this issue, but I haven't yet thought of
            // any solution that would not be painful and ugly to implement.
            //

            if (game.PlayerOne.IsAI) { game.PlayerOne.Observe(tracker); }
            if (game.PlayerTwo.IsAI) { game.PlayerTwo.Observe(tracker); }

            m_tableView.Observe(tracker);

            tracker.GameCreated   += HandleGameCreated;
            tracker.GameStarted   += HandleGameStarted;
            tracker.GameCompleted += HandleGameCompleted;
            tracker.TurnStarted   += HandleTurnStarted;
            tracker.TurnCompleted += HandleTurnCompleted;
            tracker.UndoInitiated += HandleUndoInitiated;
            tracker.UndoCompleted += HandleUndoCompleted;
            tracker.RedoCompleted += HandleRedoCompleted;
        }


        public void UnobserveGame(IGame game, IEventBasedGameActivityTracker tracker)
        {
            if (tracker != null)
            {
                if (game.PlayerOne.IsAI) { game.PlayerOne.Unobserve(tracker); }
                if (game.PlayerTwo.IsAI) { game.PlayerTwo.Unobserve(tracker); }

                m_tableView.Unobserve(tracker);

                tracker.GameCreated   -= HandleGameCreated;
                tracker.GameStarted   -= HandleGameStarted;
                tracker.GameCompleted -= HandleGameCompleted;
                tracker.TurnStarted   -= HandleTurnStarted;
                tracker.TurnCompleted -= HandleTurnCompleted;
                tracker.UndoInitiated -= HandleUndoInitiated;
                tracker.UndoCompleted -= HandleUndoCompleted;
                tracker.RedoCompleted -= HandleRedoCompleted;
            }
        }


        public async Task DisconnectFromHub()
        {
            if (m_takHubClient.IsConnected)
            {
                QuitCurrentGame(true);
                m_takHubWindow.Hide();
                await m_takHubClient.Disconnect();
                HideChatWindow();
            }
        }


        public bool CanUndoMove()
        {
            return (! MoveAnimation.IsActive)
                && (m_hintPlayer == null)
                && (m_playbackController.PendingUndos == 0)
                && m_game.CanUndoMove(m_game.LastPlayer.Id)
                && ((m_game.ActivePlayer.IsLocalHuman && m_game.LastPlayer.IsAI)
                 || (m_game.ActivePlayer.IsLocalHuman && m_game.LastPlayer.IsLocalHuman)
                 || (m_game.ActivePlayer.IsAI != m_game.LastPlayer.IsAI && m_game.IsCompleted)
                 || (m_game.LastPlayer.IsLocalHuman   && m_game.ActivePlayer.IsRemote && m_game.WasCompleted));
        }


        public bool CanRedoMove()
        {
            return (!MoveAnimation.IsActive)
                && (m_hintPlayer == null)
                && (m_game.RevertedMoves.Count > 0)
                && (m_game.RevertedMoves[^1] != m_hintMove)
                && (m_playbackController.PendingRedos == 0)
                && m_game.CanRedoMove(m_game.ActivePlayer.Id)
                && (m_game.ActivePlayer.IsLocalHuman
                 || m_game.ExecutedMoves.Count == 0);
        }


        public void UndoMove()
        {
            if (CanUndoMove())
            {
                if (m_game.LastPlayer.IsAI && m_game.ExecutedMoves.Count > 1)
                {
                    m_playbackController.AddPendingUndo();
                }
                m_game.InitiateUndo(MoveAnimationTime);
            }
        }


        public void RedoMove()
        {
            if (CanRedoMove())
            {
                if (m_game.LastPlayer.IsAI && m_game.RevertedMoves.Count > 1)
                {
                    m_playbackController.AddPendingRedo();
                }
                m_game.InitiateRedo(MoveAnimationTime);
            }
        }


        private static void InitializeScheme()
        {
            Scheme.Current = new Scheme
            {
                BackgroundColor    = Properties.Settings.Default.BackgroundColor,
                BoardTextureFile   = Properties.Settings.Default.BoardTextureFile,
                P1StoneTextureFile = Properties.Settings.Default.P1StoneTextureFile,
                P2StoneTextureFile = Properties.Settings.Default.P2StoneTextureFile
            };
        }


        private static void InitializeAIOptions()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("uiappsettings.json", optional:true, reloadOnChange:false)
                .Build();

            AIConfiguration<TakAIOptions>.Initialize(config);

            // TODO - Should TakAI.GetAI instead return a new ITakAI instance with the latest Options configured?
            foreach (string aiName in TakAI.GetAINames())
            {
                TakAI.GetAI(aiName).Options = AIConfiguration<TakAIOptions>.Get(aiName);
            }
        }


        private static void InitializeActorSystem()
        {
            string akkaConfigPath = Path.Combine(App.GetApplicationDirectory(), c_akkaConfigFileName);

            if (File.Exists(akkaConfigPath))
            {
                AkkaSystem.Initialize(File.ReadAllText(akkaConfigPath), ActorSystemAddress);
            }
        }


        private void StartInitialGame()
        {
            ITakAI takAI = TakAI.GetAI(TakAI.GetAINames().First());
            Player player1 = new Player(Environment.UserName);
            Player player2 = new Player(takAI.Name, takAI);
            m_gamePrototype = new GamePrototype(player1, player2);
            m_game = CreateGame(CreateGameBuilder(), m_gamePrototype);
        }


        private void SaveCurrentGame(string fileName)
        {
            if (m_game != null)
            {
                string site = (m_game is HubGame) ? "STak/TakHub" : "STak/WinTak";
                PtnParser.Save(fileName, m_game, site, UseVerbosePtnNotation, true, true, true);
                m_savedGameDirectory = Path.GetDirectoryName(fileName);
            }
        }


        private void SaveApplicationState()
        {
            SaveScheme();
        }


        private void RefreshConfiguration()
        {
            if (AutoReload)
            {
                // We don't want the animation speed to be refreshed because it's configurable through
                // the UI, and we don't want to change that value (or screw up the animation rate slider).
                double speed = MoveAnimation.GetAnimationSpeed();

                InteropAppConfig.Refresh();
                UIAppConfig.Refresh();

                // Restore the speed factor saved above.
                MoveAnimation.SetAnimationSpeed(speed);

                // Force update of stone highlighter.
                m_tableView.ApplyScheme(Scheme.Current);
            }
        }


        private void SetControlState()
        {
            if (m_game != null)
            {
                bool enable = AllowUnsafeOperations
                           || m_game.WasCompleted
                           || m_game.ActivePlayer.IsLocalHuman
                           || (m_game.PlayerOne.IsRemote && m_game.PlayerTwo.IsRemote)
                           || (m_game.PlayerOne.IsAI     && m_game.PlayerTwo.IsAI);

                m_canExecuteNewCommand    = enable;
                m_canExecuteOpenCommand   = enable;
                m_canExecuteSaveCommand   = enable;
                m_canExecuteSaveAsCommand = enable;
            }
        }


        private void ContentRenderedEventHandler(object sender, EventArgs e)
        {
            // We defer creating the sticky window until our window has been initialized.
            this.CreateStickyWindow(StickyWindowType.Anchor);

            StartInitialGame();
        }


        private void DebugWindowKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.D0 || e.Key == Key.D1 || e.Key == Key.D2 || e.Key == Key.D3 || e.Key == Key.D4
                                                                                         || e.Key == Key.D5)
            {
                m_debugLogWindowTarget.LogLevel = e.Key switch
                {
                    Key.D0 => LogLevel.Off,
                    Key.D1 => LogLevel.Error,
                    Key.D2 => LogLevel.Warn,
                    Key.D3 => LogLevel.Info,
                    Key.D4 => LogLevel.Debug,
                    Key.D5 => LogLevel.Trace,
                    _      => LogLevel.Off
                };
            }

            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.Key == Key.C)
                {
                    // Ctrl-C cuts AI thinking short.
                    m_game.ActivePlayer.StopThinking();
                }
            }
        }


        private async void ClosingEventHandler(object source, CancelEventArgs e)
        {
            async Task handler()
            {
                m_isWindowClosing = true;
                HideChatWindow();
                await DisconnectFromHub();
                SaveApplicationState();
            }

            try { await handler(); } catch { }
        }


        private async Task StartGame()
        {
            m_announceResult = true;

            if (m_game is HubGame && (m_game.PlayerOne.IsLocal || m_game.PlayerTwo.IsLocal))
            {
                // The server will start the game when both players have accepted.
                await m_takHubClient.AcceptGame();
            }
            else
            {
                m_game.Start();
            }
        }


        private WinTakHubClient CreateHubClient()
        {
            HubGameOptions options = null; // TODO?
            var tracker = new HubActivityTracker(this);
            ConnectionStatusWindow statusWindow = new ConnectionStatusWindow(this);
            IStatusDisplayer displayer = new ConnectionStatusDisplayer(statusWindow);
            var hubClient = new WinTakHubClient(m_gameManager, tracker, this, displayer, options);
            hubClient.GameBuilder = CreateGameBuilder(GameBuilderType.Remote, UseMirroringGame, this, hubClient);
            hubClient.Disconnected += TakHubDisconnectHandler;
            return hubClient;
        }


        private static bool ShouldDetachGame(IGame game, string detach)
        {
            return (game.PlayerOne.IsAI && game.PlayerTwo.IsAI) || game switch
            {
                HubGame _ => String.Equals(detach, "remote", StringComparison.OrdinalIgnoreCase)
                          || String.Equals(detach, "on",     StringComparison.OrdinalIgnoreCase),
                _         => String.Equals(detach, "local",  StringComparison.OrdinalIgnoreCase)
                          || String.Equals(detach, "on",     StringComparison.OrdinalIgnoreCase)
            };
        }


        private IGame DeactivateGame(IGame game)
        {
            HubGame hubGame = null;

            //
            // This allows a game against a remote player (either human or AI) to be reviewed and walked
            // back/forward through.  We do this by discarding the HubGame, replacing it with a local
            // Game, and humanizing the players.
            //
            if (game is HubGame)
            {
                hubGame = game as HubGame;
                s_logger.Debug("Deactivating hub game, converting to local game.");
                game = new Game(hubGame.BaseGame, hubGame.Tracker);
                hubGame.Tracker = null; // Important!  Detach tracker; the new game owns it now.
            }

            Player player1 = game.PlayerOne;
            Player player2 = game.PlayerTwo;

            if (player1.IsAI || player1.IsRemote) { s_logger.Debug($"Humanizing player 1 ({player1.Name})."); }
            if (player2.IsAI || player2.IsRemote) { s_logger.Debug($"Humanizing player 2 ({player2.Name})."); }

            if (player1.IsAI || player1.IsRemote) { game.HumanizePlayer(Player.One, player1.Name); }
            if (player2.IsAI || player2.IsRemote) { game.HumanizePlayer(Player.Two, player2.Name); }

            if (hubGame != null)
            {
                // Tell the TableView about the Game that replaced the HubGame.
                // Do this last, AFTER players have been humanized.
                s_logger.Debug("Informing table view of game conversion.");
                TableView.UpdateGame(game);
                m_announceResult = false;
            }

            return game;
        }


        private void AnnounceGameCompletion()
        {
            GameResult result      = m_game.Result;
            Player     player1     = m_game.PlayerOne;
            Player     player2     = m_game.PlayerTwo;
            string     player1Name = player1.Name;
            string     player2Name = player2.Name;

            if (result.WinType != WinType.Draw)
            {
                string [] playerNames   = { player1Name, player2Name };
                string    winningPlayer = playerNames[result.Winner];
                int       playerNumber  = result.Winner+1;
                string    winType       = result.WinType.ToString();
                string    score         = (result.Winner == Player.One) ? $"{result.Score}-0"
                                        : (result.Winner == Player.Two) ? $"0-{result.Score}"
                                                                        : "0-0";

                if ((result.Winner == Player.One && m_game.Players[Player.One].IsAI)
                 || (result.Winner == Player.Two && m_game.Players[Player.Two].IsAI))
                {
                    winningPlayer += " (AI)";
                }

                if (AudioEnabled && m_announceResult)
                {
                    if ((player1.IsLocalHuman && result.Winner == Player.One)
                     || (player2.IsLocalHuman && result.Winner == Player.Two))
                    {
                        AudioPlayer.PlaySound(WinGameSounds[new Random().Next(WinGameSounds.Length)]);
                    }
                    else if ((player1.IsLocalHuman && result.Winner == Player.Two)
                          || (player2.IsLocalHuman && result.Winner == Player.One))
                    {
                        AudioPlayer.PlaySound(LoseGameSounds[new Random().Next(LoseGameSounds.Length)]);
                    }
                }

                s_logger.Debug($"Game over: Player {playerNumber} ({winningPlayer}) wins!, "
                                                + $"Win type is {winType}, score is {score}");
            }
            else
            {
                MessageBox.Show("Game over: The game is a draw.");
            }

            _ = m_tableView.AnimateGameOver();

            m_announceResult = false;
        }


        private async Task AnimateMoveHint()
        {
            var tracker = GetEventRaisingTracker(m_game);
            // Prevent an AI player from moving until we're done.
            m_game.Players[m_game.LastPlayer.Id].Unobserve(tracker);
            // TODO - Build an AI of a configured type (appsettings).
            var ai = TakAI.GetAI(TakAI.GetAINames().First());
            var aiPlayer = new Player("Hinter", ai);
            aiPlayer.Join(null, m_game.ActivePlayer.Id); // To set aiPlayer.Id.
            m_hintPlayer = m_game.ActivePlayer;
            s_logger.Debug($"Animating hint for player {aiPlayer.Name} on behalf of player {m_hintPlayer.Name}.");
            m_game.ChangePlayer(aiPlayer);
            m_game.InitiateMove(await aiPlayer.ChooseMove(), MoveAnimationTime);
        }


        private static async void ShowAdvancedOptions(bool createBackup = true)
        {
            string editor = Environment.GetEnvironmentVariable("EDITOR") ?? "notepad.exe";

            string configFileDir  = App.GetApplicationDirectory();
            string configFileName = "uiappsettings.json";

            string configFile = Path.Combine(configFileDir, configFileName);
            string backupFile = configFile + "-BACKUP";

            if (createBackup || ! File.Exists(backupFile))
            {
                new FileInfo(configFile).CopyTo(backupFile, true);
            }

            var process = Process.Start(editor, configFile);
            await process.WaitForExitAsync();

            try
            {
                var options = new JsonSerializerOptions() { ReadCommentHandling = JsonCommentHandling.Skip };
                JsonSerializer.Deserialize(File.ReadAllText(configFile), typeof(object), options);
                new FileInfo(backupFile).Delete();
            }
            catch (JsonException ex)
            {
                string caption = "Invalid JSON Syntax";
                string message = $"Error at line {ex.LineNumber+1}, position {ex.BytePositionInLine+1}:\n\n"
                               + Regex.Replace(ex.Message, @"\s*Path:.*$", "") + "\n\n"
                               + "Do you wish to correct the error?  Select Yes if you would like to re-edit the "
                               + "file to fix the error; select No to instead discard your edits to the file and "
                               + "restore the original contents.";

                var result = MessageBox.Show(message, caption, MessageBoxButton.YesNo, MessageBoxImage.Exclamation,
                                                                                              MessageBoxResult.Yes);
                if (result == MessageBoxResult.Yes)
                {
                    ShowAdvancedOptions(false);
                }
                else
                {
                    new FileInfo(backupFile).CopyTo(configFile, true);
                }
            }
        }


        private static void OpenWebPage(string url)
        {
            try
            {
                string browserPath = BrowserFinder.GetDefaultBrowserPath();

                if (browserPath != null)
                {
                    Process.Start(browserPath, url);
                }
                else
                {
                    // This *should* work according to documentation I've read, but in my testing
                    // it always throws an exception: "The system cannot find the file specified."
                    Process.Start(url);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }


        private void NewCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = m_canExecuteNewCommand;
        }


        private void NewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Player player1 = m_gamePrototype.PlayerOne;
            Player player2 = m_gamePrototype.PlayerTwo;

            NewGameDialog dialog = new NewGameDialog(player1, player2)
            {
                Left = this.Left + 50,
                Top  = this.Top  + 50
            };

            if (dialog.ShowDialog() == true)
            {
                if (dialog.Player1Type == PlayerType.Human)
                {
                    player1 = new Player(dialog.Player1Name);
                }
                else
                {
                    player1 = new Player(dialog.Player1AIName, TakAI.GetAI(dialog.Player1AIName));
                }

                if (dialog.Player2Type == PlayerType.Human)
                {
                    player2 = new Player(dialog.Player2Name);
                }
                else
                {
                    player2 = new Player(dialog.Player2AIName, TakAI.GetAI(dialog.Player2AIName));
                }

                var prototype = new GamePrototype(player1, player2, dialog.BoardSize);
#if TEST_TIMED_GAME
                prototype.GameTimer = new GameTimer(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1));
#endif
                m_game = CreateGame(CreateGameBuilder(), prototype);
            }
        }


        private void OpenCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = m_canExecuteOpenCommand;
        }


        private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = c_defaultGameFileNameFilter,
                InitialDirectory = m_savedGameDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog(this) == true)
            {
                m_savedGameDirectory = Path.GetDirectoryName(dialog.FileName);
                GamePrototype prototype;
                try
                {
                    prototype = new GamePrototype(new FileInfo(dialog.FileName));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Invalid or missing game file: {dialog.FileName}: {ex.Message}");
                    return;
                }

                // TODO - Is it possible for the TurnStarted callback to be called prior to our adding
                //        the pending moves to the playback controller here?  That would be a problem,
                //        since the moves wouldn't be played back until *after* a player has manually
                //        made the initial move of the game.  This would undoubtedly result in an
                //        "invalid move" exception being raised during playback.
                m_game = CreateGame(CreateGameBuilder(), prototype);

                // This must be done AFTER CreateGame has been called.
                m_playbackController.AddPendingMoves(prototype.Moves);
            }
        }


        private void SaveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = m_canExecuteSaveCommand;
        }


        private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (m_saveFileName != null)
            {
                SaveCurrentGame(m_saveFileName);
            }
            else
            {
                SaveAsCommand_Executed(sender, e);
            }
        }


        private void SaveAsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = m_canExecuteSaveAsCommand;
        }


        private void SaveAsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string directory = String.Empty;
            string fileName  = c_defaultGameFileName;

            if (m_saveFileName != null)
            {
                directory = Path.GetDirectoryName(m_saveFileName);
                fileName  = Path.GetFileName(m_saveFileName);
            }

            SaveFileDialog dialog = new SaveFileDialog
            {
                InitialDirectory = directory,
                FileName         = fileName,
                DefaultExt       = c_defaultGameFileNameExtension,
                Filter           = c_defaultGameFileNameFilter
            };

            bool? result = dialog.ShowDialog(this);

            if (result == true)
            {
                m_saveFileName = dialog.FileName;
                SaveCommand_Executed(sender, e);
            }
        }


        private void CloseCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Application.Current.Shutdown();

            //
            // For some reason, the call to Shutdown sometimes closes the application window, but does
            // not end the actual TakUI.exe process, so we call Environment.Exit to force it to exit.
            //
            Environment.Exit(0);
        }


        private void CopyPtnCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        private void CopyPtnCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var writer = new StringWriter();
            string site = (m_game is HubGame) ? "STak/TakHub" : "STak/WinTak";
            PtnParser.Save(writer, m_game, site, UseVerbosePtnNotation, true, false);
            Clipboard.SetText(writer.ToString());
        }


        private void PastePtnCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;

            string text = Clipboard.ContainsText(TextDataFormat.Text)        ? Clipboard.GetText(TextDataFormat.Text)
                        : Clipboard.ContainsText(TextDataFormat.UnicodeText) ? Clipboard.GetText(TextDataFormat.UnicodeText)
                        : null;

            if (text != null)
            {
                try
                {
                    PtnParser.ParseText(text.Split(new string[] {"\n", "\r\n"}, StringSplitOptions.None));
                    e.CanExecute = true;
                }
                catch
                {
                    // Invalid PTN.
                }
            }
        }


        private void PastePtnCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string text = Clipboard.ContainsText(TextDataFormat.Text)        ? Clipboard.GetText(TextDataFormat.Text)
                        : Clipboard.ContainsText(TextDataFormat.UnicodeText) ? Clipboard.GetText(TextDataFormat.UnicodeText)
                        : null;

            if (text != null)
            {
                var prototype = new GamePrototype(text.Split(new string[] {"\n", "\r\n"}, StringSplitOptions.None));
                m_game = CreateGame(CreateGameBuilder(), prototype);
                // This must be done AFTER CreateGame has been called.
                m_playbackController.AddPendingMoves(prototype.Moves);
            }
        }


        private void UndoCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            //
            // TODO - Support Play/Pause/whatever controls to allow undoing of moves of a game between two
            //        AI players.  Normally, the player active after an undo is always a human player.
            //
            e.CanExecute = CanUndoMove();
        }


        private void UndoCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            UndoMove();
        }


        private void RedoCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = CanRedoMove();
        }


        private void RedoCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            RedoMove();
        }


        private void ConnectToHubCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ! m_takHubClient.IsConnected;
        }


        private async void ConnectToHubCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            async Task handler()
            {
                if (m_takHubClient.IsConnected)
                {
                    MessageBoxResult result = MessageBox.Show("Would you like to close the connection to TakHub?  "
                                                            + "Only a single connection can be active at one time.",
                                                      "Alert", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                    if (result == MessageBoxResult.Yes)
                    {
                        await DisconnectFromHub();
                    }
                }

                HubConnectDialog dialog = new HubConnectDialog(m_takHubClient)
                {
                    Left = this.Left + 50,
                    Top  = this.Top  + 50
                };

                if (dialog.ShowDialog() == true)
                {
                    if (m_takHubWindow == null)
                    {
                        // Open the window with the top at the level of the MainWindow's top, either on the
                        // left or right side of the MainWindow, with a small gap in between the windows.
                        // Left or right is chosen based on which side has the most available space.

                        double[] borders = this.GetBorderThickness();
                        double extraWidth = borders[0] + borders[2];

                        double screenWidth = SystemParameters.PrimaryScreenWidth;
                        double leftOffset  = (screenWidth / 2) - this.Left;
                        double rightOffset = (this.Left + this.Width) - (screenWidth / 2);

                        double width = 500; // TODO - Should be based on main window size and available space.

                        double left = (leftOffset <= rightOffset) ? (this.Left -      width) + extraWidth
                                    : (leftOffset >  rightOffset) ? (this.Left + this.Width) - extraWidth
                                    : 0.0;

                        // Keep it on the screen.
                        left = Math.Max(left, 0);
                        left = Math.Min(left, screenWidth - width);

                        m_takHubWindow = new TakHubWindow(m_takHubClient, m_gameManager)
                        {
                            Left   = left,
                            Width  = width,
                            Top    = this.Top,
                            Height = this.Height
                        };
                    }

                    OpenChatWindow();
                    m_takHubWindow.Show();
                    m_takHubWindow.Activate();
                }
            }

            await handler();
        }


        private void DisconnectFromHubCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = m_takHubClient.IsConnected;
        }


        private async void DisconnectFromHubCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            await DisconnectFromHub();
        }


        private void CancelCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = m_game != null;
        }


        private void CancelCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            m_game.CancelOperation();
        }


        private void ResetViewCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        private void ResetViewCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            m_tableView.ResetView();
        }


        private void AppearanceCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        private void AppearanceCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            AppearanceDialog dialog = new AppearanceDialog
            {
                Left = this.Left + 50,
                Top = this.Top + 50
            };
            dialog.ShowDialog();
        }


        private void EnableAudioCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        private void EnableAudioCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // Implicitly handled via the checkbox.
        }


        private void AdvancedOptionsCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        private void AdvancedOptionsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ShowAdvancedOptions();
        }


        private void ShowMoveHintCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = m_game.ActivePlayer.IsLocalHuman && ! m_game.IsCompleted && m_hintPlayer == null
                                                            && (m_playbackController.PendingUndos == 0)
                                                            && (m_playbackController.PendingRedos == 0)
                                                            && ! m_game.IsMoveInProgress
                                                            && ! MoveAnimation.IsActive
                                                            && ! (m_game is HubGame);
        }


        private async void ShowMoveHintCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            await AnimateMoveHint();
        }


        private void ShowTakHubWindowCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = m_takHubClient.IsConnected;
        }


        private void ShowTakHubWindowCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            m_takHubWindow.Show();
            m_takHubWindow.Activate();
        }


        private void ShowChatWindowCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = m_takHubClient.IsConnected;
        }


        private void ShowChatWindowCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (ChatWindowEnabled)
            {
                OpenChatWindow();
            }
            else
            {
                HideChatWindow();
            }
        }


        private void OpenChatWindow()
        {
            if (m_chatWindow == null)
            {
                double[] borders      = this.GetBorderThickness();
                double   screenHeight = SystemParameters.PrimaryScreenHeight;

                bool showWindowBorder = false; // TODO - Make this configurable?

                double borderLeft  = showWindowBorder ? 0 : borders[0];
                double borderWidth = showWindowBorder ? 0 : borders[0] + borders[2];

                double top    = this.Top   + (this.Height - borders[3]);
                double left   = this.Left  + borderLeft;
                double width  = this.Width - borderWidth;
                double Height = Math.Min((screenHeight - top) - borders[3], this.Height / 2);

                WindowStyle windowStyle = showWindowBorder ? WindowStyle.SingleBorderWindow : WindowStyle.None;
                bool allowsTransparency = ! showWindowBorder;

                m_chatWindow = new ChatWindow()
                {
                    Left               = left,
                    Top                = top,
                    Width              = width,
                    Height             = Height,
                    WindowStyle        = windowStyle,
                    AllowsTransparency = allowsTransparency,
                    HubClient          = m_takHubClient
                };

                m_chatWindow.Closing += (s, o) =>
                {
                    HideChatWindow();
                    m_chatWindow = null;
                };
            }

            m_chatWindow.Show();
            ChatWindowEnabled = true;
        }


        private void HideChatWindow()
        {
            m_chatWindow?.Hide();
            ChatWindowEnabled = false;
        }


        private void DebugLogCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        private void DebugLogCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            m_debugLogWindow.Left   = this.Left + 50;
            m_debugLogWindow.Top    = this.Top  + 40;
            m_debugLogWindow.Width  = this.Width;
            m_debugLogWindow.Height = this.Height;
            m_debugLogWindow.Show();
            m_debugLogWindow.Activate();
        }


        private void GameMoveLogCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        private void GameMoveLogCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            double[] borders = this.GetBorderThickness();

            m_gameMoveLogWindow.Left   = this.Left + this.Width - (borders[0] + borders[2]);
            m_gameMoveLogWindow.Top    = this.Top;
            m_gameMoveLogWindow.Width  = this.Width  / 3;
            m_gameMoveLogWindow.Height = this.Height / 2;
            m_gameMoveLogWindow.Show();
            this.Activate();
        }


        private void BitBoardLogCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        private void BitBoardLogCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            double[] borders = this.GetBorderThickness();

            m_bitBoardLogWindow.Left   = this.Left + this.Width - (borders[0] + borders[2]);
            m_bitBoardLogWindow.Top    = this.Top  + (this.Height / 2) - borders[3];
            m_bitBoardLogWindow.Width  = this.Width  / 3;
            m_bitBoardLogWindow.Height = this.Height / 2 + borders[3];
            m_bitBoardLogWindow.Show();
            this.Activate();
        }


        private void ProjectStatusCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        private void ProjectStatusCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string pathName = App.GetDocumentPathName(c_projectStatusDocument);
            OpenWebPage(App.ConvertPathNameToUri(pathName));
        }


        private void UserGuideCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        private void UserGuideCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string pathName = App.GetDocumentPathName(c_takUserGuideDocument);
            OpenWebPage(App.ConvertPathNameToUri(pathName));
        }


        private void AboutTakCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        private void AboutTakCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            AboutDialog dialog = new AboutDialog
            {
                Left = this.Left + 50,
                Top  = this.Top  + 50
            };
            dialog.ShowDialog();
        }


        private async void HandleMoveAnimationTimeChanged(object sender, AnimationTimeChangedEventArgs e)
        {
            async Task handler()
            {
                await m_takHubClient.SetProperty("MoveAnimationTime", e.Time.ToString());
            }
            if (m_takHubClient?.IsConnected == true)
            {
                try { await handler(); } catch { }
            }
        }


        private async void HandleGameCreated(object sender, GameCreatedEventArgs e)
        {
            s_logger.Debug("=> GameCreated");

            m_tableView.HideGameOverAnimation();

            var game = m_gameManager.GetGame(e.Prototype.Id);

            // NOTE: Kibitzing initialization is handled in HandleKibitzerAdded.
            if (game.PlayerOne.IsLocal || game.PlayerTwo.IsLocal)
            {
                // Set the table, then let the competition begin!
                await TableView.InitializeTable(game);
                await StartGame();
            }

            s_logger.Debug("<= GameCreated");
        }


        private void HandleGameStarted(object sender, GameStartedEventArgs e)
        {
            s_logger.Debug("=> GameStarted");
            m_bitBoardLogger.LogGameStarted(m_game);
            m_gameMoveLogger.LogGameStarted(m_game);
            s_logger.Debug("<= GameStarted");
        }


        private void HandleGameCompleted(object sender, GameCompletedEventArgs e)
        {
            s_logger.Debug("=> GameCompleted");

            if (m_hintPlayer == null)
            {
                m_bitBoardLogger.LogGameCompleted();
                m_gameMoveLogger.LogGameCompleted();

                AnnounceGameCompletion();

                // NOTE: DeactivateGame might return a new, unmanaged (unknown to GameManager) game instance.
                m_game = ShouldDetachGame(m_game, DetachGameUponCompletion) ? DeactivateGame(m_game) : m_game;

                // We explicitly clear pending operations here to handle the case where a PTN file was
                // opened/executed and found to contain extra moves following the game winning move.
                // We need to make sure those moves don't get executed if the player enters playback mode
                // by undoing the last move.  The next TurnStarted event will cause a pending mvve to run.
                m_playbackController.ClearPendingOperations();
            }

            s_logger.Debug("<= GameCompleted");
        }


        private void HandleTurnStarted(object sender, TurnStartedEventArgs e)
        {
            s_logger.Debug("=> TurnStarted");

            if (m_hintPlayer == null && e.PlayerId == Player.One)
            {
                m_gameMoveLogger.LogTurnStarted();
            }

            m_playbackController.ExecutePendingOperations();
            RefreshConfiguration();
            SetControlState();

            s_logger.Debug("<= TurnStarted");
        }


        private async void HandleTurnCompleted(object sender, TurnCompletedEventArgs e)
        {
            s_logger.Debug("=> TurnCompleted");

            if (m_hintPlayer != null)
            {
                await Task.Delay(HintAnimationPauseTime);
                m_game.InitiateUndo(MoveAnimationTime);
            }

            SetControlState();

            if (m_hintPlayer == null)
            {
                m_gameMoveLogger.LogTurnCompleted();
                m_bitBoardLogger.LogBitBoard();
            }

            s_logger.Debug("<= TurnCompleted");
        }


        private void HandleUndoInitiated(object sender, UndoInitiatedEventArgs e)
        {
            if (m_game.IsCompleted)
            {
                m_tableView.HideGameOverAnimation();
            }
        }


        private void HandleUndoCompleted(object sender, UndoCompletedEventArgs e)
        {
            s_logger.Debug("=> HandleUndoCompleted");

            if (m_hintPlayer != null)
            {
                s_logger.Debug($"Hint animation completed for player {m_game.Players[m_hintPlayer.Id].Name} "
                                                                + $"on behalf of player {m_hintPlayer.Name}.");
                var tracker = GetEventRaisingTracker(m_game);
                m_game.Players[m_game.LastPlayer.Id].Observe(tracker);
                m_game.ChangePlayer(m_hintPlayer);
                m_hintMove = m_game.RevertedMoves[^1];
                m_hintPlayer = null;
            }

            SetControlState();

            s_logger.Debug("<= HandleUndoCompleted");
        }


        private void HandleRedoCompleted(object sender, RedoCompletedEventArgs e)
        {
            s_logger.Debug("=> HandleRedoCompleted");

            SetControlState();

            s_logger.Debug("<= HandleRedoCompleted");
        }


        private void TakHubDisconnectHandler(object sender, EventArgs e)
        {
            AbandonCurrentGame();

            string message = "The connection to the TakHub server has been lost.";

            if (m_game is HubGame && m_game.IsStarted && ! m_game.IsCompleted)
            {
                message += " The active game has been aborted.";
            }

            MessageBox.Show(message);
        }


        private void OnGameConstructed(object sender, GameConstructedEventArgs e)
        {
            InitializeGame(e.Game);
        }


        private void OnGameDestructed(object sender, GameDestructedEventArgs e)
        {
            DestructGame(e.Game);
        }


        private static void GlobalExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                object obj = e.ExceptionObject;
                string message = obj.ToString();

                if (obj is Exception exception)
                {
                    while (exception.InnerException is not null)
                    {
                        exception = exception.InnerException ;
                    }
                    message = $"Caught exception: {exception.Message}\n\n{exception.StackTrace}";
                }

                MessageBox.Show(message);

                try { File.WriteAllText(Path.Combine(App.GetApplicationDirectory(), "WinTakError.txt"), message); }
                catch { }
            }
            catch
            {
            }
        }


        public class TrackerBuilder : ITrackerBuilder
        {
            public IGameActivityTracker BuildTracker(GamePrototype prototype, bool useMirroringGame = false,
                                                                              IDispatcher dispatcher = null)
            {
                //
                // NOTES:
                //
                // 1. When using the non-actor based local game (i.e., Game rather than ActorBasedGame or HubGame)
                //    we don't strictly need to use a dispatching tracker, as tracking calls will be made on the
                //    same UI thread on which calls to the game's methods are made.  However, we currently always
                //    use a dispatching tracker for consistency as well as robustness in the face of future code
                //    updates which would necessitate the asynchronous dispatching of calls.
                //
                // 2. Remote games require a mirroring game (and thus a mirroring tracker), while local games (both
                //    "direct" and actor-based games) do not.  There is no point in not using a mirroring game for
                //    remote games, as it would requ1ire that every game status query would result in a remote call
                //    to the hub.  A similar argument can be made against not using a mirroring game with actor-based
                //    games as well; however, while not using a mirroring game so is supported by the ActorBasedGame
                //    class, non-mirroring actor-based games are not recommended due to the performance hit.
                //

                IGameActivityTracker tracker;

                if (dispatcher == null)
                {
                    if (useMirroringGame)
                    {
                        var eventingTracker    = new EventRaisingGameActivityTracker();
                        var mirroringTracker   = new MirroringGameActivityTracker(prototype, eventingTracker);
                        eventingTracker.Game  = mirroringTracker.Game;
                        tracker = mirroringTracker;
                    }
                    else
                    {
                        tracker = new EventRaisingGameActivityTracker();
                    }
                }
                else
                {
                    if (useMirroringGame)
                    {
                        var eventingTracker    = new EventRaisingGameActivityTracker();
                        var mirroringTracker   = new MirroringGameActivityTracker(prototype, eventingTracker);
                        var dispatchingTracker = new DispatchingGameActivityTracker(dispatcher, mirroringTracker);
                        eventingTracker.Game  = mirroringTracker.Game;
                        tracker = dispatchingTracker;
                    }
                    else
                    {
                        var eventingTracker    = new EventRaisingGameActivityTracker();
                        var dispatchingTracker = new DispatchingGameActivityTracker(dispatcher, eventingTracker);
                        tracker = dispatchingTracker;
                    }
                }

                return tracker;
            }


            public EventRaisingGameActivityTracker GetEventRaisingTracker(IGame game)
            {
                IGameActivityTracker tracker = game?.Tracker;

                if (tracker is DispatchingGameActivityTracker dispatchingTracker)
                {
                    tracker = dispatchingTracker.Tracker;
                }

                return tracker as EventRaisingGameActivityTracker
                   ?? (tracker as MirroringGameActivityTracker)?.Tracker as EventRaisingGameActivityTracker;
            }


            public MirroringGameActivityTracker GetMirroringTracker(IGame game)
            {
                IGameActivityTracker tracker = game?.Tracker;

                if (tracker is DispatchingGameActivityTracker dispatchingTracker)
                {
                    tracker = dispatchingTracker.Tracker;
                }

                return tracker as MirroringGameActivityTracker;
            }
        }


        private class PlaybackController
        {
            public int         PendingUndos { get; private set; }
            public int         PendingRedos { get; private set; }
            public List<IMove> PendingMoves { get; private set; } = new List<IMove>();


            public PlaybackController(IGame game)
            {
                m_game = game;
            }


            public void AddPendingUndo()
            {
                ++PendingUndos;
            }


            public void AddPendingRedo()
            {
                ++PendingRedos;
            }


            public void AddPendingMove(IMove move)
            {
                PendingMoves.Add(move);
            }


            public void AddPendingMoves(IEnumerable<IMove> moves)
            {
                PendingMoves.AddRange(moves);
            }


            public void ClearPendingOperations()
            {
                PendingUndos = 0;
                PendingRedos = 0;
                PendingMoves.Clear();
            }


            public void ExecutePendingRedos()
            {
                if (PendingRedos > 0)
                {
                    s_logger.Debug("Initiating pending redo.");
                    --PendingRedos;
                    Task.Run(() => m_game.InitiateRedo(MoveAnimationTime));
                }
            }


            public void ExecutePendingUndos()
            {
                if (PendingUndos > 0)
                {
                    s_logger.Debug("Initiating pending undo.");
                    --PendingUndos;
                    Task.Run(() => m_game.InitiateUndo(MoveAnimationTime));
                }
            }


            public void ExecutePendingMoves()
            {
                // Note that it's possible that there's exactly one pending move after the game was completed
                // and then one or more moves were undone/redone.  This is because HandleTurnStarted may have
                // added a pending move chosen by an AI as its next move.
                if (PendingMoves.Count > 1 && m_game.WasCompleted)
                {
                    MessageBox.Show("The game has run to completion, yet there remain additional moves to "
                                  + "be executed.  This likely indicates that an invalid saved game file "
                                  + "was loaded.  These remaining moves will be ignored.", "Warning",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    PendingMoves.Clear();
                }

                if (PendingMoves.Count > 0)
                {
                    s_logger.Debug("Initiating pending move.");
                    IMove move = PendingMoves[0];
                    PendingMoves.RemoveAt(0);
                    Task.Run(() => m_game.InitiateMove(move, MoveAnimationTime));
                }
            }


            public void ExecutePendingOperations()
            {
                ExecutePendingUndos();
                ExecutePendingRedos();
                ExecutePendingMoves();
            }


            private readonly IGame m_game;
        }


        private class BrowserFinder
        {
            public static string GetDefaultBrowserPath()
            {
                string browserPath;

                string urlAssociation = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http";
                string browserPathKey = @"$BROWSER$\shell\open\command";

                try
                {
                    // Read the default browser path from the UserChoice key.
                    RegistryKey userChoiceKey = Registry.CurrentUser.OpenSubKey(urlAssociation + @"\UserChoice", false);

                    // If a user choice was not found, try machine default.
                    if (userChoiceKey == null)
                    {
                        // Read the default browser path from Windows XP registry key.
                        var browserKey = Registry.ClassesRoot.OpenSubKey(@"HTTP\shell\open\command", false);

                        if (browserKey == null)
                        {
                            // Try the Windows Vista (and newer) registry key.
                            browserKey = Registry.CurrentUser.OpenSubKey(urlAssociation, false);
                        }

                        browserPath = CleanifyBrowserPath(browserKey.GetValue(null) as string);
                        browserKey.Close();
                    }
                    else
                    {
                        // The user-defined browser choice was found.
                        string progId = (userChoiceKey.GetValue("ProgId").ToString());
                        userChoiceKey.Close();

                        // Now look up the path of the executable.
                        string concreteBrowserKey = browserPathKey.Replace("$BROWSER$", progId);
                        var subKey = Registry.ClassesRoot.OpenSubKey(concreteBrowserKey, false);
                        browserPath = CleanifyBrowserPath(subKey.GetValue(null) as string);
                        subKey.Close();
                    }
                }
                catch (Exception)
                {
                    browserPath = null;
                }

                return browserPath;
            }


            private static string CleanifyBrowserPath(string p)
            {
                return p.Split('"')[1];
            }
        }
    }
}
