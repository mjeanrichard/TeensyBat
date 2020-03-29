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

namespace TeensyBatExplorer.WPF.Views.Device
{
    /// <summary>
    /// Interaction logic for DevicePage.xaml
    /// </summary>
    public partial class DevicePage : UserControl
    {
        private GridLength _expandedHeight;
        private bool _autoScroll;

        public DevicePage()
        {
            InitializeComponent();
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            if (ConsoleView != null && ExpanderRow != null && Splitter != null)
            {
                ConsoleView.Visibility = Visibility.Visible;
                ExpanderRow.Height = _expandedHeight;
                Splitter.Visibility = Visibility.Visible;
                ConsoleView.AutoScroll = _autoScroll;
            }
        }

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            if (ConsoleView != null && ExpanderRow != null && Splitter != null)
            {
                _autoScroll = ConsoleView.AutoScroll;
                _expandedHeight = ExpanderRow.Height;
                ConsoleView.Visibility = Visibility.Collapsed;
                Splitter.Visibility = Visibility.Collapsed;
                ExpanderRow.Height = GridLength.Auto;
            }
        }
    }
}