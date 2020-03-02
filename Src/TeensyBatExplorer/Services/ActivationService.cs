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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Windows.ApplicationModel.Activation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Core.Preview;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

using TeensyBatExplorer.Activation;
using TeensyBatExplorer.Helpers.DependencyInjection;
using TeensyBatExplorer.Services.Project;
using TeensyBatExplorer.Views;

using Unity;

namespace TeensyBatExplorer.Services
{
    public class ActivationService
    {
        public readonly KeyboardAccelerator AltLeftKeyboardAccelerator;
        public readonly KeyboardAccelerator BackKeyboardAccelerator;

        private readonly Lazy<ShellPage> _shell;
        private readonly ProjectManager _projectManager;
        private readonly NavigationService _navigationService;

        public ActivationService(NavigationService navigationService, ProjectManager projectManager, Lazy<ShellPage> shell = null)
        {
            _navigationService = navigationService;
            _shell = shell;
            _projectManager = projectManager;

            AltLeftKeyboardAccelerator = BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu);
            BackKeyboardAccelerator = BuildKeyboardAccelerator(VirtualKey.GoBack);
        }

        public ShellPage Shell
        {
            get { return _shell.Value; }
        }

        private KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
        {
            var keyboardAccelerator = new KeyboardAccelerator() { Key = key };
            if (modifiers.HasValue)
            {
                keyboardAccelerator.Modifiers = modifiers.Value;
            }

            ToolTipService.SetToolTip(keyboardAccelerator, string.Empty);
            keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;
            return keyboardAccelerator;
        }

        private void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var result = _navigationService.GoBack();
            args.Handled = result;
        }

        public async Task ActivateAsync(object activationArgs)
        {
            if (IsInteractive(activationArgs))
            {
                // Initialize things like registering background task before the app is loaded
                await InitializeAsync();

                // Do not repeat app initialization when the Window already has content,
                // just ensure that the window is active
                if (Window.Current.Content == null)
                {
                    // Create a Frame to act as the navigation context and navigate to the first page
                    Window.Current.Content = (UIElement)_shell?.Value ?? new Frame();
                    _navigationService.NavigationFailed += (sender, e) => { throw e.Exception; };
                    _navigationService.Navigated += Frame_Navigated;
                    if (SystemNavigationManager.GetForCurrentView() != null)
                    {
                        SystemNavigationManager.GetForCurrentView().BackRequested += ActivationService_BackRequested;
                    }
                }
            }

            var activationHandler = GetActivationHandlers().FirstOrDefault(h => h.CanHandle(activationArgs));

            if (activationHandler != null)
            {
                await activationHandler.HandleAsync(activationArgs);
            }

            if (IsInteractive(activationArgs))
            {
                var defaultHandler = new LaunchActivationHandler(_navigationService);
                if (defaultHandler.CanHandle(activationArgs))
                {
                    await defaultHandler.HandleAsync(activationArgs);
                }

                // Ensure the current window is active
                Window.Current.Activate();

                // Tasks after activation
                await StartupAsync();
            }
        }

        private async Task InitializeAsync()
        {
            await ThemeSelectorService.InitializeAsync();
        }

        private async Task StartupAsync()
        {
            ThemeSelectorService.SetRequestedTheme();
            await Task.CompletedTask;
        }

        private IEnumerable<ActivationHandler> GetActivationHandlers()
        {
            return DependencyContainer.Current.ResolveAll<ActivationHandler>().OrderBy(h => h.Priority);
        }

        private bool IsInteractive(object args)
        {
            return args is IActivatedEventArgs;
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = _navigationService.CanGoBack ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
        }

        private void ActivationService_BackRequested(object sender, BackRequestedEventArgs e)
        {
            var result = _navigationService.GoBack();
            e.Handled = result;
        }
    }
}