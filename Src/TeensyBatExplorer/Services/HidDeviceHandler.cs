// 
// Teensy Bat Explorer - Copyright(C) 2018 Meinard Jean-Richard
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
using System.Threading;
using System.Threading.Tasks;

using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Xaml;

using Microsoft.Toolkit.Uwp.Helpers;

using TeensyBatExplorer.Models;

namespace TeensyBatExplorer.Services
{
    public class HidDeviceHandler
    {
        private DeviceWatcher _deviceWatcher;

        private TypedEventHandler<DeviceAccessInformation, DeviceAccessChangedEventArgs> _deviceAccessEventHandler;

        public HidDeviceHandler(ushort usagePage, ushort usageId)
        {
            RegisterForAppEvents();

            ushort vendorId = 0x16C0;
            ushort productId = 0x0486;
            DeviceSelector = HidDevice.GetDeviceSelector(usagePage, usageId, vendorId, productId);

            StartDeviceWatcher();
        }

        public TypedEventHandler<HidDeviceHandler, DeviceInformation> DeviceClosing { get; set; }

        public TypedEventHandler<HidDeviceHandler, OnDeviceConnectedEventArgs> DeviceConnected { get; set; }

        public bool IsDeviceConnected => Device != null;

        public HidDevice Device { get; private set; }

        /// <summary>
        ///     This DeviceInformation represents which device is connected or which device will be reconnected when
        ///     the device is plugged in again (if IsEnabledAutoReconnect is true);.
        /// </summary>
        public DeviceInformation DeviceInformation { get; private set; }

        /// <summary>
        ///     Returns DeviceAccessInformation for the device that is currently connected using this EventHandlerForDevice
        ///     object.
        /// </summary>
        public DeviceAccessInformation DeviceAccessInformation { get; private set; }

        /// <summary>
        ///     DeviceSelector AQS used to find this device
        /// </summary>
        private string DeviceSelector { get; }

        /// <summary>
        ///     This method opens the device using the WinRT Hid API. After the device is opened, save the device
        ///     so that it can be used across scenarios.
        ///     It is important that the FromIdAsync call is made on the UI thread because the consent prompt can only be displayed
        ///     on the UI thread.
        ///     This method is used to reopen the device after the device reconnects to the computer and when the app resumes.
        /// </summary>
        /// <param name="deviceInfo">Device information of the device to be opened</param>
        /// <returns>
        ///     True if the device was successfully opened, false if the device could not be opened for well known reasons.
        ///     An exception may be thrown if the device could not be opened for extraordinary reasons.
        /// </returns>
        private async Task<bool> OpenDeviceAsync(DeviceInformation deviceInfo)
        {
            Device = await HidDevice.FromIdAsync(deviceInfo.Id, FileAccessMode.ReadWrite);

            bool successfullyOpenedDevice = false;
            //NotifyType notificationStatus;
            string notificationMessage = null;

            // Device could have been blocked by user or the device has already been opened by another app.
            if (Device != null)
            {
                successfullyOpenedDevice = true;

                DeviceInformation = deviceInfo;

                //notificationStatus = NotifyType.StatusMessage;
                //notificationMessage = "Device " + deviceInformation.Id + " opened";

                // User can block the device after it has been opened in the Settings charm. We can detect this by registering for the 
                // DeviceAccessInformation.AccessChanged event
                if (_deviceAccessEventHandler == null)
                {
                    RegisterForDeviceAccessStatusChange();
                }
            }
            else
            {
                successfullyOpenedDevice = false;

                //notificationStatus = NotifyType.ErrorMessage;

                DeviceAccessStatus deviceAccessStatus = DeviceAccessInformation.CreateFromId(deviceInfo.Id).CurrentStatus;

                if (deviceAccessStatus == DeviceAccessStatus.DeniedByUser)
                {
                    notificationMessage = "Access to the device was blocked by the user : " + deviceInfo.Id;
                }
                else if (deviceAccessStatus == DeviceAccessStatus.DeniedBySystem)
                {
                    // This status is most likely caused by app permissions (did not declare the device in the app's package.appxmanifest)
                    // This status does not cover the case where the device is already opened by another app.
                    notificationMessage = "Access to the device was blocked by the system : " + deviceInfo.Id;
                }
                else
                {
                    // Most likely the device is opened by another app, but cannot be sure
                    notificationMessage = "Unknown error, possibly opened by another app : " + deviceInfo.Id;
                }
            }

            //MainPage.Current.NotifyUser(notificationMessage, notificationStatus);

            // Notify registered callback handle that the device has been opened
            if (DeviceConnected != null)
            {
                OnDeviceConnectedEventArgs deviceConnectedEventArgs = new OnDeviceConnectedEventArgs(successfullyOpenedDevice, deviceInfo);
                DeviceConnected(this, deviceConnectedEventArgs);
            }

            return successfullyOpenedDevice;
        }

        /// <summary>
        ///     Closes the device, stops the device watcher, stops listening for app events, and resets object state to before a
        ///     device
        ///     was ever connected.
        /// </summary>
        public void Dispose()
        {
            if (IsDeviceConnected)
            {
                CloseCurrentlyConnectedDevice();
            }

            if (DeviceAccessInformation != null)
            {
                UnregisterFromDeviceAccessStatusChange();
                DeviceAccessInformation = null;
            }

            DeviceConnected = null;
            DeviceClosing = null;
        }

        /// <summary>
        ///     This method demonstrates how to close the device properly using the WinRT Hid API.
        ///     When the HidDevice is closing, it will cancel all IO operations that are still pending (not complete).
        ///     The close will not wait for any IO completion callbacks to be called, so the close call may complete before any of
        ///     the IO completion callbacks are called.
        ///     The pending IO operations will still call their respective completion callbacks with either a task
        ///     cancelled error or the operation completed.
        /// </summary>
        private void CloseCurrentlyConnectedDevice()
        {
            if (Device != null)
            {
                // Notify callback that we're about to close the device
                DeviceClosing?.Invoke(this, DeviceInformation);

                // This closes the handle to the device
                Device.Dispose();
                Device = null;
                DeviceInformation = null;
            }
        }

        /// <summary>
        ///     Register for app suspension/resume events. See the comments
        ///     for the event handlers for more information on what is being done to the device.
        ///     We will also register for when the app exists so that we may close the device handle.
        /// </summary>
        private void RegisterForAppEvents()
        {
            // This event is raised when the app is exited and when the app is suspended
            Application.Current.Suspending += OnAppSuspension;
            Application.Current.Resuming += OnAppResume;
        }

        private void StartDeviceWatcher()
        {
            if (_deviceWatcher == null)
            {
                _deviceWatcher = DeviceInformation.CreateWatcher(DeviceSelector);
                _deviceWatcher.Added += OnDeviceAdded;
                _deviceWatcher.Removed += OnDeviceRemoved;
            }

            if (_deviceWatcher.Status != DeviceWatcherStatus.Started && _deviceWatcher.Status != DeviceWatcherStatus.EnumerationCompleted)
            {
                _deviceWatcher.Start();
            }
        }

        /// <summary>
        ///     Listen for any changed in device access permission. The user can block access to the device while the device is in
        ///     use.
        ///     If the user blocks access to the device while the device is opened, the device's handle will be closed
        ///     automatically by
        ///     the system; it is still a good idea to close the device explicitly so that resources are cleaned up.
        ///     Note that by the time the AccessChanged event is raised, the device handle may already be closed by the system.
        /// </summary>
        private void RegisterForDeviceAccessStatusChange()
        {
            DeviceAccessInformation = DeviceAccessInformation.CreateFromId(DeviceInformation.Id);

            _deviceAccessEventHandler = OnDeviceAccessChanged;
            DeviceAccessInformation.AccessChanged += _deviceAccessEventHandler;
        }

        private void UnregisterFromDeviceAccessStatusChange()
        {
            DeviceAccessInformation.AccessChanged -= _deviceAccessEventHandler;

            _deviceAccessEventHandler = null;
        }

        private void StopDeviceWatcher()
        {
            if (_deviceWatcher == null)
            {
                return;
            }

            if (_deviceWatcher.Status == DeviceWatcherStatus.Started || _deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted)
            {
                _deviceWatcher.Stop();
            }
            _deviceWatcher.Added -= OnDeviceAdded;
            _deviceWatcher.Removed -= OnDeviceRemoved;
        }

        /// <summary>
        ///     If a HidDevice object has been instantiated (a handle to the device is opened), we must close it before the app
        ///     goes into suspension because the API automatically closes it for us if we don't. When resuming, the API will
        ///     not reopen the device automatically, so we need to explicitly open the device in the app (Scenario1_DeviceConnect).
        ///     Since we have to reopen the device ourselves when the app resumes, it is good practice to explicitly call the close
        ///     in the app as well (For every open there is a close).
        ///     We must stop the DeviceWatcher because it will continue to raise events even if
        ///     the app is in suspension, which is not desired (drains battery). We resume the device watcher once the app resumes
        ///     again.
        /// </summary>
        private void OnAppSuspension(object sender, SuspendingEventArgs args)
        {
            StopDeviceWatcher();
            CloseCurrentlyConnectedDevice();
        }

        /// <summary>
        ///     When resume into the application, we should reopen a handle to the Hid device again. This will automatically
        ///     happen when we start the device watcher again; the device will be re-enumerated and we will attempt to reopen it
        ///     if IsEnabledAutoReconnect property is enabled.
        ///     See OnAppSuspension for why we are starting the device watcher again
        /// </summary>
        private void OnAppResume(object sender, object args)
        {
            StartDeviceWatcher();
        }

        /// <summary>
        ///     Close the device that is opened so that all pending operations are canceled properly.
        /// </summary>
        private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate deviceInformationUpdate)
        {
            if (IsDeviceConnected && deviceInformationUpdate.Id == DeviceInformation.Id)
            {
                // The main reasons to close the device explicitly is to clean up resources, to properly handle errors,
                // and stop talking to the disconnected device.
                CloseCurrentlyConnectedDevice();
            }
        }

        /// <summary>
        ///     Open the device that the user wanted to open if it hasn't been opened yet and auto reconnect is enabled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="deviceInfo"></param>
        private async void OnDeviceAdded(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            if (DeviceInformation == null && !IsDeviceConnected)
            {
                await DispatcherHelper.ExecuteOnUIThreadAsync(async () => { await OpenDeviceAsync(deviceInfo); });
            }
        }

        /// <summary>
        ///     Close the device if the device access was denied by anyone (system or the user) and reopen it if permissions are
        ///     allowed again
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private async void OnDeviceAccessChanged(DeviceAccessInformation sender, DeviceAccessChangedEventArgs eventArgs)
        {
            if (eventArgs.Status == DeviceAccessStatus.DeniedBySystem || eventArgs.Status == DeviceAccessStatus.DeniedByUser)
            {
                CloseCurrentlyConnectedDevice();
            }
            else if (eventArgs.Status == DeviceAccessStatus.Allowed && DeviceInformation != null)
            {
                await DispatcherHelper.ExecuteOnUIThreadAsync(async () => { await OpenDeviceAsync(DeviceInformation); });
            }
        }
    }
}