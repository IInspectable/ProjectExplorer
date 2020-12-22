#region Using Directives

using System.Xml;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    public class ProjectFile {

        ProjectFile(string name, string path, string assemblyName) {
            Name         = name;
            Path         = path;
            AssemblyName = assemblyName;
        }

        public string Name { get; }
        public string Path { get; }
        public string AssemblyName { get; set; }

        public static ProjectFile FromFile(string fileName) {

            var xmlDocument = new XmlDocument();

            xmlDocument.Load(fileName + "");

            var assemblyName = "";
            var assemblyNameElement = xmlDocument.GetElementsByTagName("AssemblyName");
            if (assemblyNameElement.Count > 0) {
                assemblyName = assemblyNameElement[0].FirstChild.Value;
            }
            
            return new ProjectFile(name        : System.IO.Path.GetFileNameWithoutExtension(fileName), 
                                   path        : fileName, 
                                   assemblyName: assemblyName);
        }
    }
}