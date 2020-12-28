#region Using Directives

using System.Xml;

using Microsoft.VisualStudio.Imaging.Interop;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    public class ProjectFile {

        ProjectFile(string name, string path, ImageMoniker imageMoniker) {
            Name         = name;
            Path         = path;
            ImageMoniker = imageMoniker;
        }

        public string       Name         { get; }
        public string       Path         { get; }
        public ImageMoniker ImageMoniker { get; }

        public static ProjectFile FromFile(string fileName, ImageMoniker imageMoniker) {

            return new ProjectFile(name: System.IO.Path.GetFileNameWithoutExtension(fileName),
                                   path: fileName,
                                   imageMoniker: imageMoniker);
        }

    }

}