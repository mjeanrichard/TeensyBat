using System.Collections;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinRtLib;

namespace TeensyBatMap.Controls
{
    public sealed partial class HistogramFilter : UserControl
    {
        public static readonly DependencyProperty BinsSourceProperty = DependencyProperty.Register(
            "BinsSource", typeof(IEnumerable), typeof(HistogramFilter), new PropertyMetadata(default(IEnumerable)));

        public static readonly DependencyProperty DisplayRangeProperty = DependencyProperty.Register(
            "DisplayRange", typeof(Range<int>), typeof(HistogramFilter), new PropertyMetadata(default(Range<int>)));

        public static readonly DependencyProperty FilterRangeProperty = DependencyProperty.Register(
            "FilterRange", typeof(Range<int>), typeof(HistogramFilter), new PropertyMetadata(default(Range<int>)));

        public static readonly DependencyProperty TitelProperty = DependencyProperty.Register(
            "Titel", typeof(string), typeof(HistogramFilter), new PropertyMetadata(default(string)));

        public HistogramFilter()
        {
            if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                FilterRange = new Range<int>(20, 70);
                DisplayRange = new Range<int>(0, 100);
                Titel = "Test Control";
            }
            InitializeComponent();

            ((FrameworkElement)Content).DataContext = this;
        }

        public Range<int> FilterRange
        {
            get { return (Range<int>)GetValue(FilterRangeProperty); }
            set { SetValue(FilterRangeProperty, value); }
        }

        public Range<int> DisplayRange
        {
            get { return (Range<int>)GetValue(DisplayRangeProperty); }
            set { SetValue(DisplayRangeProperty, value); }
        }

        public IEnumerable BinsSource
        {
            get { return (IEnumerable)GetValue(BinsSourceProperty); }
            set { SetValue(BinsSourceProperty, value); }
        }

        public string Titel
        {
            get { return (string)GetValue(TitelProperty); }
            set { SetValue(TitelProperty, value); }
        }

        public bool DrawPrimaryFirst
        {
            get { return histogramControl.DrawPrimaryFirst; }
            set { histogramControl.DrawPrimaryFirst = value; }
        }

        public Color BarColor
        {
            get { return histogramControl.BarColor; }
            set { histogramControl.BarColor = value; }
        }

        public Color SecondaryBarColor
        {
            get { return histogramControl.SecondaryBarColor; }
            set { histogramControl.SecondaryBarColor = value; }
        }

        public static readonly DependencyProperty LabelSizeProperty = DependencyProperty.Register(
            "LabelSize", typeof(Size), typeof(HistogramFilter), new PropertyMetadata(default(Size)));

        public Size LabelSize
        {
            get { return (Size)GetValue(LabelSizeProperty); }
            set { SetValue(LabelSizeProperty, value); }
        }

        public bool ShowTextBoxes
        {
            get { return txtMin.Visibility == Visibility.Visible; }
            set
            {
                Visibility visibility = value ? Visibility.Visible : Visibility.Collapsed;
                txtMin.Visibility = visibility;
                txtMax.Visibility = visibility;
            }
        }

        public bool ShowSecondary
        {
            get { return tgShowSecondary.IsOn; }
            set { tgShowSecondary.IsOn = value; }
        }
    }
}