#region Using Directives

using Microsoft.VisualStudio.Imaging.Interop;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    public sealed record ProjectFile {

        public string       Name         { get; init; }
        public string       Path         { get; init; }
        public ImageMoniker ImageMoniker { get; init; }

        public static ProjectFile FromFile(string fileName, ImageMoniker imageMoniker) {
            return new() {
                Name         = System.IO.Path.GetFileNameWithoutExtension(fileName),
                Path         = fileName,
                ImageMoniker = imageMoniker
            };
        }

    }

}