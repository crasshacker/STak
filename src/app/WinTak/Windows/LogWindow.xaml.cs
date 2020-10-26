using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.ComponentModel;
using NLog;
using NLog.Config;
using NLog.Targets;
using STak.TakEngine;

namespace STak.WinTak
{
    public partial class LogWindow : StickyTakWindow
    {
        private readonly IDispatcher m_dispatcher;


        private LogWindow()
        {
        }


        public LogWindow(IDispatcher dispatcher, string name, string title)
        {
            InitializeComponent();
            m_dispatcher = dispatcher;
            Title = title;
            Name = name;
        }


        public void AppendText(char text)
        {
            AppendText(text.ToString());
        }


        public void AppendText(string text)
        {
            if (m_dispatcher.IsDispatchNeeded)
            {
                m_dispatcher.InvokeAsync(() =>
                {
                    m_content.Text += text;
                    m_scrollViewer.ScrollToBottom();
                });
            }
            else
            {
                m_content.Text += text;
                m_scrollViewer.ScrollToBottom();
            }
        }


        public string GetText()
        {
            return m_content.Text;
        }


        public void SetText(string text, int position = 0)
        {
            if (m_dispatcher.IsDispatchNeeded)
            {
                m_dispatcher.InvokeAsync(() =>
                {
                    m_content.Text = m_content.Text.Substring(0, position);
                    AppendText(text);
                });
            }
            else
            {
                m_content.Text = m_content.Text.Substring(0, position);
                AppendText(text);
            }
        }


        public int GetTextLength()
        {
            return m_content.Text.Length;
        }


        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}
