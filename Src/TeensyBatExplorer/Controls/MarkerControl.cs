// 
// Teensy Bat Explorer - Copyright(C) 2018 Meinrad Jean-Richard
//  
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//  
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//  
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;

using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;

namespace TeensyBatExplorer.Controls
{
    [TemplatePart(Name = SizeEllipsePartName, Type = typeof(Ellipse))]
    public sealed class MarkerControl : Control
    {
        public static readonly DependencyProperty EllipseSizeProperty = DependencyProperty.Register(
            "EllipseSize", typeof(int), typeof(MarkerControl), new PropertyMetadata(default(int), PropertyChanged));

        private static void PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((MarkerControl)d).Update();
        }

        public int EllipseSize
        {
            get { return (int)GetValue(EllipseSizeProperty); }
            set
            {
                SetValue(EllipseSizeProperty, value);
                Update();
            }
        }

        private const string SizeEllipsePartName = "PART_SizeEllipse";

        private Ellipse _sizeEllipse;
        private Compositor _compositor;
        private Visual _targetVisual;
        private Storyboard _storyboard;
        private DoubleAnimation _widthAni;
        private DoubleAnimation _heightAni;
        private ScaleTransform _transform;

        public MarkerControl()
        {
            DefaultStyleKey = typeof(MarkerControl);
        }

        private void Update()
        {
            if (_sizeEllipse == null)
            {
                return;
            }


            double scale;
            int ellipseSize = EllipseSize*10+12;
            scale = ellipseSize / 12d;

            _widthAni.From = _transform.ScaleX;
            _widthAni.To = scale;
            _heightAni.From = _transform.ScaleY;
            _heightAni.To = scale;
            _storyboard.Begin();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _sizeEllipse = GetTemplateChild(SizeEllipsePartName) as Ellipse;

            if (_sizeEllipse == null)
            {
                return;
            }

            _transform = new ScaleTransform();
            _sizeEllipse.RenderTransform = _transform;
            _sizeEllipse.RenderTransformOrigin = new Point(0, 0);
            _transform.CenterX = _sizeEllipse.ActualWidth / 2.0;
            _transform.CenterY = _sizeEllipse.ActualHeight / 2.0;
            _targetVisual = ElementCompositionPreview.GetElementVisual(_sizeEllipse);

            _compositor = _targetVisual.Compositor;

            _storyboard = new Storyboard();
                
            _widthAni = new DoubleAnimation();
            _widthAni.Duration = new Duration(TimeSpan.FromMilliseconds(200));
            _widthAni.EasingFunction = new CubicEase();
            _widthAni.EnableDependentAnimation = true;
            Storyboard.SetTarget(_widthAni, _transform);
            Storyboard.SetTargetProperty(_widthAni, "ScaleX");
            _storyboard.Children.Add(_widthAni);

            _heightAni = new DoubleAnimation();
            _heightAni.Duration = new Duration(TimeSpan.FromMilliseconds(200));
            _heightAni.EasingFunction = new CubicEase();
            _heightAni.EnableDependentAnimation = true;
            Storyboard.SetTarget(_heightAni, _transform);
            Storyboard.SetTargetProperty(_heightAni, "ScaleY");
            _storyboard.Children.Add(_heightAni);
        }
    }
}