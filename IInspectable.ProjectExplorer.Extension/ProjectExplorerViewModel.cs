#region Using Directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using JetBrains.Annotations;
using Microsoft.VisualStudio.Shell;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    class ProjectExplorerViewModel: ViewModelBase {

        readonly SolutionService _solutionService;
        readonly OptionService  _optionService;
        readonly OleMenuCommandService _menuCommandService;

        ObservableCollection<ProjectViewModel> _projects;

        readonly List<Command> _commands;

        internal ProjectExplorerViewModel(SolutionService solutionService, OptionService optionService, OleMenuCommandService menuCommandService) {

            _solutionService    = solutionService;
            _optionService      = optionService;
            _menuCommandService = menuCommandService;
            _projects           = new ObservableCollection<ProjectViewModel>();

            _solutionService.AfterLoadProject    += OnAfterLoadProject;
            _solutionService.BeforeUnloadProject += OnBeforeUnloadProject;
            _solutionService.AfterOpenProject    += OnAfterOpenProject;
            _solutionService.BeforeRemoveProject += OnBeforeRemoveProject;

            _commands = new List<Command> {
                { RefreshCommand            = new RefreshCommand(this)},
                { OpenInFileExplorerCommand = new OpenInFileExplorerCommand(this)},
                { AddProjectCommand         = new AddProjectCommand(this)},
                { RemoveProjectCommand      = new RemoveProjectCommand(this)},
                { UnloadProjectCommand      = new UnloadProjectCommand(this)},
                { LoadProjectCommand        = new LoadProjectCommand(this)},
                { SettingsCommand           = new SettingsCommand(this)},
            };
            
            RegisterCommands();

            PropertyChanged += (o, e) => UpdateCommandStates();
        }

        void RegisterCommands() {
            foreach(var command in _commands) {
                command.Register(_menuCommandService);
            }
        }

        void UpdateCommandStates() {
            foreach (var command in _commands) {
                command.UpdateState();
            }
        }

        ProjectViewModel _selectedProject;

        public ProjectViewModel SelectedProject {
            get { return _selectedProject; }
            set {
                if(_selectedProject == value) {
                    return;
                }
                _selectedProject = value;
                NotifyPropertyChanged();
            }
        }

        public RefreshCommand RefreshCommand { get; }
        public OpenInFileExplorerCommand OpenInFileExplorerCommand { get; }
        public AddProjectCommand AddProjectCommand { get; }
        public RemoveProjectCommand RemoveProjectCommand { get; }
        public UnloadProjectCommand UnloadProjectCommand { get; }
        public LoadProjectCommand LoadProjectCommand { get; }
        public SettingsCommand SettingsCommand { get; }

        void OnBeforeRemoveProject(object sender, ProjectEventArgs e) {

            var guid= e.RealHierarchie.GetProjectGuid();

            // Wir können an dieser Stelle nicht unterscheiden, ob das Projekt nur entladen
            // oder entfernt wurde => Wir verzögern das Update. Wenn das Projekt entfernt wurde
            // wird es auch keine Hierarchie mehr geben...
            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() => {
                    var hier=_solutionService.GetHierarchyByProjectGuid(guid);
                    var projectVm = FindProjectViewModel(guid);
                    projectVm?.Bind(hier);

                }));
        }

        void OnAfterOpenProject(object sender, ProjectEventArgs e) {

            var projectVm = FindProjectViewModel(e.RealHierarchie);

            projectVm?.Bind(e.RealHierarchie);
        }

        void OnBeforeUnloadProject(object sender, ProjectEventArgs e) {

            var projectVm = FindProjectViewModel(e.RealHierarchie);

            projectVm?.Bind(e.StubHierarchie);
        }

        void OnAfterLoadProject(object sender, ProjectEventArgs e) {

            var projectVm= FindProjectViewModel(e.RealHierarchie);

            projectVm?.Bind(e.RealHierarchie);
        }

        [CanBeNull]
        ProjectViewModel FindProjectViewModel(Hierarchy hierarchy) {
            var projectGuid = hierarchy.GetProjectGuid();
            return FindProjectViewModel(projectGuid);
        }

        [CanBeNull]
        ProjectViewModel FindProjectViewModel(Guid projectGuid) {
            // TODO Performance Optimierung
            return _projects.FirstOrDefault(p => p.ProjectGuid == projectGuid);
        }

        public void Reload() {

            var projectFiles = _solutionService.LoadProjectFiles(ProjectsRoot);

            var projects = _solutionService.BindToHierarchy(projectFiles);

            foreach(var projectVm in projects) {
                projectVm.SetParent(this);
            }

            var orderedProjects=projects.OrderByDescending(pvm => pvm.Status)
                                        .ThenBy(pvm => pvm.Name);
            
            var oldProjects = Projects;

            Projects = new ObservableCollection<ProjectViewModel>(orderedProjects);
            SelectedProject = null;

            foreach (var projectVm in oldProjects) {
                projectVm.Dispose();
            }
        }

        [NotNull]
        internal SolutionService SolutionService {
            get { return _solutionService; }
        }

        public ObservableCollection<ProjectViewModel> Projects {
            get { return _projects; }
            private set {
                _projects = value;               
                NotifyAllPropertiesChanged();
            }
        }

        public string ProjectsRoot {
            get { return _optionService.ProjectsRoot; }
        }
        
        public void ShowSettingsButtonContextMenu(int x, int y) {
            
            var commandId = new CommandID(PackageGuids.ProjectExplorerWindowPackageCmdSetGuid, 
                                          PackageIds.SettingsButtonContextMenu);

            _menuCommandService.ShowContextMenu(commandId, x, y);
        }

        public void ShowProjectItemContextMenu(int x, int y) {

            var commandId = new CommandID(PackageGuids.ProjectExplorerWindowPackageCmdSetGuid,
                                          PackageIds.ProjectItemContextMenu);

            _menuCommandService.ShowContextMenu(commandId, x, y);
        }

    }
}