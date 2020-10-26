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

namespace STak.WinTak
{
    /// <summary>
    /// Interaction logic for InviteGameDialog.xaml
    /// </summary>
    public partial class InviteGameDialog : Window
    {
        public InviteGameDialog()
        {
            InitializeComponent();
        }


        private void OkButtonClickHandler(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }


        public bool WillPlayAI
        {
            get
            {
                return m_willPlayAI.IsChecked == true;
            }
        }


        public int[] SeatPreference
        {
            get
            {
                ComboBoxItem item = m_seatPreference.SelectedValue as ComboBoxItem;
                string preference = item.Content.ToString();

                return preference switch
                {
                    "No preference" => new int[] { 0, 1 },
                    "Player One"    => new int[] { 0 },
                    "Player Two"    => new int[] { 1 },

                    _ => throw new Exception($"Invalid seat preference: {preference}"),
                };
            }
        }


        public int[] BoardSizes
        {
            get
            {
                List<int> boardSizeList = new List<int>();
                if (m_boardSize3.IsChecked == true) { boardSizeList.Add(3); }
                if (m_boardSize4.IsChecked == true) { boardSizeList.Add(4); }
                if (m_boardSize5.IsChecked == true) { boardSizeList.Add(5); }
                if (m_boardSize6.IsChecked == true) { boardSizeList.Add(6); }
                if (m_boardSize7.IsChecked == true) { boardSizeList.Add(7); }
                if (m_boardSize8.IsChecked == true) { boardSizeList.Add(8); }
                return boardSizeList.ToArray();
            }
        }


        public string[] Opponents
        {
            get
            {
                StringSplitOptions options = StringSplitOptions.RemoveEmptyEntries;
                return m_opponents.Text.Split(new char[] {','}, options).Select(s => s.Trim()).ToArray();
            }
        }


        public HubGameType GameType
        {
            get
            {
                // TODO - Support ranked games.
                var visibility = (m_allowKibitz.IsChecked == true) ? HubGameType.Public
                                                                   : HubGameType.Private;
                return new HubGameType(visibility | HubGameType.Unranked);
            }
        }
    }
}
