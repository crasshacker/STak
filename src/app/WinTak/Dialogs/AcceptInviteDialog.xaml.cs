using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using STak.TakHub.Interop;
using STak.TakEngine;

namespace STak.WinTak
{
    /// <summary>
    /// Interaction logic for AcceptInviteDialog.xaml
    /// </summary>
    public partial class AcceptInviteDialog : Window
    {
        private readonly GameInvite m_invite;

        public GameInvite AcceptableInvite { get; private set; }


        public AcceptInviteDialog()
        {
            InitializeComponent();
            this.Loaded += LoadedEventHandler;
        }


        public AcceptInviteDialog(GameInvite invite)
            : this()
        {
            m_invite = invite;
            m_welcome.Content = ((string) m_welcome.Content).Replace("{inviter}", invite.Inviter);
        }


        private void OkButtonClickHandler(object sender, RoutedEventArgs e)
        {
            bool? quit = MainWindow.Instance.AskWhetherToQuitCurrentGame();

            if (quit == true)
            {
                MainWindow.Instance.QuitCurrentGame();
            }

            if (quit == true || quit == null)
            {
                GameInvite invite = new();
                invite.Opponent.Add(m_invite.Inviter);
                invite.PlayerNumber.Add(GetPreferredSeat());
                invite.BoardSize.Add(GetPreferredBoardSize());
                invite.WillPlayAI = m_invite.IsInviterAI;
                invite.IsInviterAI = false;

#if TEST_TIMED_GAME
                // FIXIT - TODO - Remove this.
                invite.TimeLimit = (5, 15);
                invite.Increment = (1, 1);
#endif

                AcceptableInvite = invite;
                DialogResult = true;
            }
        }


        private int GetPreferredSeat()
        {
            ComboBoxItem item = m_seatPreference.SelectedValue as ComboBoxItem;
            string seat = item.Content as string;

            return (seat == "Player One") ? Player.One : Player.Two;
        }

        private int GetPreferredBoardSize()
        {
            if      (m_boardSize3.IsChecked == true) { return 3; }
            else if (m_boardSize4.IsChecked == true) { return 4; }
            else if (m_boardSize5.IsChecked == true) { return 5; }
            else if (m_boardSize6.IsChecked == true) { return 6; }
            else if (m_boardSize7.IsChecked == true) { return 7; }
            else if (m_boardSize8.IsChecked == true) { return 8; }
            else                                     { return 0; }
        }


        private void LoadedEventHandler(object sender, RoutedEventArgs e)
        {
            bool anyBoardSize = ! m_invite.BoardSize.Any();

            m_boardSize3.IsEnabled = anyBoardSize || m_invite.BoardSize.Contains(3);
            m_boardSize4.IsEnabled = anyBoardSize || m_invite.BoardSize.Contains(4);
            m_boardSize5.IsEnabled = anyBoardSize || m_invite.BoardSize.Contains(5);
            m_boardSize6.IsEnabled = anyBoardSize || m_invite.BoardSize.Contains(6);
            m_boardSize7.IsEnabled = anyBoardSize || m_invite.BoardSize.Contains(7);
            m_boardSize8.IsEnabled = anyBoardSize || m_invite.BoardSize.Contains(8);

            if      (m_boardSize3.IsEnabled) { m_boardSize3.IsChecked = true; }
            else if (m_boardSize4.IsEnabled) { m_boardSize4.IsChecked = true; }
            else if (m_boardSize5.IsEnabled) { m_boardSize5.IsChecked = true; }
            else if (m_boardSize6.IsEnabled) { m_boardSize6.IsChecked = true; }
            else if (m_boardSize7.IsEnabled) { m_boardSize7.IsChecked = true; }
            else if (m_boardSize8.IsEnabled) { m_boardSize8.IsChecked = true; }

            if ((m_invite.PlayerNumber.Contains(0) && m_invite.PlayerNumber.Contains(1))
                                                       || ! m_invite.PlayerNumber.Any())
            {
                var item = new ComboBoxItem
                {
                    Content = "Player One"
                };
                m_seatPreference.Items.Add(item);

                item = new ComboBoxItem
                {
                    Content = "Player Two"
                };
                m_seatPreference.Items.Add(item);
            }
            else if (m_invite.PlayerNumber.Contains(0))
            {
                var item = new ComboBoxItem
                {
                    Content = "Player Two"
                };
                m_seatPreference.Items.Add(item);
            }
            else if (m_invite.PlayerNumber.Contains(1))
            {
                var item = new ComboBoxItem
                {
                    Content = "Player One"
                };
                m_seatPreference.Items.Add(item);
            }
        }
    }
}
