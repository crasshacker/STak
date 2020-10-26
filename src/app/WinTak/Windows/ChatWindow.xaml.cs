using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using STak.TakHub.Client;

namespace STak.WinTak
{
    public partial class ChatWindow : StickyTakWindow
    {
        public WinTakHubClient HubClient { get => m_chatView.HubClient;
                                           set => m_chatView.HubClient = value; }


        public ChatWindow()
        {
            InitializeComponent();
        }
    }
}
