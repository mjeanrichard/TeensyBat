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

using Windows.UI.Xaml;

using OxyPlot;
using OxyPlot.Axes;

namespace TeensyBatExplorer.Views.Main
{
    public static class OxyHelper
    {
        public static PlotModel AddStyles(this PlotModel plotModel)
        {
            OxyColor background;
            OxyColor foreground;
            OxyColor softForeground;
            if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                background = OxyColors.Black;
                foreground = OxyColors.LightGray;
                softForeground = OxyColors.Gray;
            }
            else
            {
                background = OxyColors.White;
                foreground = OxyColors.Black;
                softForeground = OxyColors.Gray;
            }
            plotModel.Background = background;
            plotModel.LegendBackground = background;
            plotModel.PlotAreaBackground = background;

            plotModel.PlotAreaBorderThickness = new OxyThickness(1);
            plotModel.PlotAreaBorderColor = foreground;

            plotModel.TextColor = foreground;
            plotModel.LegendTextColor = foreground;
            plotModel.SubtitleColor = foreground;
            plotModel.TitleColor = foreground;

            foreach (Axis axis in plotModel.Axes)
            {
                axis.AxislineColor = foreground;
                axis.ExtraGridlineColor = softForeground;
                axis.MajorGridlineColor = softForeground;
                axis.MinorGridlineColor = softForeground;
                axis.MinorTicklineColor = foreground;
                axis.TicklineColor = foreground;
                axis.TitleColor = foreground;
                axis.TextColor = foreground;

                axis.AxisTitleDistance = 10;
                axis.MajorGridlineStyle = LineStyle.Solid;
            }
            return plotModel;
        }
    }
}