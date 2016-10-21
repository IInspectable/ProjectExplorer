#region Using Directives

using System;
using System.IO;
using System.Xml;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    public class ProjectFile {

        ProjectFile(string name, string path, string assemblyName, Guid projectGuid) {
            Name         = name;
            Path         = path;
            AssemblyName = assemblyName;
            ProjectGuid  = projectGuid;
        }

        public Guid ProjectGuid { get; }
        public string Name { get; }
        public string Path { get; }
        public string AssemblyName { get; set; }

        public static ProjectFile FromFile(string fileName) {

            var xmlDocument = new XmlDocument();

            xmlDocument.Load(fileName + "");

            if (xmlDocument.DocumentElement?.NamespaceURI != "http://schemas.microsoft.com/developer/msbuild/2003") {
                throw new FileLoadException($"\'{fileName}' is not a supported MSBuild project file");
            }

            var assemblyName = "";
            var assemblyNameElement = xmlDocument.GetElementsByTagName("AssemblyName");
            if (assemblyNameElement.Count > 0) {
                assemblyName = assemblyNameElement[0].FirstChild.Value;
            }

            var projectGuid = Guid.Empty;
            var projectGuidElement = xmlDocument.GetElementsByTagName("ProjectGuid");
            if(projectGuidElement.Count > 0) {
                projectGuid = Guid.Parse(projectGuidElement[0].FirstChild.Value);
            }
            
            return new ProjectFile(name        : System.IO.Path.GetFileNameWithoutExtension(fileName), 
                                   path        : fileName, 
                                   assemblyName: assemblyName, 
                                   projectGuid : projectGuid);
        }
    }
}