using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using JetBrains.Annotations;
using Microsoft.VisualStudio.Shell.Interop;

namespace IInspectable.ProjectExplorer.Extension {

    public class ProjectExplorerViewModel: ViewModelBase {

        readonly ProjectService _projectService;

        ObservableCollection<ProjectViewModel> _projects;

        public ProjectExplorerViewModel(ProjectService projectService) {
            _projectService = projectService;
            _projects       = new ObservableCollection<ProjectViewModel>();

            _projectService.AfterLoadProject += OnAfterLoadProject;
            _projectService.BeforeUnloadProject += OnBeforeUnloadProject;
            _projectService.AfterOpenProject += OnAfterOpenProject;
            _projectService.BeforeRemoveProject += OnBeforeRemoveProject;
        }

        void OnBeforeRemoveProject(object sender, ProjectEventArgs e) {

            var guid=_projectService.GetProjectGuid(e.RealHierarchie);

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
        ProjectViewModel FindProjectViewModel(IVsHierarchy pHierarchy) {
            var projectGuid = _projectService.GetProjectGuid(pHierarchy);
            return FindProjectViewModel(projectGuid);
        }

        [CanBeNull]
        ProjectViewModel FindProjectViewModel(Guid projectGuid) {
            // TODO Performance Optimierung
            return _projects.FirstOrDefault(p => p.ProjectGuid == projectGuid);
        }

        public void Reload() {

            var projectFiles = _projectService.LoadProjectFiles();

            var projects = _projectService.BindToHierarchy(projectFiles);

            foreach(var projectVm in projects) {
                projectVm.SetParent(this);
            }

            var orderedProjects=projects.OrderByDescending(pvm => pvm.Status)
                                        .ThenBy(pvm => pvm.Name);
            
            var oldProjects = Projects;
            Projects = new ObservableCollection<ProjectViewModel>(orderedProjects);

            foreach (var projectVm in oldProjects) {
                projectVm.SetParent(null);
            }
        }

        [NotNull]
        public ProjectService ProjectService {
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
            get { return _projectService.ProjectsRoot; }
        }
    }

}