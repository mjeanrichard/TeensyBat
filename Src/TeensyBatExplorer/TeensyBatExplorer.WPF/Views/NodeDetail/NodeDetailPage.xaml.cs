// 
// Teensy Bat Explorer - Copyright(C) 2020 Meinrad Jean-Richard
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

using System.Windows.Controls;
using System.Windows.Input;

using MapControl;
using MapControl.Caching;

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
            TileImageLoader.Cache = new ImageFileCache(TileImageLoader.DefaultCacheFolder);
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
            ViewModel?.PositionChanged(e.NewPosition);
        }
    }
}