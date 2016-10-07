#region Using Directives

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    public class ProjectFile {

        public ProjectFile(string name, string path) {
            Name = name;
            Path = path;
        }

        public string Name { get; }
        public string Path { get; }
    }

    public enum ProjectStatus {
        Unavailable,
        Unloaded,
        Loaded,
    }

    public class ProjectViewModel {

        readonly ProjectFile _projectFile;

        public ProjectViewModel(ProjectFile projectFile) {
            _projectFile = projectFile;
        }

        public string Name {
            get { return _projectFile.Name; }
        }

        public string Directory {
            get { return Path.GetDirectoryName(_projectFile.Path); }
        }

        public ProjectStatus Status {
            get {
                switch(Name[0]) {
                    case 'C':
                        return ProjectStatus.Loaded;
                    case 'M':
                        return ProjectStatus.Unloaded;
                    default:
                        return ProjectStatus.Unavailable;
                }
            }
        }
    }

    public partial class ProjectExplorerControl : UserControl {

        public ProjectExplorerControl() {
            InitializeComponent();
        }

        void OnButtonClick(object sender, RoutedEventArgs e) {

            var path = @"C:\ws\Roslyn";

            var projectFiles = new List<ProjectViewModel>();

            foreach(var file in Directory.EnumerateFiles(path, "*.csproj", SearchOption.AllDirectories)) {
                var projectFile = new ProjectFile(Path.GetFileNameWithoutExtension(file), file);

                projectFiles.Add(new ProjectViewModel(projectFile));
            }

            DataContext = projectFiles.OrderByDescending(pvm=> pvm.Status)
                                      .ThenBy(pvm=>pvm.Name)
                                      .ToList();          
        }
    }
}