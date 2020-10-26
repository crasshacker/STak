using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using NLog;
using STak.TakEngine;

namespace STak.WinTak
{
    public partial class BitBoardLogger
    {
        private IGame     m_game;
        private LogWindow m_logWindow;


        private BitBoardLogger()
        {
        }


        public BitBoardLogger(LogWindow logWindow)
        {
            m_logWindow = logWindow;
        }


        public void LogGameStarted(IGame game)
        {
            m_logWindow.AppendText("\n========== Game Started ==========\n\n");
            m_game = game;
        }


        public void LogGameCompleted()
        {
            m_logWindow.AppendText("========= Game Completed =========\n");
        }


        public void LogBitBoard()
        {
            m_logWindow.AppendText(m_game.BitBoard.ToString() + "\n");
        }
    }
}
