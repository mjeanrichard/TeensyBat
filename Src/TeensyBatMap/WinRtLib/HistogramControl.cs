using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace WinRtLib
{
    public sealed class HistogramControl : Control
    {
        public static readonly DependencyProperty BinsSourceProperty = DependencyProperty.Register(
            "BinsSource",
            typeof(IEnumerable),
            typeof(HistogramControl),
            new PropertyMetadata(null, OnBinsSourceChanged)
            );

        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
            "MaxValue",
            typeof(double),
            typeof(HistogramControl),
            new PropertyMetadata(null)
            );

        public static readonly DependencyProperty LabelSizeProperty = DependencyProperty.Register(
            "LabelSize",
            typeof(Size),
            typeof(HistogramControl),
            new PropertyMetadata(null, OnGraphicalPropertyChanged)
            );

        public static readonly DependencyProperty BarColorProperty = DependencyProperty.Register(
            "BarColor",
            typeof(Color),
            typeof(HistogramControl),
            new PropertyMetadata(null, OnGraphicalPropertyChanged)
            );

        public static readonly DependencyProperty SecondaryBarColorProperty = DependencyProperty.Register(
            "SecondaryBarColor",
            typeof(Color),
            typeof(HistogramControl),
            new PropertyMetadata(null, OnGraphicalPropertyChanged)
            );

        public static readonly DependencyProperty ShowSecondaryProperty = DependencyProperty.Register(
            "ShowSecondary",
            typeof(bool),
            typeof(HistogramControl),
            new PropertyMetadata(null, OnGraphicalPropertyChanged)
            );

        private static void OnBinsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((HistogramControl)d).BinsSourceChanged(e);
        }

        private static void OnGraphicalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CanvasControl canvas = ((HistogramControl)d)._histogramCanvas;
            if (canvas != null)
            {
                canvas.Invalidate();
            }
        }

        private CanvasControl _histogramCanvas;

        public HistogramControl()
        {
            DefaultStyleKey = typeof(HistogramControl);
            TickHeight = 3;
            BarColor = Colors.White;
            DrawPrimaryFirst = false;
        }

        public IEnumerable BinsSource
        {
            get { return (IEnumerable)GetValue(BinsSourceProperty); }
            set { SetValue(BinsSourceProperty, value); }
        }

        public Color BarColor
        {
            get { return (Color)GetValue(BarColorProperty); }
            set { SetValue(BarColorProperty, value); }
        }

        public Color SecondaryBarColor
        {
            get { return (Color)GetValue(SecondaryBarColorProperty); }
            set { SetValue(SecondaryBarColorProperty, value); }
        }

        public Size LabelSize
        {
            get { return (Size)GetValue(LabelSizeProperty); }
            set { SetValue(LabelSizeProperty, value); }
        }

        public bool ShowSecondary
        {
            get { return (bool)GetValue(ShowSecondaryProperty); }
            set { SetValue(ShowSecondaryProperty, value); }
        }

        public bool DrawPrimaryFirst { get; set; }

        public double MaxValue
        {
            get { return (double)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }

        public Thickness BarPadding { get; set; }
        public int TickHeight { get; set; }

        private void BinsSourceChanged(DependencyPropertyChangedEventArgs e)
        {
            INotifyCollectionChanged oldCollection = e.OldValue as INotifyCollectionChanged;
            if (oldCollection != null)
            {
                oldCollection.CollectionChanged -= BinSourceOnCollectionChanged;
            }

            INotifyCollectionChanged newCollection = e.NewValue as INotifyCollectionChanged;
            if (newCollection != null)
            {
                newCollection.CollectionChanged += BinSourceOnCollectionChanged;
            }
        }

        private void BinSourceOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_histogramCanvas != null)
            {
                _histogramCanvas.Invalidate();
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _histogramCanvas = GetTemplateChild("HistogramCanvas") as CanvasControl;
            if (_histogramCanvas == null)
            {
                throw new InvalidOperationException("Missing CanvasControl 'HistogramCanvas' Part.");
            }
            _histogramCanvas.Draw += HistogramCanvasOnDraw;
        }

        private void HistogramCanvasOnDraw(CanvasControl canvas, CanvasDrawEventArgs args)
        {
            if (_histogramCanvas == null || BinsSource == null)
            {
                return;
            }

            using (CanvasDrawingSession session = args.DrawingSession)
            {
                IBin[] bins = ((IEnumerable<IBin>)BinsSource).ToArray();
                DrawBars(bins, session, canvas);
                DrawTicks(bins, session, canvas);
            }
        }

        private void DrawBars(IBin[] bins, CanvasDrawingSession session, CanvasControl canvas)
        {
            if (bins.Length == 0)
            {
                return;
            }

            double[] binValues = bins.Select(b => b.Value).ToArray();
            double[] secondaryValues = null;
            if (ShowSecondary)
            {
                secondaryValues = bins.Select(b => b.SecondaryValue).ToArray();
            }

            if (!DrawPrimaryFirst && ShowSecondary)
            {
                double[] temp = binValues;
                binValues = secondaryValues;
                secondaryValues = temp;
            }

            double maxBarHeight = canvas.ActualHeight - (BarPadding.Top + BarPadding.Bottom) - LabelSize.Height;
            int binCount = binValues.Length;
            double barWidth = canvas.ActualWidth / binCount;

            double maxValue = MaxValue;
            if (maxValue == 0)
            {
                maxValue = binValues.Max();
                if (secondaryValues != null)
                {
                    maxValue = Math.Max(maxValue, secondaryValues.Max());
                }
            }
            double valueFactor = Math.Max(maxBarHeight / maxValue, 0);


            Color barColor = BarColor;
            Color secondaryBarColor = SecondaryBarColor;
            for (int barIndex = 0; barIndex < binValues.Length; barIndex++)
            {
                double value = binValues[barIndex];
                double left = barIndex * barWidth;

                Rect r = new Rect();
                r.X = left + BarPadding.Left;
                r.Width = Math.Max(barWidth - (BarPadding.Left + BarPadding.Right), 0.1);

                if (secondaryValues != null)
                {
                    double secondaryValue = secondaryValues[barIndex];
                    r.Height = secondaryValue * valueFactor;
                    r.Y = maxBarHeight + BarPadding.Top - r.Height;
                    session.FillRectangle(r, secondaryBarColor);
                }

                r.Height = value * valueFactor;
                r.Y = maxBarHeight + BarPadding.Top - r.Height;
                session.FillRectangle(r, barColor);
            }
        }

        public void DrawTicks(IBin[] bins, CanvasDrawingSession session, CanvasControl canvas)
        {
            if (LabelSize.Height == 0 || LabelSize.Width == 0)
            {
                return;
            }

            double barWidth = canvas.ActualWidth / bins.Length;
            double canvasWidth = canvas.ActualWidth;
            int labelFrequency = (int)Math.Ceiling((bins.Length - 1) / (canvasWidth / LabelSize.Width));

            CanvasTextFormat labelFormat = new CanvasTextFormat();
            labelFormat.WordWrapping = CanvasWordWrapping.NoWrap;
            labelFormat.FontSize = 12;

            double lastLabelRight = 0;
            for (int barIndex = 0; barIndex < bins.Length; barIndex++)
            {
                if (barIndex % labelFrequency != 0 && barIndex < bins.Length - 1)
                {
                    continue;
                }

                Rect labelRect = new Rect();
                Vector2 p1 = new Vector2();
                Vector2 p2 = new Vector2();

                bool show = false;
                if (barIndex == 0)
                {
                    p1.X = (float)(barWidth / 2);
                    labelRect.X = 0;
                    labelFormat.HorizontalAlignment = CanvasHorizontalAlignment.Left;
                    show = false;
                }
                else if (barIndex == bins.Length - 1)
                {
                    p1.X = (float)(canvasWidth - barWidth / 2);
                    labelRect.X = canvasWidth - LabelSize.Width;
                    labelFormat.HorizontalAlignment = CanvasHorizontalAlignment.Right;
                    show = false;
                }
                else
                {
                    p1.X = (float)(barIndex * barWidth + (barWidth / 2));
                    labelRect.X = (barIndex * barWidth + (barWidth / 2)) - LabelSize.Width / 2;
                    labelFormat.HorizontalAlignment = CanvasHorizontalAlignment.Center;

                    show = true;
                }

                p1.Y = (float)(canvas.ActualHeight - LabelSize.Height);
                p2.Y = (float)(canvas.ActualHeight - LabelSize.Height + TickHeight);
                if (!show)
                {
                    p2.Y += 4;
                }
                p2.X = p1.X;
                session.DrawLine(p1, p2, Colors.White);

                if (show)
                { 
                    IBin bin = bins[barIndex];
                    string label = bin.Label;
                    labelRect.Width = LabelSize.Width;
                    labelRect.Height = LabelSize.Height - TickHeight - 1;
                    labelRect.Y = canvas.ActualHeight - LabelSize.Height + TickHeight + 1;
                    if (labelRect.Right <= canvas.ActualWidth)
                    {
                        session.DrawText(label, labelRect, Colors.White, labelFormat);
                    }
                }
                
            }
        }
    }
}