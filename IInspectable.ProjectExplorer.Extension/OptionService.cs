#region Using Directives

using System;
using System.IO;

using JetBrains.Annotations;

using IInspectable.Utilities.Logging;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed class OptionService {

        readonly Logger _logger = Logger.Create<OptionService>();

        string _projectsRoot;

        public OptionService(SolutionService solutionService) {
            SolutionService = solutionService;

            SolutionService.AfterCloseSolution += OnAfterCloseSolution;
        }
        
        public const string OptionKey = "ProjectExplorerExtension";

        public SolutionService SolutionService { get; }

        public string ProjectsRoot {
            get {
                if (String.IsNullOrWhiteSpace(_projectsRoot)) {
                    return SolutionService.GetSolutionDirectory();
                }
                return _projectsRoot;

            }
            set { _projectsRoot = value; }
        }

        public void LoadOptions(Stream stream) {

            using(var sr = new StreamReader(stream)) {
                _projectsRoot = FromSolutionRelativePath(sr.ReadLine());
            }
        }
        
        public void SaveOptions(Stream stream) {
            using(var sw = new StreamWriter(stream)) {
                sw.Write(ToSolutionRelativePath(_projectsRoot));
            }
        }

        void OnAfterCloseSolution(object sender, EventArgs e) {
            _projectsRoot = null;
        }

        string FromSolutionRelativePath([CanBeNull] string savedPath) {

            if (String.IsNullOrEmpty(savedPath)) {
                _logger.Info($"{nameof(FromSolutionRelativePath)}: Path is null or empty");
                return null;
            }

            // Für den Fall, dass doch mal ein nicht relativer Pfad gespeichert wurde
            if (Path.IsPathRooted(savedPath)) {
                _logger.Info($"{nameof(FromSolutionRelativePath)}: Path is rooted {savedPath}");
                return savedPath;
            }

            var solutionDirectory = SolutionService.GetSolutionDirectory();
            if (solutionDirectory == null) {
                _logger.Warn($"{nameof(FromSolutionRelativePath)}: No solution directory!");
                return null;
            }

            var absolutePath = new FileInfo(Path.Combine(solutionDirectory, savedPath)).FullName;

            _logger.Info($"{nameof(FromSolutionRelativePath)}: Absolute path: {absolutePath}");

            return absolutePath;
        }

        string ToSolutionRelativePath(string projectsRoot) {

            _logger.Info($"{nameof(ToSolutionRelativePath)}: {(projectsRoot ?? "<Null>")}");

            if(String.IsNullOrEmpty(projectsRoot)) {
                _logger.Info($"{nameof(ToSolutionRelativePath)}: Path is null or empty");
                return null;
            }

            var solutionDirectory = SolutionService.GetSolutionDirectory();
            if (solutionDirectory == null) {
                _logger.Warn($"{nameof(ToSolutionRelativePath)}: No solution directory!");

                return projectsRoot;
            }

            var relPath = Utilities.IO.PathHelper.GetRelativePath(solutionDirectory, projectsRoot);

            _logger.Info($"{nameof(ToSolutionRelativePath)}: Relative path is {relPath}");

            return relPath;
        }
    }
}