#region Using Directives

using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Collections.ObjectModel;

using JetBrains.Annotations;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    class ProjectExplorerViewModel: ViewModelBase {

        readonly ProjectService _projectService;
        readonly OptionService  _optionService;

        ObservableCollection<ProjectViewModel> _projects;

        internal ProjectExplorerViewModel(ProjectService projectService, OptionService optionService) {

            _projectService = projectService;
            _optionService   = optionService;
            _projects       = new ObservableCollection<ProjectViewModel>();

            _projectService.AfterLoadProject    += OnAfterLoadProject;
            _projectService.BeforeUnloadProject += OnBeforeUnloadProject;
            _projectService.AfterOpenProject    += OnAfterOpenProject;
            _projectService.BeforeRemoveProject += OnBeforeRemoveProject;
        }

        void OnBeforeRemoveProject(object sender, ProjectEventArgs e) {

            var guid= e.RealHierarchie.GetProjectGuid();

            // Wir können an dieser Stelle nicht unterscheiden, ob das Projekt nur entladen
            // oder entfernt wurde => Wir verzögern das Update. Wenn das Projekt entfernt wurde
            // wird es auch keine Hierarchie mehr geben...
            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() => {
                    var hier=_projectService.GetHierarchyByProjectGuid(guid);
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

            var projectFiles = _projectService.LoadProjectFiles(ProjectsRoot);

            var projects = _projectService.BindToHierarchy(projectFiles);

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
        internal ProjectService ProjectService {
            get { return _projectService; }
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
    }
}