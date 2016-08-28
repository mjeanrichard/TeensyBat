using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace WinRtLib
{
	public sealed class LineControl : Control
	{
		public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
			"Data",
			typeof(IEnumerable),
			typeof(LineControl),
			new PropertyMetadata(null, OnDataChanged)
			);

		public static readonly DependencyProperty LabelSizeProperty = DependencyProperty.Register(
			"LabelSize",
			typeof(Size),
			typeof(LineControl),
			new PropertyMetadata(null, OnGraphicalPropertyChanged)
			);

		public static readonly DependencyProperty LineColorProperty = DependencyProperty.Register(
			"LineColor",
			typeof(Color),
			typeof(LineControl),
			new PropertyMetadata(null, OnGraphicalPropertyChanged)
			);

		public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
			"MaxValue", typeof(double), typeof(LineControl), new PropertyMetadata(default(double)));

		public double MaxValue
		{
			get { return (double)GetValue(MaxValueProperty); }
			set { SetValue(MaxValueProperty, value); }
		}

		private CanvasControl _canvas;

		public LineControl()
		{
			DefaultStyleKey = typeof(LineControl);
			TickHeight = 3;
			LineColor = Colors.Red;
		}

		private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((LineControl)d).DataChanged(e);
		}

		private static void OnGraphicalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			CanvasControl canvas = ((LineControl)d)._canvas;
			if (canvas != null)
			{
				canvas.Invalidate();
			}
		}

		public IEnumerable Data
		{
			get { return (IEnumerable)GetValue(DataProperty); }
			set { SetValue(DataProperty, value); }
		}

		public Color LineColor
		{
			get { return (Color)GetValue(LineColorProperty); }
			set { SetValue(LineColorProperty, value); }
		}



		public Size LabelSize
		{
			get { return (Size)GetValue(LabelSizeProperty); }
			set { SetValue(LabelSizeProperty, value); }
		}

		public int TickHeight { get; set; }

		private void DataChanged(DependencyPropertyChangedEventArgs e)
		{
			INotifyCollectionChanged oldCollection = e.OldValue as INotifyCollectionChanged;
			if (oldCollection != null)
			{
				oldCollection.CollectionChanged -= DataOnCollectionChanged;
			}

			INotifyCollectionChanged newCollection = e.NewValue as INotifyCollectionChanged;
			if (newCollection != null)
			{
				newCollection.CollectionChanged += DataOnCollectionChanged;
				if (_canvas != null)
				{
					_canvas.Invalidate();
				}
			}
		}

		private void DataOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (_canvas != null)
			{
				_canvas.Invalidate();
			}
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_canvas = GetTemplateChild("LineCanvas") as CanvasControl;
			if (_canvas == null)
			{
				throw new InvalidOperationException("Missing CanvasControl 'LineCanvas' Part.");
			}
			_canvas.Draw += CanvasOnDraw;
		}

		private void CanvasOnDraw(CanvasControl canvas, CanvasDrawEventArgs args)
		{
			if (_canvas == null || Data == null)
			{
				return;
			}

			CanvasDrawingSession session = args.DrawingSession;
			ObservableCollection<KeyValuePair<int, byte>> dataPoints = (ObservableCollection<KeyValuePair<int, byte>>)Data;

			int count = dataPoints.Count;
			if (count < 2)
			{
				return;
			}

			CanvasPathBuilder builder = new CanvasPathBuilder(session);

			double widthPerPoint = (ActualWidth / count);
			double height = ActualHeight;

			double maxValue = MaxValue;
			if (MaxValue <= 0)
			{
				maxValue = dataPoints.Max(p => p.Value);
			}
			double heightPerValue = height / maxValue;

			builder.BeginFigure(0, Convert.ToSingle(height - (dataPoints[0].Value* heightPerValue)));
			for (int i = 1; i < count; i++)
			{
				int dataPoint = dataPoints[i].Value;
				builder.AddLine(System.Convert.ToSingle(i * widthPerPoint), Convert.ToSingle(height - (dataPoint * heightPerValue)));
			}
			builder.EndFigure(CanvasFigureLoop.Open);
			CanvasGeometry line = CanvasGeometry.CreatePath(builder);
			session.DrawGeometry(line, LineColor);
		}
	}
}