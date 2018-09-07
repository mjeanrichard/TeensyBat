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

using System.Threading.Tasks;

using Windows.ApplicationModel.Activation;

using TeensyBatExplorer.Services;
using TeensyBatExplorer.Views;

namespace TeensyBatExplorer.Activation
{
    public class LaunchActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
    {
        private readonly NavigationService _navigationService;

        public LaunchActivationHandler(NavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        public override int Priority => 100;

        protected override async Task HandleInternalAsync(LaunchActivatedEventArgs args)
        {
            // When the navigation stack isn't restored, navigate to the first page and configure
            // the new page by passing required information in the navigation parameter
            _navigationService.NavigateToMainPage();

            await Task.CompletedTask;
        }

        protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
        {
            // None of the ActivationHandlers has handled the app activation
            return _navigationService.Frame.Content == null;
        }
    }
}