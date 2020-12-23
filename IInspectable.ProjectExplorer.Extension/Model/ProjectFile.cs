#region Using Directives

using System.Xml;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    public class ProjectFile {

        ProjectFile(string name, string path) {
            Name = name;
            Path = path;
        }

        public string Name { get; }
        public string Path { get; }

        public static ProjectFile FromFile(string fileName) {

            return new ProjectFile(name: System.IO.Path.GetFileNameWithoutExtension(fileName),
                                   path: fileName);
        }

    }

}