using System;
using System.Timers;
using System.Diagnostics;
using NLog;

namespace STak.TakEngine
{
    [Serializable]
    public class GameTimer : IDisposable
    {
        private static readonly NLog.Logger s_logger = LogManager.GetCurrentClassLogger();

        public  static readonly GameTimer Unlimited = new GameTimer(TimeSpan.MaxValue, TimeSpan.MaxValue);

        private int         m_activePlayerId;
        private Stopwatch[] m_stopwatches;

        [NonSerialized]
        private readonly Timer m_playTimer;

        public TimeSpan     GameLimit  { get; }
        public TimeSpan     Increment  { get; }
        public TimeSpan[]   TimeLimits { get; }

        public bool StartTimerOnFirstMove { get; }

        public bool IsExpired => m_playTimer.Interval         == Int32.MaxValue // This is set when the timer fires.
                              || GetRemainingTime(Player.One) <= TimeSpan.Zero
                              || GetRemainingTime(Player.Two) <= TimeSpan.Zero;

        public event ElapsedEventHandler GameTimerExpired;


        public GameTimer(TimeSpan gameLimit, TimeSpan increment, bool startOnFirstMove = true,
                                                                  TimeSpan[] remaining = null)
        {
            GameLimit = gameLimit;
            Increment = increment;

            StartTimerOnFirstMove = startOnFirstMove;

            TimeLimits = new TimeSpan [2];
            TimeLimits[Player.One] = gameLimit;
            TimeLimits[Player.Two] = gameLimit;

            m_activePlayerId = Player.None;

            m_stopwatches = new Stopwatch[2];
            m_stopwatches[Player.One] = new Stopwatch();
            m_stopwatches[Player.Two] = new Stopwatch();

            m_playTimer = new Timer { AutoReset = false };

            // The Unlimited game never raises events, so don't add a handler.
            if (gameLimit < TimeSpan.MaxValue && increment < TimeSpan.MaxValue)
            {
                m_playTimer.Elapsed += (s, e) => GameTimerHandler(s, e);
                m_playTimer.Elapsed += (s, e) => GameTimerExpired?.Invoke(s, e);
            }

            if (remaining != null)
            {
                TimeLimits[Player.One] = remaining[Player.One];
                TimeLimits[Player.Two] = remaining[Player.Two];
            }
        }


        public GameTimer Clone()
        {
            GameTimer gameTimer = null;

            if (GameLimit != Unlimited.GameLimit)
            {
                // NOTE: We do not clone the current state; the GameTimer takes on its initial state,
                //       with both clocks set to their initial time limit and neither of them running.

                gameTimer = new GameTimer(GameLimit, Increment);
                gameTimer.TimeLimits[Player.One] = TimeLimits[Player.One];
                gameTimer.TimeLimits[Player.Two] = TimeLimits[Player.Two];

                gameTimer.m_stopwatches = new Stopwatch[2];
                gameTimer.m_stopwatches[Player.One] = new Stopwatch();
                gameTimer.m_stopwatches[Player.Two] = new Stopwatch();

                gameTimer.m_playTimer.Elapsed += (s, e) => GameTimerHandler(s, e);
                gameTimer.m_playTimer.Elapsed += (s, e) => GameTimerExpired?.Invoke(s, e);
            }
            else
            {
                gameTimer = this;
            }

            return gameTimer;
        }


        public void Start()
        {
            if (GameLimit != Unlimited.GameLimit)
            {
                if (! StartTimerOnFirstMove)
                {
                    m_activePlayerId = Player.One;
                    m_stopwatches[m_activePlayerId].Start();
                    m_playTimer.Interval = GetRemainingTime(m_activePlayerId).TotalMilliseconds;
                    m_playTimer.Start();
                }
            }
        }


        public void PunchClock(int playerId)
        {
            if (GameLimit != Unlimited.GameLimit && ! IsExpired)
            {
                Stopwatch stopwatch;

                if (StartTimerOnFirstMove && ! m_playTimer.Enabled)
                {
                    Debug.Assert(playerId == Player.One);
                    m_activePlayerId = playerId;
                }
                else
                {
                    Debug.Assert(playerId == m_activePlayerId);
                    stopwatch = m_stopwatches[m_activePlayerId];
                    s_logger.Debug($"Stopping timer for player {m_activePlayerId} - Elapsed: {stopwatch.Elapsed}");

                    stopwatch.Stop();
                    TimeLimits[m_activePlayerId] += Increment;
                }

                m_activePlayerId = 1 - m_activePlayerId;
                stopwatch = m_stopwatches[m_activePlayerId];
                stopwatch.Start();

                m_playTimer.Interval = Math.Max(1.0, GetRemainingTime(m_activePlayerId).TotalMilliseconds);

                s_logger.Debug($"Starting timer for player {m_activePlayerId} - Elapsed: {stopwatch.Elapsed}");
                s_logger.Debug($"Timer interval set for {m_playTimer.Interval}.");

                if (StartTimerOnFirstMove)
                {
                    m_playTimer.Start();
                }
            }
        }


        public void Pause()
        {
            // TODO
        }


        public void Resume()
        {
            // TODO
        }


        public void End()
        {
            if (GameLimit != Unlimited.GameLimit)
            {
                if (m_activePlayerId != Player.None)
                {
                    m_activePlayerId = Player.None;
                    m_stopwatches[Player.One].Stop();
                    m_stopwatches[Player.Two].Stop();
                    m_playTimer.Interval = Int32.MaxValue;
                    m_playTimer.Stop();
                }
            }
        }


        public TimeSpan GetRemainingTime(int playerId)
            => TimeLimits[playerId] - m_stopwatches[playerId].Elapsed;


        private void GameTimerHandler(object source, ElapsedEventArgs e)
        {
            s_logger.Debug($"Game timer expired during player {m_activePlayerId+1}'s turn.");
            s_logger.Debug($"Player 1 time used: {m_stopwatches[Player.One].Elapsed}.");
            s_logger.Debug($"Player 2 time used: {m_stopwatches[Player.Two].Elapsed}.");

            End();
        }


        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_playTimer.Dispose();
            }
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
