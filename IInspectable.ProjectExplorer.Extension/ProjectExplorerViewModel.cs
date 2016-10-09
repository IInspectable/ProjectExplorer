#region Using Directives

using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Windows.Input;
using JetBrains.Annotations;
using Microsoft.VisualStudio.Shell;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    class ProjectExplorerViewModel: ViewModelBase {

        readonly SolutionService _solutionService;
        readonly OptionService  _optionService;
        readonly OleMenuCommandService _menuCommandService;

        ObservableCollection<ProjectViewModel> _projects;

        internal ProjectExplorerViewModel(IServiceProvider serviceProvider, SolutionService solutionService, OptionService optionService, OleMenuCommandService menuCommandService) {

            _solutionService    = solutionService;
            _optionService      = optionService;
            _menuCommandService = menuCommandService;

            _projects       = new ObservableCollection<ProjectViewModel>();

            _solutionService.AfterLoadProject    += OnAfterLoadProject;
            _solutionService.BeforeUnloadProject += OnBeforeUnloadProject;
            _solutionService.AfterOpenProject    += OnAfterOpenProject;
            _solutionService.BeforeRemoveProject += OnBeforeRemoveProject;

            // TODO Command Modell
            RefreshCommand.Initialize(serviceProvider, this);
        }

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

            var p = Mouse.GetPosition(null);

            _menuCommandService.ShowContextMenu(commandId, x, y);
        }

        public void ShowProjectItemContextMenu(int x, int y) {

            var commandId = new CommandID(PackageGuids.ProjectExplorerWindowPackageCmdSetGuid,
                                          PackageIds.ProjectItemContextMenu);

            var p = Mouse.GetPosition(null);

            _menuCommandService.ShowContextMenu(commandId, x, y);
        }

    }
}