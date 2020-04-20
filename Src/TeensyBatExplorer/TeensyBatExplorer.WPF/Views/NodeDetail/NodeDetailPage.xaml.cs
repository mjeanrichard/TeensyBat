using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using MapControl;

using TeensyBatExplorer.WPF.Controls;

namespace TeensyBatExplorer.WPF.Views.NodeDetail
{
    /// <summary>
    /// Interaction logic for NodeDetailPage.xaml
    /// </summary>
    public partial class NodeDetailPage : UserControl
    {
        public NodeDetailPage()
        {
            ImageLoader.HttpClient.DefaultRequestHeaders.Add("User-Agent", "XAML Map Control Test Application");
            TileImageLoader.Cache = new MapControl.Caching.ImageFileCache(TileImageLoader.DefaultCacheFolder);
            InitializeComponent();
        }

        private NodeDetailViewModel ViewModel => (NodeDetailViewModel)DataContext;

        private void MapMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2)
            {
                ViewModel.NodeLocation = Map.ViewToLocation(e.GetPosition(Map));
            }
        }

        private void BarControl_OnPositionChanged(object? sender, PositionEventArgs e)
        {
            ViewModel.PositionChanged(e.NewPosition);
        }
    }
}
