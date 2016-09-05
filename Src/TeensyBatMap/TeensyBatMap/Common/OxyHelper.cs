using Windows.UI.Xaml;

using OxyPlot;
using OxyPlot.Axes;

namespace TeensyBatMap.Common
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
                axis.MajorGridlineColor= softForeground;
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