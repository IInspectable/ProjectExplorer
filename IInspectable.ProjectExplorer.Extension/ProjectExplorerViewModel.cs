using System;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.VisualStudio.Shell.Interop;

namespace IInspectable.ProjectExplorer.Extension {

    public class ProjectExplorerViewModel: ViewModelBase {

        readonly ProjectService _projectService;

        ObservableCollection<ProjectViewModel> _projects;

        public ProjectExplorerViewModel(ProjectService projectService) {
            _projectService = projectService;

            _projectService.AfterLoadProject += OnAfterLoadProject;
            _projectService.BeforeUnloadProject += OnBeforeUnloadProject;
            _projectService.AfterOpenProject += OnAfterOpenProject;
            _projectService.BeforeRemoveProject += OnBeforeRemoveProject;
        }

        void OnBeforeRemoveProject(object sender, ProjectEventArgs e) {
            var projectVm = FindProjectViewModel(e.RealHierarchie);

            projectVm?.Unbind();
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
            var projectGuid = _projectService.GetGuidOfProject(pHierarchy);
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

            Projects = new ObservableCollection<ProjectViewModel>(orderedProjects);
        }

        [NotNull]
        public ProjectService ProjectService {
            get { return _projectService; }
        }

        public ObservableCollection<ProjectViewModel> Projects {
            get { return _projects; }
            private set {
                _projects = value;
                NotifyPropertyChanged();
            }
        }
    }

}