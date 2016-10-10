#region Using Directives

using System;
using System.IO;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed class OptionService {

        string _projectsRoot;

        public OptionService(SolutionService solutionService) {
            SolutionService = solutionService;

            SolutionService.AfterCloseSolution += OnAfterCloseSolution;
        }
        
        public const string OptionKey = "ProjectExplorerExtension";

        public SolutionService SolutionService { get; }

        public string ProjectsRoot {
            get {
                if(String.IsNullOrWhiteSpace(_projectsRoot)) {
                    return SolutionService.GetSolutionDirectory();
                }
                // TODO Convert relative path to absolute path
                return _projectsRoot; 
                
            }
            set {
                // TODO Convert to relative path
                _projectsRoot = value;
            }
        }

        public void LoadOptions(Stream stream) {

            if(stream.Length == 0) {
                _projectsRoot = null;
            } else {
                using(var sr = new StreamReader(stream)) {                    
                    _projectsRoot = sr.ReadLine();
                }
            }
        }

        public void SaveOptions(Stream stream) {

            if(String.IsNullOrEmpty(_projectsRoot)) {
                return;
            }

            using(var sw = new StreamWriter(stream)) {
                sw.Write(_projectsRoot);                
            }
        }

        void OnAfterCloseSolution(object sender, EventArgs e) {
            _projectsRoot = null;
        }
    }
}