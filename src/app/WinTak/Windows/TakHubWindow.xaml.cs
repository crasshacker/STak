using System;
using System.Linq;
using System.Globalization;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NodaTime;
using NLog;
using STak.TakEngine;
using STak.TakEngine.Management;
using STak.TakHub.Interop;
using STak.TakHub.Client;

namespace STak.WinTak
{
    public partial class TakHubWindow : StickyTakWindow
    {
        private static readonly NLog.Logger s_logger = LogManager.GetCurrentClassLogger();

        private readonly WinTakHubClient             m_hubClient;
        private readonly ActiveGameDescriptionList   m_activeGameList;
        private readonly ActiveInviteDescriptionList m_activeInviteList;
        private readonly GameManager                 m_gameManager;

        // FIXIT - This is a horrible hack to allow IValueConverter classes to know the username.  We'd like to
        //         pass this in as the parameter to the Convert method, but I can't figure out how to do so.
        public static string UserName { get; private set; }

        public static readonly DependencyProperty CanUserInviteGameProperty;


        static TakHubWindow()
        {
            TakHubWindow.CanUserInviteGameProperty = DependencyProperty.Register("CanUserInviteGame", typeof(bool),
                                                         typeof(TakHubWindow), new FrameworkPropertyMetadata(false,
                                                         null));
        }


        public TakHubWindow()
        {
            InitializeComponent();
            DataContext = this;
        }


        public TakHubWindow(WinTakHubClient client, GameManager gameManager)
            : this()
        {
            m_hubClient = client ?? throw new ArgumentException("The hub client cannot be null.", nameof(client));
            m_gameManager = gameManager;

            UserName = m_hubClient.UserName;

            m_activeGameList   = (ActiveGameDescriptionList) Resources["activeGames"];
            m_activeInviteList = (ActiveInviteDescriptionList) Resources["activeInvites"];

            m_hubClient.Tracker.InviteAdded     += InviteAdded;
            m_hubClient.Tracker.InviteRemoved   += InviteRemoved;
            m_hubClient.Tracker.GameAdded       += GameAdded;
            m_hubClient.Tracker.GameRemoved     += GameRemoved;
            m_hubClient.Tracker.GameAbandoned   += GameAbandoned;
            m_hubClient.Tracker.KibitzerAdded   += KibitzerAdded;
            m_hubClient.Tracker.KibitzerRemoved += KibitzerRemoved;

            m_hubClient.Connected    += TakHubConnectedHandler;
            m_hubClient.Disconnected += TakHubDisconnectedHandler;

            this.Loaded += LoadedEventHandler;
        }


        public bool CanUserInviteGame
        {
            get { return (bool) GetValue(TakHubWindow.CanUserInviteGameProperty);        }
            set {               SetValue(TakHubWindow.CanUserInviteGameProperty, value); }
        }


        internal static bool CanKibitzGame(ActiveGameDescription gameDesc)
        {
            // FIXIT - Kibitzing itself works, but call to the client to create the game doesn't carry
            //         the AllowKibitz flag with it; the server doesn't keep track of it properly.
            //         For now rather than disallowing kibitzing we allow if for everyone.
            return // gameDesc.AllowKibitz &&
                      gameDesc.PlayerOne != TakHubWindow.UserName &&
                      gameDesc.PlayerTwo != TakHubWindow.UserName;
        }


        private async Task DisconnectFromHub(bool askFirst = true)
        {
            if (m_hubClient.IsConnected)
            {
                bool disconnect = true;

                if (askFirst && ! MainWindow.Instance.IsWindowClosing)
                {
                    var result = MessageBox.Show(this, "Would you like to disconnect from the TakHub server?", "Alert",
                                                                  MessageBoxButton.YesNo, MessageBoxImage.Information);
                    disconnect = (result == MessageBoxResult.Yes);
                }

                if (disconnect)
                {
                    await MainWindow.Instance.DisconnectFromHub();
                    TakHubDisconnectedHandler(this, EventArgs.Empty);
                }
            }
        }


        protected override async void OnClosing(CancelEventArgs e)
        {
            async Task handler()
            {
                if (IsVisible)
                {
                    await DisconnectFromHub();
                    e.Cancel = true;
                    this.Hide();
                }
            }
            await handler();
        }


        private async void ActiveGamesDoubleClickHandler(object sender, MouseEventArgs e)
        {
            async Task handler()
            {
                await KibitzSelectedGame();
            }
            await handler();
        }


        private async void ActiveInvitesDoubleClickHandler(object sender, MouseEventArgs e)
        {
            async Task handler()
            {
                await AcceptSelectedInvite();
            }
            await handler();
        }


        private async void KibitzSelectedButtonHandler(object sender, RoutedEventArgs e)
        {
            async Task handler()
            {
                await KibitzSelectedGame();
            }
            await handler();
        }


        private async void AcceptSelectedButtonHandler(object sender, RoutedEventArgs e)
        {
            async Task handler()
            {
                await AcceptSelectedInvite();
            }
            await handler();
        }


        private async void AcceptOldestButtonHandler(object sender, RoutedEventArgs e)
        {
            async Task handler()
            {
                GameInvite oldestInvite = null;
                Instant    oldestCreate = Instant.MaxValue;

                bool isHumanWaiting = m_activeInviteList.Where(inv => (inv.PlayerName != UserName && ! inv.IsPlayerAI)).Any();

                foreach (var inviteDesc in m_activeInviteList)
                {
                    if (inviteDesc.PlayerName != UserName && ((isHumanWaiting && ! inviteDesc.IsPlayerAI) || ! isHumanWaiting))
                    {
                        if (inviteDesc.Invite.CreateTime < oldestCreate)
                        {
                            oldestInvite = inviteDesc.Invite;
                            oldestCreate = inviteDesc.Invite.CreateTime;
                        }
                    }
                }

                if (oldestInvite != null)
                {
                    await AcceptInvite(oldestInvite);
                }
            }
            await handler();
        }


        private async void InviteNewButtonHandler(object sender, RoutedEventArgs e)
        {
            async Task handler()
            {
                var dialog = new InviteGameDialog()
                {
                    Left = this.Left + 50,
                    Top  = this.Top  + 50
                };

                if (dialog.ShowDialog() == true)
                {
                    var invite = new GameInvite(dialog.Opponents, dialog.SeatPreference, dialog.BoardSizes,
                                                                false, dialog.WillPlayAI, dialog.GameType);
                    await m_hubClient.InviteGame(invite);
                }
            }
            await handler();
        }


        private async void CloseButtonHandler(object sender, RoutedEventArgs e)
        {
            async Task handler()
            {
                await DisconnectFromHub();
                this.Hide();
            }
            await handler();
        }


        private async Task KibitzSelectedGame()
        {
            if (m_activeGames.SelectedItem is ActiveGameDescription gameDesc && CanKibitzGame(gameDesc))
            {
                await MainWindow.Instance.KibitzGame(gameDesc.Prototype);
            }
        }


        private async Task<bool> AcceptInvite(GameInvite invite)
        {
            bool accepted = false;

            var dialog = new AcceptInviteDialog(invite)
            {
                Left = this.Left + 50,
                Top  = this.Top  + 50
            };

            if (dialog.ShowDialog() == true)
            {
                invite = dialog.AcceptableInvite;
                await m_hubClient.InviteGame(invite);
                accepted = true;
            }

            return accepted;
        }


        private async Task<bool> AcceptSelectedInvite()
        {
            bool accepted = false;

            if (m_activeInvites.SelectedItem is ActiveInviteDescription inviteDesc && ! inviteDesc.IsUserInviter)
            {
                accepted = await AcceptInvite(inviteDesc.Invite);
            }

            return accepted;
        }


        private void GameListFilter(object sender, FilterEventArgs e)
        {
            e.Accepted = true;
        }


        private void InviteListFilter(object sender, FilterEventArgs e)
        {
            e.Accepted = true;
        }


        private void InviteAdded(object sender, InviteAddedEventArgs e)
        {
            if ((e.Invite.Inviter == m_hubClient.UserName) || (! e.Invite.Opponent.Any())
                                     || e.Invite.Opponent.Contains(m_hubClient.UserName))
            {
                bool isUserInviter = m_hubClient.UserName == e.Invite.Inviter;
                m_activeInviteList.Add(new ActiveInviteDescription(isUserInviter, e.Invite));
                CanUserInviteGame = ! m_activeInviteList.IsUserInviter;
            }

            m_acceptOldestGame.IsEnabled = m_activeInviteList.Where(inv => (inv.PlayerName != UserName && ! inv.IsPlayerAI)).Any();
        }


        private void InviteRemoved(object sender, InviteRemovedEventArgs e)
        {
            bool isUserInviter = m_hubClient.UserName == e.Invite.Inviter;
            m_activeInviteList.Remove(new ActiveInviteDescription(isUserInviter, e.Invite));
            CanUserInviteGame = ! m_activeInviteList.IsUserInviter;

            m_acceptOldestGame.IsEnabled = m_activeInviteList.Where(inv => (inv.PlayerName != UserName && ! inv.IsPlayerAI)).Any();
        }


        private void GameAdded(object sender, GameAddedEventArgs e)
        {
            m_activeGameList.Add(new ActiveGameDescription(e.Prototype, e.GameType));
        }


        private void GameAbandoned(object sender, GameAbandonedEventArgs e)
        {
            if (e.AbandonerName != m_hubClient.UserName)
            {
                var game = m_gameManager.GetGame(e.GameId);
                if (game != null)
                {
                    if (e.AbandonerName == game.PlayerOne.Name
                     || e.AbandonerName == game.PlayerTwo.Name)
                    {
                        MainWindow.Instance.AlertGameAbandoned(e.GameId, e.AbandonerName);
                    }
                }
            }
        }


        private void GameRemoved(object sender, GameRemovedEventArgs e)
        {
            try
            {
                m_activeGameList.Remove(m_activeGameList.Where(g => g.GameId == e.GameId).Single());
            }
            catch (Exception ex)
            {
                s_logger.Error($"Cannot remove nonexistent active game with Id={e.GameId}: {ex}");
            }
        }


        private void KibitzerAdded(object sender, KibitzerAddedEventArgs e)
        {
            if (m_hubClient.UserName == e.KibitzerName)
            {
                // TODO?
            }
        }


        private void KibitzerRemoved(object sender, KibitzerRemovedEventArgs e)
        {
            if (m_hubClient.UserName == e.KibitzerName)
            {
                // TODO?
            }
        }


        private async void TakHubConnectedHandler(object sender, EventArgs e)
        {
            async Task handler()
            {
                // They should already be clear, but just in case...
                m_activeInviteList.Clear();
                m_activeGameList.Clear();

                await m_hubClient.RequestActiveInvites();
                await m_hubClient.RequestActiveGames();
            }
            await handler();
        }


        private void TakHubDisconnectedHandler(object sender, EventArgs e)
        {
            MainWindow.Instance.Invoke(() =>
            {
                m_activeInviteList.Clear();
                m_activeGameList.Clear();
                this.Hide();
            });
        }


        private async void LoadedEventHandler(object sender, RoutedEventArgs e)
        {
            async Task handler()
            {
                await m_hubClient.RequestActiveInvites();
                await m_hubClient.RequestActiveGames();
            }
            await handler();
        }
    }


    public class ActiveGameDescriptionList : ObservableCollection<ActiveGameDescription>
    {
    }


    public class ActiveInviteDescriptionList : ObservableCollection<ActiveInviteDescription>
    {
        public bool IsUserInviter => this.Where(a => a.IsUserInviter).Any();
    }


    public class ActiveGameDescription : IEquatable<ActiveGameDescription>
    {
        public GamePrototype Prototype { get; set; }
        public HubGameType   GameType  { get; set; }

        public Guid   GameId               => Prototype.Id;
        public int    BoardSize            => Prototype.BoardSize;
        public string PlayerOne            => Prototype.PlayerOne.Name;
        public string PlayerTwo            => Prototype.PlayerTwo.Name;
        public bool   IsPlayerOneAI        => Prototype.PlayerOne.IsAI;
        public bool   IsPlayerTwoAI        => Prototype.PlayerTwo.IsAI;
        public bool   AllowKibitz          => GameType.IsPublic;
        public string BoardSizeDescription => $"{BoardSize}x{BoardSize}";


        public ActiveGameDescription(GamePrototype prototype, HubGameType gameType)
        {
            Prototype = prototype;
            GameType  = gameType;
        }


        public bool Equals(ActiveGameDescription desc)
        {
            if (desc == null)
            {
                return false;
            }

            return GameId == desc.GameId;
        }


        public override bool Equals(object obj)
            => obj is ActiveGameDescription desc && Equals(desc);


        public override int GetHashCode()
        {
            return HashCode.Combine(GameId, BoardSize, PlayerOne, PlayerTwo);
        }
    }


    public class ActiveInviteDescription : IEquatable<ActiveInviteDescription>
    {
        public bool        IsUserInviter { get; set; }
        public GameInvite  Invite        { get; set; }
        public Guid        Id            { get; set; }
        public string      PlayerName    { get; set; }
        public bool        IsPlayerAI    { get; set; }
        public int[]       BoardSizes    { get; set; }
        public int[]       OpenSeats     { get; set; }
        public string[]    Opponents     { get; set; }
        public bool        AllowKibitz   { get; set; }
        public HubGameType GameType      { get; set; }
        public Instant     CreateTime    { get; set; }


        public ActiveInviteDescription(bool isUserInviter, GameInvite invite)
        {
            IsUserInviter = isUserInviter;
            Invite        = invite.Clone();
            Id            = invite.Id;
            PlayerName    = invite.Inviter;
            IsPlayerAI    = invite.IsInviterAI;
            BoardSizes    = invite.BoardSize.ToArray();
            Opponents     = invite.Opponent.ToArray();
            GameType      = invite.GameType;
            CreateTime    = invite.CreateTime;
            OpenSeats     = invite.PlayerNumber.ToArray();
            AllowKibitz   = invite.GameType.IsPublic;
        }


        public bool Equals(ActiveInviteDescription desc)
        {
            return desc != null && Id == desc.Id;
        }


        public override bool Equals(object obj)
            => obj is ActiveInviteDescription desc && Equals(desc);


        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Invite, CreateTime);
        }
    }


    [ValueConversion(typeof(object), typeof(bool))]
    public class SelectedGameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is ActiveGameDescription gameDesc && TakHubWindow.CanKibitzGame(gameDesc);
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("SelectedGameConverter.ConvertBack not supported.");
        }
    }


    [ValueConversion(typeof(object), typeof(bool))]
    public class SelectedInviteConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && ! (value as ActiveInviteDescription).IsUserInviter;
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("SelectedInviteConverter.ConvertBack not supported.");
        }
    }


    [ValueConversion(typeof(object), typeof(int[]))]
    public class SeatPreferenceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int[] seats = (int[]) value;
            return ((seats.Contains(Player.One) && seats.Contains(Player.Two)) || ! seats.Any()) ? "P1,P2"
                                                : (seats.Contains(Player.Two))                   ? "P1"
                                                                                                 : "P2";
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((string)value).Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => Int32.Parse(s)-1);
        }
    }


    [ValueConversion(typeof(object), typeof(int[]))]
    public class BoardSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int[] sizes = (value != null && ((int[])value).Any()) ? (int[])value : Board.Sizes;
            return String.Join(",", sizes.OrderBy(s => s).Select(s => s.ToString()));
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((string)value).Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => Int32.Parse(s));
        }
    }
}
