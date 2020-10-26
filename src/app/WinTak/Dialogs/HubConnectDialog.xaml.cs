using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using STak.TakEngine;
using STak.TakHub.Client;
using STak.TakHub.Client.Trackers;

namespace STak.WinTak
{
    public partial class HubConnectDialog : Window
    {
        private readonly GameHubClient m_hubClient;
        private          bool          m_allowClose;


        public HubConnectDialog()
        {
            InitializeComponent();

            PreviewKeyDown += new KeyEventHandler(EscapeKeyHandler);
        }


        public HubConnectDialog(GameHubClient hubClient)
            : this()
        {
            m_hubClient = hubClient;
        }


        private async void OkButtonClickHandler(object sender, RoutedEventArgs e)
        {
            Func<Task> handler = async () =>
            {
                m_allowClose = false;

                string takHubUrl  = $"http://{m_hostname.Text}:{m_port.Text}/takhub";
                string gameHubUrl = $"{takHubUrl}/gamehub";

                Uri takHubUri, gameHubUri;

                try
                {
                    takHubUri  = new Uri(takHubUrl);
                    gameHubUri = new Uri(gameHubUrl);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Invalid hostname or port: ${ex.Message}", "Error", MessageBoxButton.OK,
                                                                                            MessageBoxImage.Error);
                    DialogResult = false;
                    return;
                }

                if (m_register.IsChecked == true)
                {
                    if (m_password.Password != m_repeatPw.Password)
                    {
                        MessageBox.Show(this, "The two passwords do not match.", "Error", MessageBoxButton.OK,
                                                                                        MessageBoxImage.Error);
                        DialogResult = false;
                        return;
                    }
                }

                if ((m_password.Password == String.Empty)
                 || (m_repeatPw.Password == String.Empty && m_register.IsChecked == true))
                {
                    MessageBox.Show(this, "Password must not be empty.", "Error", MessageBoxButton.OK,
                                                                                  MessageBoxImage.Error);
                    DialogResult = false;
                    return;
                }

                string userName = m_userName.Text;
                string password = m_password.Password;
                string email    = m_email.Text;

                if (m_register.IsChecked == true)
                {
                    var authenticator = new Authenticator(takHubUri, userName, password);
                    Exception ex = await authenticator.RegisterUser(email);

                    if (ex != null)
                    {
                        MessageBox.Show(this, $"Failed to register user \"{userName}\": {ex.Message}", "Error",
                                                                   MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                try
                {
                    if (m_hubClient.IsConnected)
                    {
                        await m_hubClient.Disconnect();
                    }

                    Authenticator authenticator = new Authenticator(takHubUri, userName, password);
                    await m_hubClient.Connect(gameHubUri, authenticator);
                    // TODO - MoveAnimation.GetAnimationSpeed doesn't give us what we want;
                    //        we need a property we can query for the current speed/time/rate.
                    // await m_takHubClient.SetProperty("MoveAnimationTime", animationTime);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Error: {ex.Message}", "Warning", MessageBoxButton.OK,
                                                                        MessageBoxImage.Warning);
                }

                if (m_hubClient.IsConnected)
                {
                    m_allowClose = true;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    string message = "Login failed: Invalid username or password.";

                    // TODO - This matching against a string is fragile.  What can be done about it?
                    if (! Regex.IsMatch(m_hubClient.ConnectException.Message, @"\b401\b +\(?Unauthorized\)?"))
                    {
                        message = (m_hubClient.ConnectException != null)
                            ? $"An error occurred while connecting to the server at {takHubUri}:\n\n"
                                                              + m_hubClient.ConnectException.Message
                            : $"An unknown error occurred while connecting to the server at {takHubUri}";
                    }

                    MessageBox.Show(this, message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    DialogResult = false;
                }
            };
            await handler();
        }


        private void EscapeKeyHandler(object obj, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                m_allowClose = true;
                Close();
            }
        }


        private void CancelButtonClickHandler(object sender, RoutedEventArgs e)
        {
            m_allowClose = true;
            Close();
        }


        private void WindowClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = ! m_allowClose;
        }
    }
}
