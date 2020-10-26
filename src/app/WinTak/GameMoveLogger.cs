using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using NLog;
using STak.TakEngine;

namespace STak.WinTak
{
    public partial class GameMoveLogger
    {
        private IGame     m_game;
        private LogWindow m_logWindow;
        private int       m_gameStartPosition;


        private GameMoveLogger()
        {
        }


        public GameMoveLogger(LogWindow logWindow)
        {
            m_logWindow = logWindow;
        }


        public void LogGameStarted(IGame game)
        {
            string text = m_logWindow.GetText();

            if (text.Length > 0 && text[^1] != '\n')
            {
                m_logWindow.AppendText("\n");
            }
            m_logWindow.AppendText("\n========== Game Started ==========\n\n");

            m_game = game;
            m_gameStartPosition = m_logWindow.GetTextLength();
        }


        public void LogGameCompleted()
        {
            UpdateGameMoveText();
            m_logWindow.AppendText("\n\n========= Game Completed =========\n");
        }


        public void LogTurnStarted()
        {
            UpdateGameMoveText();
        }


        public void LogTurnCompleted()
        {
            UpdateGameMoveText();
        }


        public void LogMove()
        {
            UpdateGameMoveText();
        }


        public void LogUndo()
        {
            UpdateGameMoveText();
        }


        public void LogRedo()
        {
            UpdateGameMoveText();
        }


        private void UpdateGameMoveText()
        {
            m_logWindow.SetText(PtnParser.FormatMoves(m_game.ExecutedMoves, null), m_gameStartPosition);
        }
    }
}
