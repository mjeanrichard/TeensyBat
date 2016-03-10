using TeensyBatMap.Common;

namespace TeensyBatMap.Views.LogDetails
{
    public class LogDetailsPageBase : AppPage<LogDetailsPageModel>
    {}

    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LogDetailsPage : LogDetailsPageBase
    {
        public LogDetailsPage()
        {
            InitializeComponent();
        }
    }
}