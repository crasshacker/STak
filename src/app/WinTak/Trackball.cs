//
// Based on Trackball.cs in the WPF Samples on GitHub (https://github.com/microsoft/WPF-Samples).
//
// Original copyright:
//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using STak.TakEngine;

namespace STak.WinTak
{
    public enum TrackballEventType
    {
        MouseUp,
        MouseDown,
        MouseMove,
        MouseWheel,
        KeyUp,
        KeyDown
    }


    public class Trackball
    {
        // The state of the trackball.
        private bool              m_isEnabled;
        private Point             m_point;
        private bool              m_rotating;
        private Quaternion        m_rotation;
        private Quaternion        m_rotationDelta;
        private double            m_scaleDelta;

        // The state of the current drag.
        private bool             m_scaling;
        private List<Viewport3D> m_viewports;
        private Vector3D         m_translate;
        private Vector3D         m_translateDelta;

        // Configurable filter.
        private Func<TrackballEventType, object, EventArgs, bool> m_eventHandlingFilter;

        private static double BoardRotationDelta => UIAppConfig.Appearance.BoardRotationDelta;


        public Trackball()
        {
            Reset();
        }


        public List<Viewport3D> Viewports
        {
            get { return m_viewports ?? (m_viewports = new List<Viewport3D>()); }
            set { m_viewports = value; }
        }


        public bool IsEnabled
        {
            get { return m_isEnabled && (m_viewports?.Count > 0); }
            set { m_isEnabled = value; }
        }


        public Func<TrackballEventType, object, EventArgs, bool> EventHandlingFilter
        {
            get { return m_eventHandlingFilter;  }
            set { m_eventHandlingFilter = value; }
        }


        public void Attach(FrameworkElement element)
        {
            element.PreviewKeyDown      += PreviewKeyDownHandler;
            element.MouseMove           += MouseMoveHandler;
            element.MouseLeftButtonDown += MouseDownHandler;
            element.MouseLeftButtonUp   += MouseUpHandler;
            element.MouseWheel          += MouseWheelHandler;
        }


        public void Detach(FrameworkElement element)
        {
            element.PreviewKeyDown      -= PreviewKeyDownHandler;
            element.MouseMove           -= MouseMoveHandler;
            element.MouseLeftButtonDown -= MouseDownHandler;
            element.MouseLeftButtonUp   -= MouseUpHandler;
            element.MouseWheel          -= MouseWheelHandler;
        }


        public void Zoom(double delta, bool reset = false)
        {
            if (reset)
            {
                Reset(delta);
            }
            else
            {
                m_scaleDelta = delta;
                UpdateViewports(m_rotation, m_scaleDelta, m_translate);
            }
        }


        public void Reset(bool reverse = false)
        {
            Reset(1.0, reverse);
        }


        public void SetScale(double delta)
        {
            UpdateViewports(m_rotation, delta, m_translate);
        }


        public void SetRotation(Quaternion quaternion)
        {
            UpdateViewports(quaternion, m_scaleDelta, m_translate);
        }


        public void SetTranslation(Vector3D vector)
        {
            UpdateViewports(m_rotation, m_scaleDelta, vector);
        }


        private void Reset(double scaleDelta, bool reverse = false)
        {
            m_rotation = new Quaternion(new Vector3D(0,1,0), (reverse ? 180 : 0));
            m_translate.X = 0;
            m_translate.Y = 0;
            m_translate.Z = 0;
            m_translateDelta.X = 0;
            m_translateDelta.Y = 0;
            m_translateDelta.Z = 0;

            // Clear delta too, because if reset is called because of a double click then the mouse
            // up handler will also be called and this way it won't do anything.
            m_rotationDelta = Quaternion.Identity;
            m_scaleDelta = scaleDelta;
            UpdateViewports(m_rotation, m_scaleDelta, m_translate);
        }


        private bool IsAllowedByFilter(TrackballEventType eventType, object sender, EventArgs eventArgs)
        {
            return IsEnabled && (m_eventHandlingFilter == null || m_eventHandlingFilter(eventType, sender, eventArgs));
        }


        // Updates the matrices of the viewports using the rotation quaternion.
        private void UpdateViewports(Quaternion q, double s, Vector3D t)
        {
            if (m_viewports != null)
            {
                foreach (var i in m_viewports)
                {
                    var mv = i.Children[0] as ModelVisual3D;
                    var t3Dg = mv.Transform as Transform3DGroup;
                    var groupScaleTransform = t3Dg.Children[0] as ScaleTransform3D;
                    var groupRotateTransform = t3Dg.Children[1] as RotateTransform3D;
                    var groupTranslateTransform = t3Dg.Children[2] as TranslateTransform3D;
                    groupScaleTransform.ScaleX = s;
                    groupScaleTransform.ScaleY = s;
                    groupScaleTransform.ScaleZ = s;
                    groupRotateTransform.Rotation = new AxisAngleRotation3D(q.Axis, q.Angle);
                    groupTranslateTransform.OffsetX = t.X;
                    groupTranslateTransform.OffsetY = t.Y;
                    groupTranslateTransform.OffsetZ = t.Z;
                }
            }
        }


        private void MouseMoveHandler(object sender, MouseEventArgs e)
        {
            if (! IsAllowedByFilter(TrackballEventType.MouseMove, sender, e))
            {
                return;
            }

            var el = (UIElement) sender;

            if (el.IsMouseCaptured)
            {
                var delta = m_point - e.MouseDevice.GetPosition(el);
                delta /= 2;
                var q = m_rotation;

                if (m_rotating)
                {
                    // We can redefine this 2D mouse delta as a 3D mouse delta
                    // where "into the screen" is Z
                    var mouse = new Vector3D(delta.X, -delta.Y, 0);
                    var axis = Vector3D.CrossProduct(mouse, new Vector3D(0, 0, 1));
                    var len = axis.Length;
                    if (len < 0.00001 || m_scaling)
                    {
                        m_rotationDelta = new Quaternion(new Vector3D(0, 0, 1), 0);
                    }
                    else
                    {
                        m_rotationDelta = new Quaternion(axis, len);
                    }

                    q = m_rotationDelta*m_rotation;
                }
                else
                {
                    delta /= 20;
                    m_translateDelta.X = delta.X*-1;
                    m_translateDelta.Y = delta.Y;
                }

                var t = m_translate + m_translateDelta;

                UpdateViewports(q, m_scaleDelta, t);
                e.Handled = true;
            }
        }


        private void MouseDownHandler(object sender, MouseButtonEventArgs e)
        {
            if (! IsAllowedByFilter(TrackballEventType.MouseDown, sender, e))
            {
                return;
            }

            var el = (UIElement) sender;
            m_point = e.MouseDevice.GetPosition(el);
            m_scaling = (e.MiddleButton == MouseButtonState.Pressed);
            m_rotating = Keyboard.IsKeyDown(Key.Space) == false;

            el.CaptureMouse();
            // e.Handled = true;
        }


        private void MouseUpHandler(object sender, MouseButtonEventArgs e)
        {
            if (! IsAllowedByFilter(TrackballEventType.MouseUp, sender, e))
            {
                return;
            }

            // Stuff the current initial + delta into initial so when we next move we
            // start at the right place.
            if (m_rotating)
            {
                m_rotation = m_rotationDelta*m_rotation;
            }
            else
            {
                m_translate += m_translateDelta;
                m_translateDelta.X = 0;
                m_translateDelta.Y = 0;
            }

            var el = (UIElement) sender;
            el.ReleaseMouseCapture();
            e.Handled = true;
        }


        private void MouseWheelHandler(object sender, MouseWheelEventArgs e)
        {
            if (! IsAllowedByFilter(TrackballEventType.MouseWheel, sender, e))
            {
                return;
            }

            e.Handled = true;
            m_scaleDelta += e.Delta/(double) 1000;
            m_scaleDelta = Math.Max(m_scaleDelta, 0); // Disallow view inversion
            UpdateViewports(m_rotation, m_scaleDelta, m_translate);
        }


        private void PreviewKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left || e.Key == Key.Right)
            {
                var factor    = (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) ? 5 : 1;
                var delta     = BoardRotationDelta * factor;
                var direction = (e.Key == Key.Right) ? 1 : -1;
                var q = new Quaternion(new Vector3D(0,1,0), delta * direction);

                m_rotation *= q;
                m_rotationDelta = q;
                UpdateViewports(m_rotation, m_scaleDelta, m_translate);

                e.Handled = true;
            }
        }
    }
}
