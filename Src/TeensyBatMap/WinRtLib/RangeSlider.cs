using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Shapes;

namespace WinRtLib
{
    [TemplatePart(Name = ThumbPartNameUpper, Type = typeof(Thumb))]
    [TemplatePart(Name = ThumbPartNameLower, Type = typeof(Thumb))]
    [TemplatePart(Name = MiddleThumbPartName, Type = typeof(Rectangle))]
    public class RangeSlider : Control
    {
        private const string MiddleThumbPartName = "PART_Thumb_Middle";
        private const string ThumbPartNameLower = "PART_Thumb_Lower";
        private const string ThumbPartNameUpper = "PART_Thumb_Upper";
        private const string LeftRectPartName = "PART_RectLeft";
        private const string RightRectPartName = "PART_RectRight";

        public static readonly DependencyProperty UpperValueProperty =
            DependencyProperty.Register("UpperValue", typeof(double), typeof(RangeSlider), new PropertyMetadata(80.0, UpperValuePropertyChanged));

        public static readonly DependencyProperty LowerValueProperty =
            DependencyProperty.Register("LowerValue", typeof(double), typeof(RangeSlider), new PropertyMetadata(20.0, LowerValuePropertyChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(RangeSlider), new PropertyMetadata(100.0, MaximumPropertyChanged));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(RangeSlider), new PropertyMetadata(0.0, MinimumPropertyChanged));

        private static void LowerValuePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            RangeSlider rangeSlider = (RangeSlider)dependencyObject;
            rangeSlider.OnLowerValueChanged((double)e.NewValue);
        }

        private static void MaximumPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            RangeSlider rangeSlider = (RangeSlider)dependencyObject;
            rangeSlider.OnMaximumChanged((double)e.NewValue);
        }

        private static void MinimumPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            RangeSlider rangeSlider = (RangeSlider)dependencyObject;
            rangeSlider.OnMinimumChanged((double)e.NewValue);
        }

        private static void UpperValuePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            RangeSlider rangeSlider = (RangeSlider)dependencyObject;
            rangeSlider.OnUpperValueChanged((double)e.NewValue);
        }

        public event EventHandler LowerValueChanged;
        public event EventHandler UpperValueChanged;
        private bool _isThumbSelected = true;
        private Thumb _lowerThumb;
        private Thumb _middleThumb;
        private Thumb _upperThumb;
        private Rectangle _rightRect;
        private Rectangle _leftRect;

        public RangeSlider()
        {
            DefaultStyleKey = typeof(RangeSlider);
            Maximum = 100;
            Minimum = 0;
            UpperValue = 80;
            LowerValue = 20;
            TapChange = 1;
            SizeChanged += OnSizeChanged;
        }

        public bool IsThumbSelected
        {
            get { return _isThumbSelected; }
            set { _isThumbSelected = value; }
        }

        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public double LowerValue
        {
            get { return (double)GetValue(LowerValueProperty); }
            set { SetValue(LowerValueProperty, value); }
        }

        public double UpperValue
        {
            get { return (double)GetValue(UpperValueProperty); }
            set { SetValue(UpperValueProperty, value); }
        }

        public double TapChange { get; set; }

        private void OnSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            InvalidateArrange();
        }

        protected virtual void OnUpperValueChanged(double newUpperValue)
        {
            InvalidateArrange();
            UpperValueChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnLowerValueChanged(double newLowerValue)
        {
            InvalidateArrange();
            LowerValueChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnMaximumChanged(double newMaximum)
        {
            InvalidateArrange();
        }

        protected virtual void OnMinimumChanged(double newMinimum)
        {
            InvalidateArrange();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _upperThumb = (Thumb)GetTemplateChild(ThumbPartNameUpper);
            _upperThumb.DragDelta += UpperThumb_DragDelta;

            _lowerThumb = (Thumb)GetTemplateChild(ThumbPartNameLower);
            _lowerThumb.DragDelta += LowerThumb_DragDelta;

            _middleThumb = (Thumb)GetTemplateChild(MiddleThumbPartName);
            _middleThumb.DragDelta += MiddleThumbDrageDelta;

            _rightRect = (Rectangle)GetTemplateChild(RightRectPartName);
            _rightRect.Tapped += RightRectTapped;

            _leftRect = (Rectangle)GetTemplateChild(LeftRectPartName);
            _leftRect.Tapped += LeftRectTapped;
        }

        private void LeftRectTapped(object sender, TappedRoutedEventArgs e)
        {
            SetLowerValue(-TapChange);
        }

        private void RightRectTapped(object sender, TappedRoutedEventArgs e)
        {
            SetUpperValue(TapChange);
        }

        private void MiddleThumbDrageDelta(object sender, DragDeltaEventArgs e)
        {
            double change = ConvertValueChange(e.HorizontalChange);
            SetUpperValue(change);
            SetLowerValue(change);
        }

        private void UpperThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            SetUpperValue(ConvertValueChange(e.HorizontalChange));
        }

        private void LowerThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            SetLowerValue(ConvertValueChange(e.HorizontalChange));
        }

        private void SetUpperValue(double change)
        {
            double newValue = SanitizeValue(UpperValue + change);
            if (newValue < LowerValue)
            {
                newValue = LowerValue;
            }
            UpperValue = newValue;
        }

        private void SetLowerValue(double change)
        {
            double newValue = SanitizeValue(LowerValue + change);
            if (newValue > UpperValue)
            {
                newValue = UpperValue;
            }
            LowerValue = newValue;
        }

        protected double ConvertValueChange(double thumbChange)
        {
            double thumbsWidth = _upperThumb.ActualWidth + _lowerThumb.ActualWidth;
            double positionFactor = (ActualWidth - thumbsWidth) / (Maximum - Minimum);
            return thumbChange / positionFactor;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }

        protected double SanitizeValue(double value)
        {
            if (value > Maximum)
            {
                return Maximum;
            }
            if (value < Minimum)
            {
                return Minimum;
            }
            return value;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Minimum >= Maximum)
            {
                _lowerThumb.Visibility = Visibility.Collapsed;
                _upperThumb.Visibility = Visibility.Collapsed;
                _middleThumb.Visibility = Visibility.Collapsed;
                _rightRect.Visibility = Visibility.Collapsed;
                _leftRect.Visibility = Visibility.Collapsed;
                return base.ArrangeOverride(finalSize);
            }

            double thumbsWidth = _upperThumb.ActualWidth + _lowerThumb.ActualWidth;
            double positionFactor = (ActualWidth - thumbsWidth) / (Maximum - Minimum);

            double lower = SanitizeValue(LowerValue);
            double upper = Math.Max(SanitizeValue(UpperValue), lower);

            double lowerLeft = positionFactor * (lower - Minimum);
            double upperLeft = positionFactor * (upper - Minimum) + _lowerThumb.ActualWidth;

            Canvas.SetLeft(_lowerThumb, lowerLeft);
            Canvas.SetLeft(_upperThumb, upperLeft);
            Canvas.SetLeft(_middleThumb, lowerLeft + _lowerThumb.ActualWidth);
            _middleThumb.Width = upperLeft - (lowerLeft + _lowerThumb.ActualWidth);

            Canvas.SetLeft(_leftRect, 0);
            _leftRect.Width = lowerLeft;

            double upperRight = upperLeft + _upperThumb.ActualWidth;
            Canvas.SetLeft(_rightRect, upperRight);
            _rightRect.Width = Math.Max(1, ActualWidth - upperRight);

            _lowerThumb.Visibility = Visibility.Visible;
            _upperThumb.Visibility = Visibility.Visible;
            _middleThumb.Visibility = Visibility.Visible;
            _rightRect.Visibility = Visibility.Visible;
            _leftRect.Visibility = Visibility.Visible;

            return base.ArrangeOverride(finalSize);
        }
    }
}