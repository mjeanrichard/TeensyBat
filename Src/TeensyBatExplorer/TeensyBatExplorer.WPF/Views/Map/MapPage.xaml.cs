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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using MapControl;

namespace TeensyBatExplorer.WPF.Views.Map
{
    /// <summary>
    /// Interaction logic for ProjectPage.xaml
    /// </summary>
    public partial class MapPage : UserControl
    {
        private Location? _lastRightClickLocation;

        public MapPage()
        {
            InitializeComponent();
        }

        private void PlaceDeviceMenu_OnClick(object sender, RoutedEventArgs e)
        {
            if (_lastRightClickLocation != null)
            {
                ((MapViewModel)DataContext).UpdateNodeLocation(_lastRightClickLocation);
            }
        }

        private void Map_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                MapControl.Map map = (MapControl.Map)sender;
                _lastRightClickLocation = map.ViewToLocation(e.GetPosition(map));
            }
        }
    }
}