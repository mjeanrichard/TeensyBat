// 
// Teensy Bat Explorer - Copyright(C) 2020 Meinrad Jean-Richard
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

using System.Windows;
using System.Windows.Threading;

using MaterialDesignThemes.Wpf;

using TeensyBatExplorer.Core;
using TeensyBatExplorer.WPF.Infrastructure;

using Unity;

namespace TeensyBatExplorer.WPF
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IUnityContainer _rootContainer;
        private ISnackbarMessageQueue _snackbarMessageQueue;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _rootContainer = new UnityContainer();
            RegisterServices(_rootContainer);

            _snackbarMessageQueue = _rootContainer.Resolve<ISnackbarMessageQueue>();

            Current.DispatcherUnhandledException += OnUnhandledException;

            Current.MainWindow = _rootContainer.Resolve<MainWindow>();
            Current.MainWindow.Show();

            NavigationService navigationService = _rootContainer.Resolve<NavigationService>();
            await navigationService.NavigateToStartPage();
        }

        private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _snackbarMessageQueue.Enqueue(e.Exception.Message);
            e.Handled = true;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _rootContainer.Dispose();
            base.OnExit(e);
        }

        private void RegisterServices(IUnityContainer rootContainer)
        {
            rootContainer.RegisterSingleton<NavigationService>();
            rootContainer.RegisterSingleton<ProjectManager>();
            rootContainer.RegisterSingleton<ISnackbarMessageQueue, SnackbarMessageQueue>();
        }
    }
}