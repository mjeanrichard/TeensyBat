// 
// Teensy Bat Explorer - Copyright(C) 2019 Meinrad Jean-Richard
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

using Microsoft.Toolkit.Uwp.Helpers;

using TeensyBatExplorer.Views;
using TeensyBatExplorer.Views.AddFiles;
using TeensyBatExplorer.Views.Devices;
using TeensyBatExplorer.Views.Log;
using TeensyBatExplorer.Views.Main;
using TeensyBatExplorer.Views.Project;

namespace TeensyBatExplorer.Services
{
    public class NavigationService
    {
        private Frame _frame;
        private object _lastParamUsed;

        public Frame Frame
        {
            get
            {
                if (_frame == null)
                {
                    _frame = Window.Current.Content as Frame;
                    RegisterFrameEvents();
                }

                return _frame;
            }

            set
            {
                UnregisterFrameEvents();
                _frame = value;
                RegisterFrameEvents();
            }
        }

        public bool CanGoBack => Frame.CanGoBack;

        public bool CanGoForward => Frame.CanGoForward;

        public bool GoBack()
        {
            if (CanGoBack)
            {
                Frame.GoBack();
                return true;
            }

            return false;
        }

        public void GoForward() => Frame.GoForward();

        public async Task<bool> Navigate(Type pageType, object parameter = null, NavigationTransitionInfo infoOverride = null)
        {
            return await DispatcherHelper.ExecuteOnUIThreadAsync(() =>
            {
                // Don't open the same page multiple times
                if (Frame.Content?.GetType() != pageType || parameter != null && !parameter.Equals(_lastParamUsed))
                {
                    var navigationResult = Frame.Navigate(pageType, parameter, infoOverride);
                    if (navigationResult)
                    {
                        _lastParamUsed = parameter;
                    }

                    return navigationResult;
                }

                return false;
            });
        }

        private async Task<bool> Navigate<T>(object parameter = null, NavigationTransitionInfo infoOverride = null) where T : Page => await Navigate(typeof(T), parameter, infoOverride);

        public async Task<bool> NavigateToMainPage()
        {
            return await Navigate<MainPage>();
        }

        public async Task<bool> NavigateToSettingsPage()
        {
            return await Navigate<SettingsPage>();
        }

        public async Task<bool> NavigateToDevicesPage()
        {
            return await Navigate<DevicesPage>();
        }

        public async Task<bool> NavigateToProjectPage()
        {
            return await Navigate<ProjectPage>();
        }

        public async Task<bool> NavigateToAddFilesPage()
        {
            return await Navigate<AddFilesPage>();
        }

        public async Task<bool> NavigateToNodePage(int nodeNumber)
        {
            return await Navigate<LogPage>(nodeNumber);
        }

        private void RegisterFrameEvents()
        {
            if (_frame != null)
            {
                _frame.Navigated += Frame_Navigated;
                _frame.NavigationFailed += Frame_NavigationFailed;
            }
        }

        private void UnregisterFrameEvents()
        {
            if (_frame != null)
            {
                _frame.Navigated -= Frame_Navigated;
                _frame.NavigationFailed -= Frame_NavigationFailed;
            }
        }

        private void Frame_NavigationFailed(object sender, NavigationFailedEventArgs e) => NavigationFailed?.Invoke(sender, e);

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            Navigated?.Invoke(sender, e);
        }

        public event NavigatedEventHandler Navigated;

        public event NavigationFailedEventHandler NavigationFailed;
    }
}