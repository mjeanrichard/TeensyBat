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

            Application.Current.DispatcherUnhandledException += OnUnhandledException;

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