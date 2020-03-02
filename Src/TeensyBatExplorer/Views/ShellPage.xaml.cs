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

using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Controls;

using TeensyBatExplorer.Services;
using TeensyBatExplorer.ViewModels;

namespace TeensyBatExplorer.Views
{
    // TODO WTS: Change the icons and titles for all NavigationViewItems in ShellPage.xaml.
    public sealed partial class ShellPage : Page
    {
        public ShellPage(ActivationService activationService, ShellViewModel shellViewModel)
        {
            InitializeComponent();
            HideNavViewBackButton();
            
            ViewModel = shellViewModel;
            DataContext = ViewModel;
            ViewModel.Initialize(shellFrame, navigationView);

            KeyboardAccelerators.Add(activationService.AltLeftKeyboardAccelerator);
            KeyboardAccelerators.Add(activationService.BackKeyboardAccelerator);
        }

        public ShellViewModel ViewModel { get; }

        private void HideNavViewBackButton()
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 6))
            {
                navigationView.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;
            }
        }
    }
}