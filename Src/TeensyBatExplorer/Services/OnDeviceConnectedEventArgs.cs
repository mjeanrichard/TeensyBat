using Windows.Devices.Enumeration;

namespace TeensyBatExplorer.Services
{
    public class OnDeviceConnectedEventArgs
    {
        public OnDeviceConnectedEventArgs(bool isDeviceSuccessfullyConnected, DeviceInformation deviceInformation)
        {
            IsDeviceSuccessfullyConnected = isDeviceSuccessfullyConnected;
            DeviceInformation = deviceInformation;
        }

        public bool IsDeviceSuccessfullyConnected { get; }

        public DeviceInformation DeviceInformation { get; }
    }
}