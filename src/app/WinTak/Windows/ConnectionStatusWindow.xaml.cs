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

namespace STak.WinTak
{
    public partial class ConnectionStatusWindow : Window
    {
        private readonly Window m_parentWindow;
        private bool            m_closing;

        public CancellationTokenSource CancellerSource { get; set; }


        public ConnectionStatusWindow()
        {
            InitializeComponent();
        }


        public ConnectionStatusWindow(Window parentWindow)
            : this()
        {
            m_parentWindow = parentWindow;
        }


        public new void Show()
        {
            if (! m_closing)
            {
                PositionWindow();
                base.Show();
            }
        }


        public void SetText(string text = null)
        {
            m_statusText.Text = text ?? "";
        }


        private void PositionWindow()
        {
            Left = m_parentWindow.Left + (m_parentWindow.Width/2)  - (Width/2);
            Top  = m_parentWindow.Top  + (m_parentWindow.Height/2) - (Height/2);
        }


        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }


        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            SetText("Cancelling...");
            CancellerSource?.Cancel();
            this.Hide();
        }


        protected override void OnClosing(CancelEventArgs e)
        {
            m_closing = true;
        }
    }
}
