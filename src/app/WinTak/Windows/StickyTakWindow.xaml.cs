using System;
using System.Windows;
using System.Windows.Controls;
using StickyWindows;

namespace STak.WinTak
{
    public partial class StickyTakWindow : Window
    {
        protected StickyWindow m_stickyWindow;


        public StickyTakWindow()
        {
            this.Loaded += (s, e) => m_stickyWindow = App.MakeStickyWindow(this);
        }
    }
}
