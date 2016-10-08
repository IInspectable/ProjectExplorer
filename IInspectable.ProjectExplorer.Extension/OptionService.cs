using System;
using System.IO;
using Microsoft.VisualStudio.Shell.Interop;

namespace IInspectable.ProjectExplorer.Extension {

    class OptionService {

        
        readonly Lazy< IVsSolution>_solution1;

        string _projectsRoot;

        public OptionService() {
            _solution1 = new Lazy<IVsSolution>(ProjectExplorerPackage.GetGlobalService<SVsSolution, IVsSolution>);
        }

        public string OptionKey { get { return "ProjectExplorerExtension"; } }

        public IVsSolution Solution {
            get { return _solution1.Value; }
        }

        public string ProjectsRoot {
            get {
                if(String.IsNullOrWhiteSpace(_projectsRoot)) {
                    return GetSolutionDirectory();
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
            using(var sw = new StreamWriter(stream)) {
                sw.Write(_projectsRoot);                
            }
        }

        string GetSolutionDirectory() {
            string solutionDirectory;
            string solutionFile;
            string userOptsFile;
            Solution.GetSolutionInfo(
                pbstrSolutionDirectory: out solutionDirectory,
                pbstrSolutionFile: out solutionFile,
                pbstrUserOptsFile: out userOptsFile);

            return solutionDirectory;
        }        
    }

}