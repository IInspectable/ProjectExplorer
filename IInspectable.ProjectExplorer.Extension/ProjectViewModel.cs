using System;
using System.IO;
using JetBrains.Annotations;
using Microsoft.VisualStudio.Shell.Interop;

namespace IInspectable.ProjectExplorer.Extension {

    public class ProjectViewModel: ViewModelBase {

        [NotNull]
        readonly ProjectFile _projectFile;
        [CanBeNull]
        IVsHierarchy _pHierarchy;
        [CanBeNull]
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

        // TODO Confirmation/Fehlerbehandlung
        public void Open() {
            _parent?.ProjectService.OpenProject(_projectFile.Path);
        }

        // TODO Confirmation/Fehlerbehandlung
        public void Close() {
            if (_pHierarchy == null) {
                return;
            }
            _parent?.ProjectService.CloseProject(_pHierarchy);
        }

        // TODO Confirmation/Fehlerbehandlung
        public void Load() {
            if(_pHierarchy == null) {
                return;
            }
            _parent?.ProjectService.LoadProject(_pHierarchy);
        }

        // TODO Confirmation/Fehlerbehandlung
        public void Unload() {
            if (_pHierarchy == null) {
                return;
            }
            _parent?.ProjectService.UnloadProject(_pHierarchy);
        }

        // TODO Confirmation/Fehlerbehandlung
        public void DefaultAction() {
            switch (Status) {
                case ProjectStatus.Unavailable:
                    Open();
                    break;
                case ProjectStatus.Unloaded:
                    Load();
                    break;
                case ProjectStatus.Loaded:
                    Unload();
                    break;
            }
        }

        public void Bind(IVsHierarchy pHierarchy) {
            _pHierarchy = pHierarchy;
            NotifyAllPropertiesChanged();

        }

        public void SetParent(ProjectExplorerViewModel parent) {
            _parent = parent;
        }
    }
}