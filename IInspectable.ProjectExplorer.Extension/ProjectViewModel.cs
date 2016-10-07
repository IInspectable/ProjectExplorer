using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace IInspectable.ProjectExplorer.Extension {

    public class ProjectViewModel {

        readonly ProjectFile _projectFile;
        IVsHierarchy _pHierarchy;

        public ProjectViewModel(ProjectFile projectFile) {
            _projectFile = projectFile;
        }

        public string Name {
            get { return _projectFile.Name; }
        }

        public string Directory {
            get { return Path.GetDirectoryName(_projectFile.Path); }
        }

        public ProjectStatus Status {
            get {

                if(_pHierarchy == null) {
                    return ProjectStatus.Unavailable;
                }

                object status;
                var hr = _pHierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID5.VSHPROPID_ProjectUnloadStatus, out status);

                return ErrorHandler.Succeeded(hr)? ProjectStatus.Unloaded: ProjectStatus.Loaded;                
            }
        }

        public void Bind(IVsHierarchy pHierarchy) {
            _pHierarchy = pHierarchy;
        }

        public void Unbind() {
            _pHierarchy = null;
        }
    }

}