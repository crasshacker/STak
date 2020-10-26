using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using STak.TakHub.Interop;

namespace STak.WinTak
{
    public partial class ChatView : UserControl
    {
        private const string c_prompt = "";

        private WinTakHubClient m_hubClient;


        public ChatView()
        {
            InitializeComponent();
            m_chatBox.Text += "\n\n";
            m_inputBox.Text = c_prompt;
        }


        public WinTakHubClient HubClient
        {
            get
            {
                return m_hubClient;
            }

            set
            {
                if (m_hubClient != null && m_hubClient != value)
                {
                    m_hubClient.Tracker.ChatMessage -= ChatMessageHandler;
                }

                if (value != null)
                {
                    m_hubClient = value;
                    m_hubClient.Tracker.ChatMessage += ChatMessageHandler;
                }
                else
                {
                    m_hubClient = null;
                }
            }
        }


        private void ChatMessageHandler(object sender, ChatMessageEventArgs e)
        {
            m_chatBox.Text += $"[{e.Sender}] {e.Message}\n";
            m_chatBox.CaretIndex = m_chatBox.Text.Length;
            m_chatBox.ScrollToEnd();
        }


        public async Task SendMessage(string target, string message)
        {
            if (m_hubClient.IsConnected)
            {
                await m_hubClient.Chat(Guid.Empty, target, message);
                m_inputBox.Text = c_prompt;
            }
        }


        protected override async void OnKeyDown(KeyEventArgs e)
        {
            Func<Task> handler = async () =>
            {
                if (e.Key == Key.Enter || e.Key == Key.Return)
                {
                    string target    = "TABLE";
                    string message   = m_inputBox.Text;
                    string separator = "::";

                    int colon = message.IndexOf(separator);
                    if (colon != -1)
                    {
                        string[] userNames = message.Substring(c_prompt.Length, colon)
                                                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(u => u.Trim())
                                                    .Distinct()
                                                    .ToArray();
                        message = message.Substring(colon + separator.Length);
                        target = String.Join(',', userNames);
                    }

                    await SendMessage(target, message);
                    e.Handled = true;
                }
            };
            await handler();
        }
    }
}
