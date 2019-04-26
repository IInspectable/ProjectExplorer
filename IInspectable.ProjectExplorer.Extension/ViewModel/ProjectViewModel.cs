#region Using Directives

using System;
using JetBrains.Annotations;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    class ProjectViewModel: ItemViewModel, IVsHierarchyEvents {

        static readonly Logger Logger = Logger.Create<ProjectViewModel>();

        [NotNull]
        readonly ProjectFile _projectFile;

        [CanBeNull]
        Hierarchy _hierarchy;

        [CanBeNull]
        ProjectExplorerViewModel _parent;
        uint _eventCookie;
        
        public ProjectViewModel(ProjectFile projectFile) {

            if (projectFile == null) {
                throw new ArgumentNullException(nameof(projectFile));
            }
            
            _projectFile = projectFile;
        }

        [CanBeNull]
        public ProjectExplorerViewModel Parent {
            get { return _parent; }
        }

        public override string DisplayName {
            get { return _projectFile.Name; }
        }

        public override ImageMoniker ImageMoniker {
            get {

                switch (Status) {
                    case ProjectStatus.Loaded:
                    case ProjectStatus.Unloaded:
                        // ReSharper disable once PossibleNullReferenceException _hierarchy ist nicht null, wenn Loaded
                        return _hierarchy.GetImageMoniker();
                }

                return KnownMonikers.NewDocumentCollection;
            }
        }

        public override bool IsSelected {
            get { return _parent?.SelectionService.IsSelected(this) ?? false; }
            set {
                if (value) {
                    _parent?.SelectionService.AddSelection(this);
                } else {
                    _parent?.SelectionService.RemoveSelection(this);
                }
            }
        }

        public string Directory {
            get { return System.IO.Path.GetDirectoryName(_projectFile.Path); }
        }

        public string Path {
            get { return _projectFile.Path; }
        }
        
        public ProjectStatus Status {
            get {

                if(_hierarchy == null || _parent==null) {
                    return ProjectStatus.Closed;
                }

                return _hierarchy.IsProjectUnloaded()?ProjectStatus.Unloaded:ProjectStatus.Loaded;                
            }
        }

        public override void Filter(SearchContext context) {
            Visible = context.IsMatch(DisplayName);

            if(IsSelected && !Visible) {
                IsSelected = false;
            }
        }

        public int Open() {
            return _parent?.SolutionService.OpenProject(_projectFile.Path) ?? VSConstants.E_FAIL;
        }

        public int Close() {      
            return _hierarchy?.CloseProject() ?? VSConstants.S_OK;
        }

        public int Reload() {
            return _hierarchy?.ReloadProject() ?? VSConstants.E_FAIL;
        }

        public int Unload() {
            return _hierarchy?.UnloadProject() ?? VSConstants.E_FAIL;
        }
        
        public void BindToHierarchy([CanBeNull] Hierarchy hierarchy) {

            UnadviseHierarchyEvents();

            _hierarchy = hierarchy;

            AdviseHierarchyEvents();

            NotifyAllPropertiesChanged();
        }
       
        public void SetParent([NotNull] ProjectExplorerViewModel parent) {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent)); 
        }

        public void Dispose() {
            IsSelected = false;
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