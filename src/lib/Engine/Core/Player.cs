using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Security.Permissions;
using NLog;
using STak.TakEngine.Trackers;
using STak.TakEngine.AI;

namespace STak.TakEngine
{
    [Serializable]
    public class Player
    {
        private static readonly NLog.Logger s_logger = LogManager.GetCurrentClassLogger();

        //
        // TODO - Although one could argue that animation speed is a property of a player, and thus belongs here,
        //        it seems really ugly (especially since the setting is static, and thus applies to all AI players).
        //
        private static readonly int               DefaultMoveAnimationTime = 1000; // One second to cross the board.
        public  static          Func<Player, int> MoveAnimationTime { get; set; } = (p) => DefaultMoveAnimationTime;

        public const int None = -1;
        public const int One  =  0;
        public const int Two  =  1;

        public int      Id       { get; private  set; }
        public string   Name     { get; private  set; }
        public ITakAI   AI       { get; private  set; }
        public bool     WasAI    { get; private  set; }
        public bool     IsAI     { get; internal set; }
        public bool     IsPaused { get; private  set; }
        public bool     IsRemote { get; set; }

        public bool     IsLocal => ! IsRemote;
        public bool     IsHuman => ! IsAI;

        public bool     IsLocalHuman  => IsLocal  && IsHuman;
        public bool     IsRemoteHuman => IsRemote && IsHuman;
        public bool     IsLocalAI     => IsLocal  && IsAI;
        public bool     IsRemoteAI    => IsRemote && IsAI;

        private CancellationTokenSource m_cancelTokenSource;
        private bool                    m_cancelled;

        protected IBasicGame Game { get; set; }


        // For MessagePack.
        public Player()
        {
        }


        public Player(string playerName, bool isRemote = false)
            : this(playerName, null, isRemote)
        {
        }


        public Player(string playerName, ITakAI ai, bool isRemote = false)
        {
            Id       = Player.None;
            Name     = playerName;
            IsRemote = isRemote;
            IsAI     = ai != null;
            WasAI    = false;
            AI       = ai;
        }


        public Player(Player player)
        {
            if (player == null)
            {
                throw new ArgumentException("Player copy constructor cannot take null as an argument.");
            }

            Id       = player.Id;
            Name     = player.Name;
            IsRemote = player.IsRemote;
            IsPaused = player.IsPaused;
            IsAI     = player.IsAI;
            WasAI    = player.WasAI;
            AI       = player.AI;
        }


        public void Humanize(string name)
        {
            if (IsAI || IsRemote)
            {
                Name     = name;
                AI       = null;
                IsAI     = false;
                WasAI    = true;
                IsRemote = false;
            }
        }


        public void Join(IBasicGame game, int playerId)
        {
            Game = game;
            Id   = playerId;
        }


        public void Observe(IEventBasedGameActivityTracker tracker)
        {
            if (IsAI)
            {
                tracker.TurnStarted    += HandleTurnStarted;
                tracker.UndoCompleted  += HandleUndoCompleted;
                tracker.RedoCompleted  += HandleRedoCompleted;
                tracker.CurrentTurnSet += HandleCurrentTurnSet;
            }
        }


        public void Unobserve(IEventBasedGameActivityTracker tracker)
        {
            if (IsAI)
            {
                tracker.TurnStarted    -= HandleTurnStarted;
                tracker.UndoCompleted  -= HandleUndoCompleted;
                tracker.RedoCompleted  -= HandleRedoCompleted;
                tracker.CurrentTurnSet -= HandleCurrentTurnSet;
            }
        }


        public async Task<IMove> ChooseMove()
        {
            if (IsHuman)
            {
                throw new Exception("ChooseMove cannot be called for a human player.");
            }

            Debug.Assert(m_cancelTokenSource == null, "Attempt to choose move multiple times simultaneously.");

            int maxThinkingTime = AI.Options.MaximumThinkingTime;

            IMove move = null;

            while (move == null)
            {
                try
                {
                    m_cancelled = false;
                    m_cancelTokenSource = new CancellationTokenSource();

                    s_logger.Info($"Searching for move, timeout is {maxThinkingTime}ms.");
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    if (maxThinkingTime > 0)
                    {
                        m_cancelTokenSource.CancelAfter(maxThinkingTime);
                    }

                    move = await SearchForMove(AI, Game as IGame, m_cancelTokenSource.Token);

                    stopwatch.Stop();
                    s_logger.Info($"Player {Id+1} search complete after {stopwatch.Elapsed.ToString()} seconds.");
                }
                catch (OperationCanceledException)
                {
                    s_logger.Warn("Move search was canceled before it could begin.");
                    maxThinkingTime *= 2; // Don't cancel so early the next time.
                }
                finally
                {
                    m_cancelTokenSource.Dispose();
                    m_cancelTokenSource = null;
                }
            }

            return move;
        }


        public void StopThinking()
        {
            if (m_cancelTokenSource != null && ! m_cancelled)
            {
                m_cancelTokenSource.Cancel();
                m_cancelled = true;
            }
        }


        private static Task<IMove> SearchForMove(ITakAI ai, IGame game, CancellationToken token)
        {
            // Pass a copy of the game so that the AI can't intentionally or inadvertently affect the state.
            return Task<IMove>.Run(() => { return ai.ChooseNextMove(new BasicGame(game), token); }, token);
        }


        private void HandleUndoCompleted(object sender, UndoCompletedEventArgs e)
        {
            if (IsAI && Game.ActivePlayer.Id == Id)
            {
                // This AI player just undid a move, so we need to pause it so that the
                // next TurnStarted event won't cause the AI to immediately move again.
                IsPaused = true;
            }
        }


        private void HandleRedoCompleted(object sender, RedoCompletedEventArgs e)
        {
            if (IsAI && Game.ActivePlayer.Id == Id)
            {
                // We're an AI and it's our turn to make a move, but a move was just redone
                // so we're in "redoing mode".  Suppress the AI from automatically making a
                // move.  It's expected that the UI will be calling RedoMove to redo our move.
                IsPaused = true;
            }
        }


        private void HandleCurrentTurnSet(object sender, CurrentTurnSetEventArgs e)
        {
            if (IsAI && Game.ActivePlayer.Id == Id && e.Stones.Length > 0)
            {
                // The current turn is being set explicitly; disable this AI for one turn.
                IsPaused = true;
            }
        }


        private async void HandleTurnStarted(object sender, TurnStartedEventArgs e)
        {
            Func<Task> handler = async () =>
            {
                if (IsLocalAI && ! IsPaused && e.PlayerId == Id)
                {
                    // The zero here says to use UI's animation speed setting.
                    // Also boy is this conversion to IGame ugly.  Sigh...
                    (Game as IGame).InitiateMove(Id, await ChooseMove(), MoveAnimationTime(this));
                }
                IsPaused = false;
            };
            await handler();
        }
    }
}
