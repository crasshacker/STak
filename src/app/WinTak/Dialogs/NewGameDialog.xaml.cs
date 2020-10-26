using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using STak.TakEngine;
using STak.TakEngine.AI;

namespace STak.WinTak
{
    public enum PlayerType
    {
        None,
        Human,
        AI
    }


    public partial class NewGameDialog : Window
    {
        private static PlayerType   s_player1Type;
        private static PlayerType   s_player2Type;
        private static string       s_player1Name;
        private static string       s_player2Name;
        private static string       s_player1AIName;
        private static string       s_player2AIName;
        private static int          s_player1AIIndex;
        private static int          s_player2AIIndex;
        private static int          s_boardSizeIndex = -1;

        private readonly List<string> m_takAINames;

        public int          BoardSize     => Int32.Parse(m_boardSize.Text);
        public PlayerType   Player1Type   => (PlayerType) Enum.Parse(typeof(PlayerType), m_player1Type.Text);
        public PlayerType   Player2Type   => (PlayerType) Enum.Parse(typeof(PlayerType), m_player2Type.Text);
        public string       Player1AIName => m_player1AIName.SelectedItem.ToString();
        public string       Player2AIName => m_player2AIName.SelectedItem.ToString();
        public string       Player1Name   => m_player1Name.Text;
        public string       Player2Name   => m_player2Name.Text;
        public List<string> Player1TakAI  => m_takAINames;
        public List<string> Player2TakAI  => m_takAINames;


        public NewGameDialog(Player player1, Player player2)
        {
            InitializeComponent();
            DataContext = this;

            if (player1 != null && player2 != null)
            {
                s_player1Name = player1.Name;
                s_player2Name = player2.Name;
                // If a player was originally an AI and was Humanized at game completion, restore its AI-ness.
                s_player1Type = (player1.IsHuman && ! player1.WasAI) ? PlayerType.Human : PlayerType.AI;
                s_player2Type = (player2.IsHuman && ! player2.WasAI) ? PlayerType.Human : PlayerType.AI;
            }

            if (s_player1Type    != PlayerType.None) { m_player1Type.Text        = s_player1Type.ToString(); }
            if (s_player2Type    != PlayerType.None) { m_player2Type.Text        = s_player2Type.ToString(); }
            if (s_player1Name    != null           ) { m_player1Name.Text        = s_player1Name;            }
            if (s_player2Name    != null           ) { m_player2Name.Text        = s_player2Name;            }
            if (s_player1AIName  != null           ) { m_player1AIName.Text      = s_player1AIName;          }
            if (s_player2AIName  != null           ) { m_player2AIName.Text      = s_player2AIName;          }
            if (s_boardSizeIndex != -1             ) { m_boardSize.SelectedIndex = s_boardSizeIndex;         }

            m_takAINames = new List<string>(TakAI.GetAINames());

            if (m_takAINames.Count > 0)
            {
                m_player1AIName.SelectedIndex = s_player1AIIndex;
                m_player2AIName.SelectedIndex = s_player2AIIndex;
            }
        }


        private void ChangePlayer1Type(object sender, SelectionChangedEventArgs e)
        {
            if (m_player1Name != null && m_player1AIName != null)
            {
                ComboBoxItem item = (sender as ComboBox).SelectedItem as ComboBoxItem;
                string selection = item.Content.ToString();

                if (selection == "Human")
                {
                    m_player1Name.Text = Environment.UserName;
                    m_player1Name.SetValue(Grid.ZIndexProperty, 2);
                    m_player1AIName.SetValue(Grid.ZIndexProperty, 1);
                    m_player1Name.IsTabStop = true;
                    m_player1AIName.IsTabStop = false;
                }
                else
                {
                    m_player1Name.SetValue(Grid.ZIndexProperty, 1);
                    m_player1AIName.SetValue(Grid.ZIndexProperty, 2);
                    m_player1Name.IsTabStop = false;
                    m_player1AIName.IsTabStop = true;
                }
            }
        }


        private void ChangePlayer2Type(object sender, SelectionChangedEventArgs e)
        {
            if (m_player2Name != null && m_player2AIName != null)
            {
                ComboBoxItem item = (sender as ComboBox).SelectedItem as ComboBoxItem;
                string selection = item.Content.ToString();

                if (selection == "Human")
                {
                    m_player2Name.Text = Environment.UserName;
                    m_player2Name.SetValue(Grid.ZIndexProperty, 2);
                    m_player2AIName.SetValue(Grid.ZIndexProperty, 1);
                    m_player2Name.IsTabStop = true;
                    m_player2AIName.IsTabStop = false;
                }
                else
                {
                    m_player2Name.SetValue(Grid.ZIndexProperty, 1);
                    m_player2AIName.SetValue(Grid.ZIndexProperty, 2);
                    m_player2Name.IsTabStop = false;
                    m_player2AIName.IsTabStop = true;
                }
            }
        }


        private void OkButtonClickHandler(object sender, RoutedEventArgs e)
        {
            s_player1Type    = Player1Type;
            s_player2Type    = Player2Type;
            s_player1Name    = Player1Name;
            s_player2Name    = Player2Name;
            s_player1AIName  = Player1AIName;
            s_player2AIName  = Player2AIName;
            s_player1AIIndex = m_player1AIName.SelectedIndex;
            s_player2AIIndex = m_player2AIName.SelectedIndex;
            s_boardSizeIndex = m_boardSize.SelectedIndex;

            DialogResult = true;
        }
    }
}
