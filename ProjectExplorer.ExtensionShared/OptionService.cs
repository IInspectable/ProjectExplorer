#region Using Directives

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;

using Microsoft.VisualStudio.Shell;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    [Export]
    sealed class OptionService {

        static readonly Logger Logger = Logger.Create<OptionService>();

        string _projectsRoot;

        [ImportingConstructor]
        public OptionService(SolutionService solutionService) {
            SolutionService = solutionService;

            SolutionService.AfterCloseSolution += OnAfterCloseSolution;
        }
        
        public const string OptionKey = "ProjectExplorerExtension";

        public SolutionService SolutionService { get; }

        public string ProjectsRoot {
            get {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (String.IsNullOrWhiteSpace(_projectsRoot)) {

                    var solutionDir = SolutionService.GetSolutionDirectory();
                    if (String.IsNullOrEmpty(solutionDir)) {
                        return solutionDir;
                    }

                    // Wenn die Solution bereits gespeichert wurde, dann ist sie per se unsere Wurzel
                    // Andernfalls gehen wir zum �bergeordneten Verzeichnis.
                    var solutionFile = SolutionService.GetSolutionFile();
                    if (File.Exists(solutionFile) || !Directory.Exists(solutionDir)) {
                        return solutionDir;
                    }

                    var dirInfo = new DirectoryInfo(solutionDir);
                    return dirInfo.Parent?.FullName;
                }

                return _projectsRoot;

            }
            set => _projectsRoot = value;
        }

        public void LoadOptions(Stream stream) {

            try {
                ThreadHelper.ThrowIfNotOnUIThread();
                var xDoc = XDocument.Load(stream);

                _projectsRoot= FromSolutionRelativePath(xDoc.Root?.Descendants(nameof(ProjectsRoot)).FirstOrDefault()?.Value);

            } catch (Exception ex) {
                Logger.Error(ex, "Fehler beim Laden der Optionen");
            }
        }

        public void SaveOptions(Stream stream) {
            ThreadHelper.ThrowIfNotOnUIThread();
            var xDoc = new XDocument(
                new XElement(nameof(OptionService),
                    new XElement(nameof(ProjectsRoot), ToSolutionRelativePath(_projectsRoot))
            ));

            xDoc.Save(stream);
        }

        void OnAfterCloseSolution(object sender, EventArgs e) {
            _projectsRoot = null;
        }

        string FromSolutionRelativePath([CanBeNull] string savedPath) {

            ThreadHelper.ThrowIfNotOnUIThread();
            
            if (String.IsNullOrEmpty(savedPath)) {
                Logger.Info($"{nameof(FromSolutionRelativePath)}: Path is null or empty");
                return null;
            }

            // F�r den Fall, dass doch mal ein nicht relativer Pfad gespeichert wurde
            if (Path.IsPathRooted(savedPath)) {
                Logger.Info($"{nameof(FromSolutionRelativePath)}: Path is rooted {savedPath}");
                return savedPath;
            }

            var solutionDirectory = SolutionService.GetSolutionDirectory();
            if (solutionDirectory == null) {
                Logger.Warn($"{nameof(FromSolutionRelativePath)}: No solution directory!");
                return null;
            }

            var absolutePath = new FileInfo(Path.Combine(solutionDirectory, savedPath)).FullName;

            Logger.Info($"{nameof(FromSolutionRelativePath)}: Absolute path: {absolutePath}");

            return absolutePath;
        }

        string ToSolutionRelativePath(string projectsRoot) {

            ThreadHelper.ThrowIfNotOnUIThread();

            Logger.Info($"{nameof(ToSolutionRelativePath)}: {(projectsRoot ?? "<Null>")}");

            if(String.IsNullOrEmpty(projectsRoot)) {
                Logger.Info($"{nameof(ToSolutionRelativePath)}: Path is null or empty");
                return null;
            }

            var solutionDirectory = SolutionService.GetSolutionDirectory();
            if (solutionDirectory == null) {
                Logger.Warn($"{nameof(ToSolutionRelativePath)}: No solution directory!");

                return projectsRoot;
            }

            var relPath = Utilities.IO.PathHelper.GetRelativePath(solutionDirectory, projectsRoot);

            Logger.Info($"{nameof(ToSolutionRelativePath)}: Relative path is {relPath}");

            return relPath;
        }
    }
}