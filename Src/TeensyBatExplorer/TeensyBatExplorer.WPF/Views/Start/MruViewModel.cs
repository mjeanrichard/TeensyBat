using System.Threading.Tasks;

using Nito.Mvvm;

using TeensyBatExplorer.Core;
using TeensyBatExplorer.WPF.Infrastructure;

namespace TeensyBatExplorer.WPF.Views.Start
{
    public class MruViewModel
    {
        private readonly ProjectMruEntry _projectMruEntry;
        private readonly ProjectManager _projectManager;
        private readonly BaseViewModel _parentViewModel;
        private readonly NavigationService _navigationService;

        public MruViewModel(ProjectMruEntry projectMruEntry, ProjectManager projectManager, BaseViewModel parentViewModel, NavigationService navigationService)
        {
            _projectMruEntry = projectMruEntry;
            _projectManager = projectManager;
            _parentViewModel = parentViewModel;
            _navigationService = navigationService;
            OpenCommand = new AsyncCommand(Open);
        }

        public AsyncCommand OpenCommand { get; private set; }

        private async Task Open()
        {
            using (_parentViewModel.BeginBusy("Öffne Projekt..."))
            {
                await Task.Run(async () => await _projectManager.OpenProject(_projectMruEntry.FullPath));
                await _navigationService.NavigateToProjectPage();
            }
        }

        public string Name => _projectMruEntry.ProjectName;
    }
}