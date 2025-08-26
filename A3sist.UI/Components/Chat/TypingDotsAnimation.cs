using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace A3sist.UI.Components.Chat
{
    /// <summary>
    /// A simple typing dots animation control for chat interfaces
    /// </summary>
    public class TypingDotsAnimation : Control
    {
        private Storyboard? _animationStoryboard;
        private bool _isAnimating;

        static TypingDotsAnimation()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TypingDotsAnimation), 
                new FrameworkPropertyMetadata(typeof(TypingDotsAnimation)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            StartAnimation();
        }

        /// <summary>
        /// Starts the typing animation
        /// </summary>
        public void StartAnimation()
        {
            if (_isAnimating || !IsLoaded)
                return;

            _isAnimating = true;
            
            // Create a simple opacity animation for the dots
            _animationStoryboard = new Storyboard();
            _animationStoryboard.RepeatBehavior = RepeatBehavior.Forever;
            
            var opacityAnimation = new DoubleAnimation
            {
                From = 0.3,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(800),
                AutoReverse = true,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            
            Storyboard.SetTarget(opacityAnimation, this);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));
            
            _animationStoryboard.Children.Add(opacityAnimation);
            _animationStoryboard.Begin();
        }

        /// <summary>
        /// Stops the typing animation
        /// </summary>
        public void StopAnimation()
        {
            if (!_isAnimating)
                return;

            _isAnimating = false;
            _animationStoryboard?.Stop();
            _animationStoryboard = null;
            Opacity = 1.0;
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            StopAnimation();
            base.OnUnloaded(e);
        }
    }
}