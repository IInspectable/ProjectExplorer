#region Using Directives

using System;
using System.Diagnostics;
using IInspectable.Utilities.Logging;
using JetBrains.Annotations;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    class ProjectViewModel: ViewModelBase, IVsHierarchyEvents {

        static readonly Logger Logger = Logger.Create<ProjectViewModel>();

        [NotNull]
        readonly ProjectFile _projectFile;

        [CanBeNull]
        Hierarchy _hierarchy;

        [CanBeNull]
        ProjectExplorerViewModel _parent;
        readonly string _uniqueNameOfProject;
        uint _eventCookie;
        
        public ProjectViewModel(ProjectFile projectFile, string uniqueNameOfProject) {

            if (projectFile == null) {
                throw new ArgumentNullException(nameof(projectFile));
            }

            if (uniqueNameOfProject == null) {
                throw new ArgumentNullException(nameof(uniqueNameOfProject));
            }

            _projectFile         = projectFile;
            _uniqueNameOfProject = uniqueNameOfProject;
        }

        [NotNull]
        public string UniqueNameOfProject {
            get { return _uniqueNameOfProject; }
        }

        [CanBeNull]
        public ProjectExplorerViewModel Parent {
            get { return _parent; }
        }

        public string Name {
            get { return _projectFile.Name; }
        }

        public string Directory {
            get { return System.IO.Path.GetDirectoryName(_projectFile.Path); }
        }

        public string Path {
            get { return _projectFile.Path; }
        }

        public ImageMoniker ImageMoniker {
            get {

                switch(Status) {
                    case ProjectStatus.Loaded:
                    case ProjectStatus.Unloaded:
                        // ReSharper disable once PossibleNullReferenceException _hierarchy ist nicht null, wenn Loaded
                        return _hierarchy.GetImageMoniker();                    
                }

                return KnownMonikers.NewDocumentCollection;
            }
        }

        public ProjectStatus Status {
            get {

                if(_hierarchy == null || _parent==null) {
                    return ProjectStatus.Closed;
                }

                return _hierarchy.IsProjectUnloaded() ? ProjectStatus.Unloaded: ProjectStatus.Loaded;                
            }
        }

        public int Open() {
            return _parent?.SolutionService.OpenProject(_projectFile.Path) ?? VSConstants.S_OK;
        }

        public int Remove() {      
            return _hierarchy?.CloseProject() ?? VSConstants.S_OK;
        }

        public int Load() {
            return _hierarchy?.LoadProject() ?? VSConstants.S_OK;
        }

        public int Unload() {
            return _hierarchy?.UnloadProject() ?? VSConstants.S_OK;
        }

        public int DefaultAction() {
            switch (Status) {
                case ProjectStatus.Closed:
                    return Open();
                case ProjectStatus.Unloaded:
                    return Load();
                case ProjectStatus.Loaded:
                    return Unload();
            }
            return VSConstants.S_OK;
        }

        public void OpenFolderInFileExplorer() {

            // TODO Error Handling
            string args = $"/e, /select, \"{Path}\"";

            ProcessStartInfo info = new ProcessStartInfo {
                FileName = "explorer",
                Arguments = args
            };
            Process.Start(info);
        }

        public void Bind([CanBeNull] Hierarchy hierarchy) {

            UnadviseHierarchyEvents();

            _hierarchy = hierarchy;

            AdviseHierarchyEvents();

            NotifyAllPropertiesChanged();
        }
       
        public void SetParent([NotNull] ProjectExplorerViewModel parent) {
            if(parent == null) {
                throw new ArgumentNullException(nameof(parent));
            }
            _parent = parent; 
        }

        public void Dispose() {
            UnadviseHierarchyEvents();
            _parent    = null;
            _hierarchy = null;
        }

        #region IVsHierarchyEvents

        void AdviseHierarchyEvents() {
            if(_eventCookie != 0) {
                Logger.Error($"{nameof(AdviseHierarchyEvents)}: event cookie not 0 ({_eventCookie})");
            }
            _eventCookie = _hierarchy?.AdviseHierarchyEvents(this) ?? 0;
        }

        void UnadviseHierarchyEvents() {
            if(_eventCookie != 0) {
                _hierarchy?.UnadviseHierarchyEvents(_eventCookie);
                _eventCookie = 0;
            }
        }

        int IVsHierarchyEvents.OnItemAdded(uint itemidParent, uint itemidSiblingPrev, uint itemidAdded) {
            return VSConstants.S_OK;
        }

        int IVsHierarchyEvents.OnItemsAppended(uint itemidParent) {
            return VSConstants.S_OK;
        }

        int IVsHierarchyEvents.OnItemDeleted(uint itemid) {
            return VSConstants.S_OK;
        }

        int IVsHierarchyEvents.OnPropertyChanged(uint itemid, int propid, uint flags) {
            if(propid ==(int)__VSHPROPID.VSHPROPID_IconHandle) {
                NotifyThisPropertyChanged(nameof(ImageMoniker));
            }
            return VSConstants.S_OK; 
        }

        int IVsHierarchyEvents.OnInvalidateItems(uint itemidParent) {
            return VSConstants.S_OK;
        }

        int IVsHierarchyEvents.OnInvalidateIcon(IntPtr hicon) {            
            return VSConstants.S_OK;
        }

        #endregion
    }
}