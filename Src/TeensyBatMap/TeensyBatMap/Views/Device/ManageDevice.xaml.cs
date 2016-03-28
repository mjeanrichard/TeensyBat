using TeensyBatMap.Common;
using TeensyBatMap.Views.LogDetails;

namespace TeensyBatMap.Views.Device
{
    public class ManageDevicePageBase : AppPage<ManageDevicePageModel>
    {}

    /// <summary>
    ///     An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ManageDevicePage : ManageDevicePageBase
	{
        public ManageDevicePage()
        {
            InitializeComponent();
        }
    }
}