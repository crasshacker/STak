using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Reflection;

namespace STak.WinTak
{
    public class JumpSlider : Slider
    {
        private Thumb   m_thumb;
        private ToolTip m_autoToolTip;

        public  string     AutoToolTipFormat { get; set; }
        public  Visibility ThumbVisibility   { get => m_thumb.Visibility; set => m_thumb.Visibility = value; }
        public  bool       IsThumbVisible    { get => ThumbVisibility == Visibility.Visible;
                                               set => ThumbVisibility = value ? Visibility.Visible : Visibility.Hidden; }


        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (m_thumb != null)
            {
                m_thumb.MouseEnter -= MouseEnteredThumbEventHandler;
            }
            m_thumb = (GetTemplateChild("PART_Track") as Track).Thumb;
            if (m_thumb != null)
            {
                m_thumb.MouseEnter += MouseEnteredThumbEventHandler;
            }
        }


        protected override void OnThumbDragStarted(DragStartedEventArgs e)
        {
            base.OnThumbDragStarted(e);
            FormatAutoToolTipContent();
        }


        protected override void OnThumbDragDelta(DragDeltaEventArgs e)
        {
            base.OnThumbDragDelta(e);
            FormatAutoToolTipContent();
        }


        private ToolTip AutoToolTip
        {
            get
            {
                if (m_autoToolTip == null)
                {
                    var field = typeof(Slider).GetField("_autoToolTip", BindingFlags.NonPublic
                                                                      | BindingFlags.Instance);
                    m_autoToolTip = field.GetValue(this) as ToolTip;
                }
                return m_autoToolTip;
            }
        }


        private void FormatAutoToolTipContent()
        {
            if (! string.IsNullOrEmpty(AutoToolTipFormat))
            {
                AutoToolTip.Content = String.Format(AutoToolTipFormat, AutoToolTip.Content);
            }
        }


        private void MouseEnteredThumbEventHandler(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // The left button is pressed on mouse enter so the thumb must have been moved under the
                // mouse in response to a click on the track.  Generate a MouseLeftButtonDown event.
                MouseButtonEventArgs args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left)
                {
                    RoutedEvent = MouseLeftButtonDownEvent
                };
                (sender as Thumb).RaiseEvent(args);
            }
        }
    }
}
