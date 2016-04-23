using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace hashtag.wordcloud.twitter.Helpers
{
    public class WrapPanel : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            var finalSize = new Size { Width = availableSize.Width };
            double x = 0;
            var rowHeight = 0d;
            foreach (var child in Children)
            {
                child.Measure(availableSize);
                x += child.DesiredSize.Width;
                if (x > availableSize.Width)
                {
                    x = child.DesiredSize.Width;
                    finalSize.Height += rowHeight;
                    rowHeight = child.DesiredSize.Height;
                }
                else
                {
                    rowHeight = Math.Max(child.DesiredSize.Height, rowHeight);
                }
            }
            finalSize.Height += rowHeight;
            return finalSize;
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            var startPoint = new Point(0, 0);
            var largestHeight = 0.0;

            foreach (var child in Children.Where(child => child.Visibility == Visibility.Visible))
            {
                if (startPoint.X + child.DesiredSize.Width > finalSize.Width)
                {
                    startPoint.X = 0;
                    startPoint.Y += largestHeight;
                    largestHeight = 0;
                }

                largestHeight = Math.Max(largestHeight, child.DesiredSize.Height);
                child.Arrange(new Rect(startPoint, new Point(startPoint.X + child.DesiredSize.Width, startPoint.Y + child.DesiredSize.Height)));
                startPoint.X += child.DesiredSize.Width;
            }

            if (Math.Abs(Height - (startPoint.Y + largestHeight)) > double.Epsilon)
            {
                SetValue(HeightProperty, startPoint.Y + largestHeight);
            }

            AnimateChildren();
            return base.ArrangeOverride(new Size(finalSize.Width, startPoint.Y + largestHeight));

        }

        private void AnimateChildren()
        {
            var rnd = new Random();
            var sb = new Storyboard();
            var propertyX = new PropertyPath("(UIElement.RenderTransform).(CompositeTransform.TranslateX)");
            var propertyY = new PropertyPath("(UIElement.RenderTransform).(CompositeTransform.TranslateY)");
            foreach (var item in Children)
            {
                var t = (item.RenderTransform as CompositeTransform) ?? new CompositeTransform();
                t.TranslateX = rnd.NextDouble() * 300 - 150;
                t.TranslateY = rnd.NextDouble() * 300 - 150;
                item.RenderTransform = t;

                var xAnim = new DoubleAnimation()
                {
                    BeginTime = TimeSpan.FromMilliseconds(700),
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(rnd.NextDouble() * 5000 + 1000),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                };
                Storyboard.SetTarget(xAnim, item);
                Storyboard.SetTargetProperty(xAnim, propertyX.Path);
                sb.Children.Add(xAnim);

                var yAnim = new DoubleAnimation
                {
                    BeginTime = TimeSpan.FromMilliseconds(700),
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(rnd.NextDouble() * 5000 + 1000),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                };

                Storyboard.SetTarget(yAnim, item);
                Storyboard.SetTargetProperty(yAnim, propertyY.Path);
                sb.Children.Add(yAnim);

            }
            sb.Begin();
        }
    }
}