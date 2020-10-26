using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Controls;

namespace STak.WinTak
{
    public class StoryboardAnimation
    {
        private FrameworkElement            m_scope;
        private Storyboard                  m_storyboard;
        private HashSet<AnimatableProperty> m_properties;


        public StoryboardAnimation(FrameworkElement scope)
        {
            m_scope      = scope;
            m_storyboard = new Storyboard();
            m_properties = new HashSet<AnimatableProperty>();

            NameScope.SetNameScope(m_scope, new NameScope());
        }


        public void AddTimeline(Timeline timeline, AnimatableProperty property)
        {
            m_scope.RegisterName(property.Name, property.Animatable);

            Storyboard.SetTargetName(timeline, property.Name);
            Storyboard.SetTargetProperty(timeline, property.PropertyPath);

            m_storyboard.Children.Add(timeline);
            m_properties.Add(property);
        }


        public async Task Run()
        {
            // Create a task to be triggered when the animation completes.
            var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Create the code to be called upon animation completion.
            void finisher(object s, EventArgs e)
            {
                foreach (var property in m_properties)
                {
                    if (property.Animatable is DependencyObject obj)
                    {
                        double value = (double) obj.GetValue(property.Property);
                        property.Animatable.BeginAnimation(property.Property, null);
                        obj.SetValue(property.Property, value);
                    }
                }
                // Trigger task completion.
                source.SetResult(true);
            }

            // Run the animations.
            m_storyboard.Completed += finisher;
            m_storyboard.Begin(m_scope, true);

            // Wait for animation completion.
            await source.Task;
        }
    }


    public class AnimatableProperty
    {
        public string             Name         { get; set; }
        public IAnimatable        Animatable   { get; set; }
        public DependencyProperty Property     { get; set; }
        public PropertyPath       PropertyPath { get; set; }
    }
}
