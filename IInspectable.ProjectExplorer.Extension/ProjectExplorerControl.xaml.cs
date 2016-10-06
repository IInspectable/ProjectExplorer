#region Using Directives

using System.Collections.Generic;
using System.IO;
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

    public partial class ProjectExplorerControl : UserControl {

        public ProjectExplorerControl() {
            InitializeComponent();
        }

        void OnButtonClick(object sender, RoutedEventArgs e) {

            var path = @"C:\ws\Roslyn";

            var projectFiles = new List<ProjectFile>();

            foreach(var file in Directory.EnumerateFiles(path, "*.csproj", SearchOption.AllDirectories)) {
                var projectFile = new ProjectFile(Path.GetFileNameWithoutExtension(file), file);

                projectFiles.Add(projectFile);
            }

            DataContext = projectFiles;          
        }
    }
}