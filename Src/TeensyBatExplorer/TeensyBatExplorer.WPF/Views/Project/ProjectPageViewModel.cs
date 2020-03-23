using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using MaterialDesignThemes.Wpf;

using Nito.Mvvm;

using TeensyBatExplorer.Core;
using TeensyBatExplorer.Core.Models;
using TeensyBatExplorer.Core.Queries;
using TeensyBatExplorer.WPF.Annotations;
using TeensyBatExplorer.WPF.Infrastructure;

namespace TeensyBatExplorer.WPF.Views.Project
{
    public class ProjectPageViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;
        private readonly ProjectManager _projectManager;
        private IEnumerable<BatNode> _nodes;

        public ProjectPageViewModel(NavigationService navigationService, ProjectManager projectManager)
        {
            _navigationService = navigationService;
            _projectManager = projectManager;

            OpenNodeCommand = new AsyncCommand(async o => await OpenLog((int)o));

            AddToolbarButton(new ToolBarButton(AddLog, PackIconKind.PlusBoxMultipleOutline, "Logs hinzufügen"));
            AddToolbarButton(new ToolBarButton(SaveProject, PackIconKind.ContentSave, "Speichern"));
        }

        private async Task OpenLog(int nodeNumber)
        {
            //await _navigationService.NavigateToNodePage(nodeNumber);
        }

        public AsyncCommand OpenNodeCommand { get; set; }

        public BatProject BatProject { get; private set; }

        public IEnumerable<BatNode> Nodes
        {
            get => _nodes;
            private set
            {
                _nodes = value;
                OnPropertyChanged();
            }
        }

        private async Task AddLog()
        {
            await _navigationService.NavigateToAddLogsPage();
        }

        private async Task SaveProject()
        {
            //SaveProjectCommand command = new SaveProjectCommand();
            //await command.ExecuteAsyc(BatProject, _projectFile);
        }

        public override async Task Load()
        {
            List<BatNode> nodes = await _projectManager.GetNodes();
            await RunOnUiThread(() => Nodes = nodes);
        }

        public override Task Initialize()
        {
            BatProject = _projectManager.Project;
            return Task.CompletedTask;
        }
    }
}
