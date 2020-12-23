#region Using Directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Linq;
using System.Threading;

using JetBrains.Annotations;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell.Interop;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    class ProjectSynchronizerService: IVsHierarchyEvents {

        static readonly Logger Logger = Logger.Create<ProjectSynchronizerService>();

        readonly SolutionService                        _solutionService;
        readonly ObservableCollection<ProjectViewModel> _projects;

        readonly Dictionary<string, HierarchyData> _projectHierarchyData;

        IObserver<EventArgs> _updateRequestQueue;

        public ProjectSynchronizerService(SolutionService solutionService, ObservableCollection<ProjectViewModel> projects) {

            _projectHierarchyData = new Dictionary<string, HierarchyData>();
            _solutionService      = solutionService;
            _projects             = projects;

            WireEvents();

            Observable.Create<EventArgs>(
                           observer => {
                               _updateRequestQueue = observer;
                               return () => _updateRequestQueue = null;
                           })
                      .Throttle(TimeSpan.FromMilliseconds(200))
                      .ObserveOn(SynchronizationContext.Current)
                      .Subscribe(_ => UpdateState());

        }

        public void Dispose() {
            UnwireEvents();
            _updateRequestQueue.OnCompleted();
        }

        [CanBeNull]
        public Hierarchy HierarchyFromViewModel(ProjectViewModel model) {

            if (model == null) {
                return null;
            }

            if (_updateStateRequested) {
                UpdateState();
            }

            if (_projectHierarchyData.TryGetValue(model.Path.ToLower(), out var hierarchy)) {
                return hierarchy.Hierarchy;
            }

            return null;
        }

        bool _updateStateRequested;

        void InvalidateState() {
            _updateStateRequested = true;
            _updateRequestQueue?.OnNext(EventArgs.Empty);
        }

        void UpdateState() {

            _updateStateRequested = false;

            RebuildHierarchyData();

            foreach (var project in _projects) {
                var hierarchy = HierarchyFromViewModel(project);
                SyncProject(hierarchy, project);
            }

        }

        static void SyncProject(Hierarchy hierarchy, ProjectViewModel project) {

            if (hierarchy != null) {
                project.Status       = hierarchy.IsProjectUnloaded() ? ProjectStatus.Unloaded : ProjectStatus.Loaded;
                project.ImageMoniker = hierarchy.GetImageMoniker();

            } else {
                project.Status       = ProjectStatus.Closed;
                project.ImageMoniker = KnownMonikers.NewDocumentCollection;
            }
        }

        void RebuildHierarchyData() {

            foreach (var hd in _projectHierarchyData.Values) {
                hd.UnadviseHierarchyEvents();
            }

            _projectHierarchyData.Clear();

            foreach (var hierarchy in _solutionService.EnumerateHierarchy()) {

                var fullPath = hierarchy.FullPath;
                if (fullPath != null) {
                    var hd = new HierarchyData(hierarchy);
                    _projectHierarchyData[fullPath.ToLower()] = hd;

                    hd.AdviseHierarchyEvents(this);
                }
            }
        }

        #region Event Handler Methods

        void WireEvents() {
            _solutionService.AfterOpenSolution   += OnAfterOpenSolution;
            _solutionService.AfterCloseSolution  += OnAfterCloseSolution;
            _solutionService.AfterLoadProject    += OnAfterLoadProject;
            _solutionService.BeforeUnloadProject += OnBeforeUnloadProject;
            _solutionService.AfterOpenProject    += OnAfterOpenProject;
            _solutionService.BeforeRemoveProject += OnBeforeRemoveProject;

            _projects.CollectionChanged += OnProjectsChanged;
        }

        void UnwireEvents() {
            _solutionService.AfterOpenSolution   -= OnAfterOpenSolution;
            _solutionService.AfterCloseSolution  -= OnAfterCloseSolution;
            _solutionService.AfterLoadProject    -= OnAfterLoadProject;
            _solutionService.BeforeUnloadProject -= OnBeforeUnloadProject;
            _solutionService.AfterOpenProject    -= OnAfterOpenProject;
            _solutionService.BeforeRemoveProject -= OnBeforeRemoveProject;
            _projects.CollectionChanged          -= OnProjectsChanged;
        }

        void OnAfterOpenSolution(object sender, EventArgs e) {
            InvalidateState();
        }

        void OnAfterCloseSolution(object sender, EventArgs e) {
            Logger.Info($"{nameof(OnAfterCloseSolution)}");
            InvalidateState();
        }

        void OnBeforeRemoveProject(object sender, ProjectEventArgs e) {
            Logger.Info($"{nameof(OnBeforeRemoveProject)}: {e.RealHierarchie.FullPath}");
            InvalidateState();

        }

        void OnAfterOpenProject(object sender, ProjectEventArgs e) {
            Logger.Info($"{nameof(OnAfterOpenProject)}: {e.RealHierarchie.FullPath}");
            InvalidateState();
        }

        void OnBeforeUnloadProject(object sender, ProjectEventArgs e) {
            Logger.Info($"{nameof(OnBeforeUnloadProject)}: {e.StubHierarchie?.FullPath}");
            InvalidateState();
        }

        void OnAfterLoadProject(object sender, ProjectEventArgs e) {
            Logger.Info($"{nameof(OnAfterLoadProject)}: {e.RealHierarchie?.FullPath}");
            InvalidateState();
        }

        void OnProjectsChanged(object sender, NotifyCollectionChangedEventArgs e) {
            InvalidateState();
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
            if (propid == (int) __VSHPROPID.VSHPROPID_IconHandle) {
                InvalidateState();
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