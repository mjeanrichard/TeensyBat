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
using System.Linq;
using System.Windows.Input;

using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

using TeensyBatExplorer.Helpers;
using TeensyBatExplorer.Helpers.DependencyInjection;
using TeensyBatExplorer.Helpers.ViewModels;
using TeensyBatExplorer.Helpers.Xaml;
using TeensyBatExplorer.Services;
using TeensyBatExplorer.Views;

namespace TeensyBatExplorer.ViewModels
{
    public class ShellViewModel : Observable
    {
        private readonly NavigationService _navigationService;
        private NavigationView _navigationView;
        private NavigationViewItem _selected;
        private ICommand _itemInvokedCommand;

        public ShellViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        public NavigationViewItem Selected
        {
            get { return _selected; }
            set { Set(ref _selected, value); }
        }

        public ICommand ItemInvokedCommand => _itemInvokedCommand ?? (_itemInvokedCommand = new ParameterCommand<NavigationViewItemInvokedEventArgs>(OnItemInvoked));

        public void Initialize(Frame frame, NavigationView navigationView)
        {
            _navigationView = navigationView;
            _navigationService.Frame = frame;
            _navigationService.Navigated += Frame_Navigated;
        }

        private void OnItemInvoked(NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                _navigationService.NavigateToSettingsPage();
                return;
            }

            NavigationViewItem item = _navigationView.MenuItems.OfType<NavigationViewItem>().First(menuItem => (string)menuItem.Content == (string)args.InvokedItem);
            Type pageType = item.GetValue(NavHelper.NavigateToProperty) as Type;
            _navigationService.Navigate(pageType);
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.SourcePageType == typeof(SettingsPage))
            {
                Selected = _navigationView.SettingsItem as NavigationViewItem;
                return;
            }

            Selected = _navigationView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(menuItem => IsMenuItemForPageType(menuItem, e.SourcePageType));
        }

        private bool IsMenuItemForPageType(NavigationViewItem menuItem, Type sourcePageType)
        {
            Type pageType = menuItem.GetValue(NavHelper.NavigateToProperty) as Type;
            return pageType == sourcePageType;
        }
    }
}