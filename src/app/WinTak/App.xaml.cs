using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.Configuration;
using NLog.Extensions.Logging;
using NLog;
using StickyWindows.WPF;
using StickyWindows;

using Size = System.Drawing.Size;

namespace STak.WinTak
{
    public partial class App : Application
    {
        private static int  EventHorizon   => UIAppConfig.StickyWindow.EventHorizon;
        private static bool StickToScreen  => UIAppConfig.StickyWindow.StickToScreen;
        private static bool StickToWindow  => UIAppConfig.StickyWindow.StickToWindow;
        private static bool StickOnResize  => UIAppConfig.StickyWindow.StickOnResize;
        private static bool StickOnMove    => UIAppConfig.StickyWindow.StickOnMove;


        public void App_Startup(object sender, StartupEventArgs e)
        {
            // TODO - Show a spash screen or progress display of some sort.
        }


        public static void InitializeLogging()
        {
            string configFile = Path.Combine(GetApplicationDirectory(), "uiappsettings.json");
            var config = new ConfigurationBuilder().AddJsonFile(configFile, true, true).Build();
            LogManager.Configuration = new NLogLoggingConfiguration(config.GetSection("nlog"));
        }


        public static string GetApplicationDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }


        public static string GetResourcesDirectory()
        {
            return Path.Combine(GetApplicationDirectory(), "Resources");
        }


        public static string GetImageDirectoryName()
        {
            return Path.Combine(GetResourcesDirectory(), "Images");
        }


        public static string GetImagePathName(string fileName)
        {
            return Path.IsPathRooted(fileName) ? fileName : Path.Combine(GetImageDirectoryName(), fileName);
        }


        public static string GetSoundDirectoryName()
        {
            return Path.Combine(GetResourcesDirectory(), "Sounds");
        }


        public static string GetSoundPathName(string fileName)
        {
            return Path.IsPathRooted(fileName) ? fileName : Path.Combine(GetSoundDirectoryName(), fileName);
        }


        public static string GetModelDirectoryName()
        {
            return Path.Combine(GetResourcesDirectory(), "Models");
        }


        public static string GetModelPathName(string fileName)
        {
            return Path.IsPathRooted(fileName) ? fileName : Path.Combine(GetModelDirectoryName(), fileName);
        }


        public static string GetDocumentDirectoryName()
        {
            return Path.Combine(GetResourcesDirectory(), "Documents");
        }


        public static string GetDocumentPathName(string fileName)
        {
            return Path.IsPathRooted(fileName) ? fileName : Path.Combine(GetDocumentDirectoryName(), fileName);
        }


        public static string GetFontDirectoryName()
        {
            return Path.Combine(GetResourcesDirectory(), "Fonts");
        }


        public static string GetFontPathName(string fileName)
        {
            return Path.IsPathRooted(fileName) ? fileName : Path.Combine(GetFontDirectoryName(), fileName);
        }


        public static string ConvertPathNameToUri(string pathName)
        {
            return "file:///" + Regex.Replace(pathName, @"\\", "/");
        }


        public static (double, double) GetDisplayScalingFactors()
        {
            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Static;
            var dpiXProperty = typeof(SystemParameters).GetProperty("DpiX", bindingFlags);
            var dpiYProperty = typeof(SystemParameters).GetProperty("Dpi",  bindingFlags);

            int dpiX = (int) dpiXProperty.GetValue(null, null);
            int dpiY = (int) dpiYProperty.GetValue(null, null);

            // A DPI of 96 is 100% (1-to-1 scaling).
            return (dpiX / 96.0, dpiY / 96.0);
        }


        public static StickyWindow MakeStickyWindow(Window window)
        {
            // Default behavior is to allow window to be moved by clicking the mouse in the window's client area
            // and then dragging, but only if both a Shift and a Control key are down.  Note that we don't really
            // need to pass both left and right key specifiers, since the StickyWindow code doesn't differentiate.
            return MakeStickyWindow(window, new Key[] { Key.LeftShift, Key.RightShift, Key.LeftCtrl, Key.RightCtrl });
        }


        public static StickyWindow MakeStickyWindow(Window window, Key[] keys)
        {
            var (scaleX, scaleY) = GetDisplayScalingFactors();

            // TODO - Rather than naively using the larger of the two values we should use either the X or the Y
            //        scale depending on whichever edge of a window is being checked for "stuckness" at the time.
            //        NOTE: I don't know that it's even possible to instruct Windows to scale X and Y differently.
            int distance = (int) (EventHorizon * Math.Max(scaleX, scaleY));

            var stickyWindow = window.CreateStickyWindow(StickyWindowType.Sticky);

            if (keys != null)
            {
                StickyWindow.ModifierKey modifiers = StickyWindow.ModifierKey.None;
                if (keys.Contains(Key.LeftCtrl))   { modifiers |= StickyWindow.ModifierKey.Control; }
                if (keys.Contains(Key.RightCtrl))  { modifiers |= StickyWindow.ModifierKey.Control; }
                if (keys.Contains(Key.LeftShift))  { modifiers |= StickyWindow.ModifierKey.Shift;   }
                if (keys.Contains(Key.RightShift)) { modifiers |= StickyWindow.ModifierKey.Shift;   }
                stickyWindow.ClientAreaMoveKey = modifiers;
            }

            stickyWindow.Stickiness    = distance;
            stickyWindow.StickToOther  = StickToWindow;
            stickyWindow.StickToScreen = StickToScreen;
            stickyWindow.StickOnResize = StickOnResize;
            stickyWindow.StickOnMove   = StickOnMove;

            stickyWindow.Stick();

            return stickyWindow;
        }


        protected override void OnStartup(StartupEventArgs e) 
        {
            // Select the text in a TextBox when it receives the focus.

            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler(SelectivelyIgnoreMouseButton));
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotKeyboardFocusEvent, 
                new RoutedEventHandler(SelectAllText));
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.MouseDoubleClickEvent,
                new RoutedEventHandler(SelectAllText));
            base.OnStartup(e); 
        }


        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // TODO - What should we do here?
            // e.Handled = true; // Prevent default unhandled exception processing.
        }


        private void SelectivelyIgnoreMouseButton(object sender, MouseButtonEventArgs e)
        {
            DependencyObject parent = e.OriginalSource as UIElement;

            while (parent != null && ! (parent is TextBox))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            if (parent != null)
            {
                var textBox = (TextBox)parent;

                // If the text box is not yet focused, give it the focus and
                // stop further processing of this click event.
                if (! textBox.IsKeyboardFocusWithin)
                {
                    textBox.Focus();
                    e.Handled = true;
                }
            }
        }

        private void SelectAllText(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }
    }
}
