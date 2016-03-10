using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace WinRtLib
{
    public sealed class PageGrid : ContentControl
    {
        public PageGrid()
        {
            this.DefaultStyleKey = typeof(PageGrid);
        }

        public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(
            "IsBusy", typeof(bool), typeof(PageGrid), new PropertyMetadata(default(bool)));

        public bool IsBusy
        {
            get { return (bool)GetValue(IsBusyProperty); }
            set { SetValue(IsBusyProperty, value); }
        }

        public static readonly DependencyProperty TitelProperty = DependencyProperty.Register(
            "Titel", typeof(string), typeof(PageGrid), new PropertyMetadata(default(string)));

        public string Titel
        {
            get { return (string)GetValue(TitelProperty); }
            set { SetValue(TitelProperty, value); }
        }
    }
}
