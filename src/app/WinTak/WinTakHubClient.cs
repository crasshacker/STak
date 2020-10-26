using System;
using System.Windows;
using System.Threading;
using System.Threading.Tasks;
using STak.TakEngine;
using STak.TakEngine.Management;
using STak.TakHub.Client;
using STak.TakHub.Client.Trackers;
using STak.TakHub.Interop;

namespace STak.WinTak
{
    public class WinTakHubClient : GameHubClient
    {
        private readonly IStatusDisplayer m_statusDisplayer;
        private readonly IDispatcher      m_dispatcher;
        private          HubClientState   m_state;

        public event EventHandler Connected;
        public event EventHandler Disconnected;


        public WinTakHubClient(GameManager gameManager, IEventBasedHubActivityTracker tracker, IDispatcher dispatcher,
                                               IStatusDisplayer statusDisplayer = null, HubGameOptions options = null)
            : base(gameManager, tracker, options)
        {
            m_dispatcher      = dispatcher;
            m_statusDisplayer = statusDisplayer;
            m_state           = HubClientState.Disconnected;
        }


        public async Task Connect(Uri gameHubUri, Authenticator authenticator)
        {
            using var cancellerSource = new CancellationTokenSource();
            m_statusDisplayer.SetCancellerSource(cancellerSource);
            await base.Connect(gameHubUri, authenticator, cancellerSource.Token);
        }


        protected void OnConnected()
        {
            m_dispatcher.Invoke(() => Connected?.Invoke(this, EventArgs.Empty));
        }


        protected void OnDisconnected()
        {
            m_dispatcher.Invoke(() => Disconnected?.Invoke(this, EventArgs.Empty));
        }


        public override void OnStateChange(HubClientState state)
        {
            base.OnStateChange(state);

            switch (state)
            {
              case HubClientState.Connecting:     HandleConnecting();    break;
              case HubClientState.Connected:      HandleConnected();     break;
              case HubClientState.Reconnecting:   HandleReconnecting();  break;
              case HubClientState.Reconnected:    HandleReconnected();   break;
              case HubClientState.Disconnecting:  HandleDisconnecting(); break;
              case HubClientState.Disconnected:   HandleDisconnected();  break;
            }

            m_state = state;
        }


        private void HandleConnecting()
        {
            m_statusDisplayer.Show("Connecting to TakHub server...");
        }


        private void HandleConnected()
        {
            m_statusDisplayer.Hide();
            OnConnected();
        }


        private void HandleReconnecting()
        {
            m_statusDisplayer.Show("Reconnecting to TakHub server...");
        }


        private void HandleReconnected()
        {
            m_statusDisplayer.Hide();
            OnConnected();
        }


        private void HandleDisconnecting()
        {
            m_statusDisplayer.Show("Disconnecting from TakHub server...");
        }


        private async void HandleDisconnected()
        {
            Func<Task> handler = async () =>
            {
                // Only inform the user of unexpected disconnections.
                if (m_state == HubClientState.Connected || m_state == HubClientState.Reconnected)
                {
                    OnDisconnected();
                }

                m_statusDisplayer.Show("Connection closed.");
                await Task.Delay(500);
                m_statusDisplayer.Hide();
            };
            await handler();
        }
    }
}
