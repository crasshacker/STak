using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
// using ColorPickerWPF;
using Xceed.Wpf.Toolkit;

namespace STak.WinTak
{
    public partial class AppearanceDialog : Window
    {
        private const string c_imageFileNameFilter = "Image Files|*.png;*.jpg;*.gif;*.bmp|All Files|*.*";
        private static string s_savedImageDirectory;

        private readonly Scheme m_initialScheme;
        private Scheme m_currentScheme;


        private Color BackgroundColor =>
            OperatingSystem.IsWindowsVersionAtLeast(7, 0) && m_colorPicker.SelectedColor.HasValue
                                             ? (Color) m_colorPicker.SelectedColor : Colors.White;

        private static TableView TableView => MainWindow.Instance.TableView;


        public AppearanceDialog()
        {
            InitializeComponent();

            m_initialScheme = Scheme.Current.Clone();
            m_currentScheme = Scheme.Current.Clone();

            if (OperatingSystem.IsWindowsVersionAtLeast(7, 0))
            {
                m_colorPicker.SelectedColorChanged += ColorChangedEventHandler;
            }
            this.Closing += ClosingEventHandler;

            UpdateControls();
        }


        private void ColorChangedEventHandler(object source, EventArgs e)
        {
            m_currentScheme.BackgroundColor = BackgroundColor;
            TableView.ApplyScheme(m_currentScheme);
        }


        private void ChooseBoardTextureClickHandler(object sender, RoutedEventArgs e)
        {
            string fileName = GetImageFileNameFromUser();

            if (fileName != null)
            {
                s_savedImageDirectory = Path.GetDirectoryName(fileName);
                m_currentScheme.BoardTextureFile = fileName;
                TableView.ApplyScheme(m_currentScheme);
                m_boardTexture.Text = fileName;
            }
        }


        private void ChooseP1StoneTextureClickHandler(object sender, RoutedEventArgs e)
        {
            string fileName = GetImageFileNameFromUser();

            if (fileName != null)
            {
                s_savedImageDirectory = Path.GetDirectoryName(fileName);
                m_currentScheme.P1StoneTextureFile = fileName;
                TableView.ApplyScheme(m_currentScheme);
                m_p1StoneTexture.Text = fileName;
            }
        }


        private void ChooseP2StoneTextureClickHandler(object sender, RoutedEventArgs e)
        {
            string fileName = GetImageFileNameFromUser();

            if (fileName != null)
            {
                s_savedImageDirectory = Path.GetDirectoryName(fileName);
                m_currentScheme.P2StoneTextureFile = fileName;
                TableView.ApplyScheme(m_currentScheme);
                m_p2StoneTexture.Text = fileName;
            }
        }


        private void ResetButtonClickHandler(object sender, RoutedEventArgs e)
        {
            m_currentScheme = m_initialScheme.Clone();
            TableView.ApplyScheme(m_currentScheme);
            UpdateControls();
        }


        private void DefaultButtonClickHandler(object sender, RoutedEventArgs e)
        {
            m_currentScheme = Scheme.Default.Clone();
            TableView.ApplyScheme(m_currentScheme);
            UpdateControls();
        }


        private void OkButtonClickHandler(object sender, RoutedEventArgs e)
        {
            Scheme.Current = m_currentScheme;
            TableView.ApplyScheme(m_currentScheme);
            DialogResult = true;
        }


        private void CancelButtonClickHandler(object sender, RoutedEventArgs e)
        {
            m_currentScheme = m_initialScheme.Clone();
            TableView.ApplyScheme(m_currentScheme);
            DialogResult = true;
        }


        private void ClosingEventHandler(object source, CancelEventArgs e)
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(7, 0))
            {
                m_colorPicker.SelectedColorChanged -= ColorChangedEventHandler;
            }
        }


        private void UpdateControls()
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(7, 0))
            {
                m_colorPicker.SelectedColor         = m_currentScheme.BackgroundColor;
                m_boardTexture.Text                 = m_currentScheme.BoardTextureFile;
                m_p1StoneTexture.Text               = m_currentScheme.P1StoneTextureFile;
                m_p2StoneTexture.Text               = m_currentScheme.P2StoneTextureFile;
            }
        }


        private string GetImageFileNameFromUser()
        {
            OpenFileDialog dialog = new()
            {
                Filter = c_imageFileNameFilter,
                InitialDirectory = s_savedImageDirectory ?? App.GetImageDirectoryName()
            };
            return dialog.ShowDialog(this) == true ? dialog.FileName : null;
        }
    }
}
