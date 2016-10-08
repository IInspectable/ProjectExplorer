using System;
using System.IO;
using JetBrains.Annotations;
using Microsoft.VisualStudio.Shell.Interop;

namespace IInspectable.ProjectExplorer.Extension {

    public class ProjectViewModel: ViewModelBase {

        readonly ProjectFile _projectFile;
        IVsHierarchy _pHierarchy;
        ProjectExplorerViewModel _parent;

        public ProjectViewModel(ProjectFile projectFile) {
            _projectFile = projectFile;
        }

        [CanBeNull]
        public ProjectExplorerViewModel Parent {
            get { return _parent; }
        }

        public string Name {
            get { return _projectFile.Name; }
        }

        public string Directory {
            get { return Path.GetDirectoryName(_projectFile.Path); }
        }

        public Guid ProjectGuid {
            get { return _projectFile.ProjectGuid; }
        }

        public ProjectStatus Status {
            get {

                if(_pHierarchy == null || _parent==null) {
                    return ProjectStatus.Unavailable;
                }

                return _parent.ProjectService.IsProjectUnloaded(_pHierarchy) ? 
                       ProjectStatus.Unloaded: ProjectStatus.Loaded;                
            }
        }

        public void Bind(IVsHierarchy pHierarchy) {
            _pHierarchy = pHierarchy;
            NotifyAllPropertiesChanged();

        }

        public void Unbind() {
            _pHierarchy = null;
            NotifyAllPropertiesChanged();
        }

        public void SetParent(ProjectExplorerViewModel parent) {
            _parent = parent;
            NotifyAllPropertiesChanged();
        }
    }
}