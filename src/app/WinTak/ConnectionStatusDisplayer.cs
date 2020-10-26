using System;
using System.Threading;

namespace STak.WinTak
{
    public class ConnectionStatusDisplayer : IStatusDisplayer
    {
        private readonly ConnectionStatusWindow m_statusWindow;


        public ConnectionStatusDisplayer(ConnectionStatusWindow statusWindow)
        {
            m_statusWindow = statusWindow;
        }


        public void SetCancellerSource(CancellationTokenSource cancellerSource)
        {
            m_statusWindow.CancellerSource = cancellerSource;
        }


        public void Show(string statusText)
        {
            m_statusWindow.Dispatcher.InvokeAsync(() =>
            {
                m_statusWindow.SetText(statusText);
                m_statusWindow.Show();
                m_statusWindow.Activate();
            });
        }


        public void Hide()
        {
            m_statusWindow.Dispatcher.InvokeAsync(() =>
            {
                m_statusWindow.SetText();
                m_statusWindow.Hide();
            });
        }
    }
}
