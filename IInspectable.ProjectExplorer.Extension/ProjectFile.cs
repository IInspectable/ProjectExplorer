using System;
using System.Xml;

namespace IInspectable.ProjectExplorer.Extension {

    public class ProjectFile {

        ProjectFile(string name, string path, string assemblyName, Guid projectGuid) {
            Name        = name;
            Path        = path;
            AssemblyName = assemblyName;
            ProjectGuid = projectGuid;
        }

        public Guid ProjectGuid { get; }
        public string Name { get; }
        public string Path { get; }
        public string AssemblyName { get; set; }

        public static ProjectFile FromFile(string fileName) {

            // TODO Im Fehlerfall null liefern und loggen.
            var xmlDocument = new XmlDocument();
            xmlDocument.Load(fileName);
            if (xmlDocument.DocumentElement?.NamespaceURI != "http://schemas.microsoft.com/developer/msbuild/2003") {
                throw new ArgumentException("Not a supported C# project file: \"" + fileName + "\"");
            }

            var assemblyName = "";
            var assemblyNameElement = xmlDocument.GetElementsByTagName("AssemblyName");
            if (assemblyNameElement.Count > 0) {
                assemblyName = assemblyNameElement[0].FirstChild.Value;
            }

            var projectGuid = Guid.Parse(xmlDocument.GetElementsByTagName("ProjectGuid")[0].FirstChild.Value);

            return new ProjectFile(name: System.IO.Path.GetFileNameWithoutExtension(fileName), 
                                   path: fileName, 
                                   assemblyName: assemblyName, 
                                   projectGuid: projectGuid);
        }
    }
}