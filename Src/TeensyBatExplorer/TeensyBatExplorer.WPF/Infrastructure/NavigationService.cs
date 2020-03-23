using System;
using System.Threading.Tasks;

using TeensyBatExplorer.WPF.Views.AddLogs;
using TeensyBatExplorer.WPF.Views.Project;
using TeensyBatExplorer.WPF.Views.Start;

using Unity;

namespace TeensyBatExplorer.WPF.Infrastructure
{
    public class NavigationService
    {
        private readonly IUnityContainer _container;

        public NavigationService(IUnityContainer container)
        {
            _container = container;
        }

        public BaseViewModel CurrentViewModel { get; private set; }

        public async Task NavigateToProjectPage()
        {
            await Navigate<ProjectPageViewModel>();
        }

        private async Task<T> Navigate<T>() where T : BaseViewModel
        {
            T currentViewModel = _container.Resolve<T>();
            await currentViewModel.Initialize();
            CurrentViewModel = currentViewModel;
            OnViewModelChanged?.Invoke(this, new EventArgs());
            await Task.Run(() => currentViewModel.Load());
            return currentViewModel;
        }

        public event EventHandler<EventArgs> OnViewModelChanged;

        public async Task NavigateToStartPage()
        {
            await Navigate<StartPageViewModel>();
        }

        public async Task NavigateToAddLogsPage()
        {
            await Navigate<AddLogsViewModel>();
        }
    }
}