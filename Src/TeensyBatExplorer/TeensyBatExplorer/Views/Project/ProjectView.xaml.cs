// 
// Teensy Bat Explorer - Copyright(C) 2017 Meinard Jean-Richard
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
using System.Linq;
using System.Numerics;

using Windows.Foundation;
using Windows.Graphics.DirectX;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;

using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Composition;

using TeensyBatExplorer.Common;

using UniversalMapControl.Projections;

namespace TeensyBatExplorer.Views.Project
{
    public class ProjectViewBase : AppPage<ProjectViewModel>
    {
    }

    public sealed partial class ProjectView
    {
        private SpriteVisual _visual;

        public ProjectView()
        {
            InitializeComponent();
            higlightColorConverter.FalseValue = new SolidColorBrush(Colors.Red);
            higlightColorConverter.TrueValue = new SolidColorBrush(Colors.DarkMagenta);
            Loaded += OnLoaded;


        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ZoomToLogs();
        }

        private void ZoomToLogs()
        {
            if (!ViewModel.Logs.Any())
            {
                return;
            }

            double maxLat = double.MinValue;
            double maxLong = double.MinValue;
            double minLat = double.MaxValue;
            double minLong = double.MaxValue;

            foreach (NodeViewModel log in ViewModel.Logs)
            {
                maxLat = Math.Max(maxLat, log.Location.Latitude);
                maxLong = Math.Max(maxLong, log.Location.Longitude);
                minLat = Math.Min(minLat, log.Location.Latitude);
                minLong = Math.Min(minLong, log.Location.Longitude);
            }
            map.ZoomToRect(new Wgs84Location(minLat, minLong), new Wgs84Location(maxLat, maxLong), -0.5);
        }

    }
}