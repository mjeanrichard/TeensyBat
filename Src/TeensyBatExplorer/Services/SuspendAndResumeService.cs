// 
// Teensy Bat Explorer - Copyright(C) 2018 Meinrad Jean-Richard
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
using System.Threading.Tasks;

using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

using TeensyBatExplorer.Activation;
using TeensyBatExplorer.Helpers.Storage;

namespace TeensyBatExplorer.Services
{
    // More details regarding the application lifecycle and how to handle suspend and resume at https://docs.microsoft.com/windows/uwp/launch-resume/app-lifecycle
    public class SuspendAndResumeService : ActivationHandler<LaunchActivatedEventArgs>
    {
        private const string StateFilename = "SuspendAndResumeState";
        private readonly NavigationService _navigationService;

        public SuspendAndResumeService(NavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        public override int Priority => 10;

        // TODO WTS: Subscribe to this event if you want to save the current state. It is fired just before the app enters the background.
        public event EventHandler<OnBackgroundEnteringEventArgs> OnBackgroundEntering;

        public async Task SaveStateAsync()
        {
            var suspensionState = new SuspensionState()
            {
                SuspensionDate = DateTime.Now
            };

            var target = OnBackgroundEntering?.Target.GetType();
            var onBackgroundEnteringArgs = new OnBackgroundEnteringEventArgs(suspensionState, target);

            OnBackgroundEntering?.Invoke(this, onBackgroundEnteringArgs);

            await ApplicationData.Current.LocalFolder.SaveAsync(StateFilename, onBackgroundEnteringArgs);
        }

        protected override async Task HandleInternalAsync(LaunchActivatedEventArgs args)
        {
            await RestoreStateAsync();
        }

        protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
        {
            return args.PreviousExecutionState == ApplicationExecutionState.Terminated;
        }

        private async Task RestoreStateAsync()
        {
            var saveState = await ApplicationData.Current.LocalFolder.ReadAsync<OnBackgroundEnteringEventArgs>(StateFilename);
            if (saveState?.Target != null && typeof(Page).IsAssignableFrom(saveState.Target))
            {
                _navigationService.Navigate(saveState.Target, saveState.SuspensionState);
            }
        }
    }
}