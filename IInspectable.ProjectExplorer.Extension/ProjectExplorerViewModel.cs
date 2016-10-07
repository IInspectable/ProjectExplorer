using System.Collections.ObjectModel;
using System.Linq;

namespace IInspectable.ProjectExplorer.Extension {

    public class ProjectExplorerViewModel: ViewModelBase {

        readonly ProjectExplorerController _controller;

        ObservableCollection<ProjectViewModel> _projects;

        public ProjectExplorerViewModel(ProjectExplorerController controller) {
            _controller = controller;
        }

        public void Reload() {
            var projectFiles = _controller.LoadProjectFiles();

            var projects = _controller.BindProjectFiles(projectFiles)
                                      .OrderByDescending(pvm => pvm.Status)
                                      .ThenBy(pvm => pvm.Name);

            Projects = new ObservableCollection<ProjectViewModel>(projects);
        }

        public ObservableCollection<ProjectViewModel> Projects {
            get { return _projects; }
            private set {
                _projects = value;
                OnPropertyChanged();
            }
        }

    }

}