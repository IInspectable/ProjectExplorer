#region Using Directives

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;

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
                if (String.IsNullOrWhiteSpace(_projectsRoot)) {
                    
                    var solutionDir=SolutionService.GetSolutionDirectory();
                    if (String.IsNullOrEmpty(solutionDir)) {
                        return solutionDir;
                    }

                    return Directory.GetParent(solutionDir)?.FullName;
                }
                return _projectsRoot;

            }
            set { _projectsRoot = value; }
        }

        public void LoadOptions(Stream stream) {

            try {
                var xDoc = XDocument.Load(stream);

                _projectsRoot= FromSolutionRelativePath(xDoc.Root?.Descendants(nameof(ProjectsRoot)).FirstOrDefault()?.Value);

            } catch (Exception ex) {
                Logger.Error(ex, "Fehler beim Laden der Optionen");
            }
        }

        public void SaveOptions(Stream stream) {
            
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

            if (String.IsNullOrEmpty(savedPath)) {
                Logger.Info($"{nameof(FromSolutionRelativePath)}: Path is null or empty");
                return null;
            }

            // Für den Fall, dass doch mal ein nicht relativer Pfad gespeichert wurde
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