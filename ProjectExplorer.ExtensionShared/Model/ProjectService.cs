#region Using Directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

using JetBrains.Annotations;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;

using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;

#endregion

namespace IInspectable.ProjectExplorer.Extension; 

class ProjectService {

    static readonly Logger Logger = Logger.Create<ProjectService>();

    readonly SolutionService                        _solutionService;
    readonly ObservableCollection<ProjectViewModel> _projects;
    readonly Dictionary<string, HierarchyData>      _projectHierarchyData;
    readonly IObserver<EventArgs>                   _updateRequestSource;
    readonly IDisposable                            _updateRequestConnection;
    readonly HierarchyEvents                        _hierarchyEvents;

    public ProjectService(SolutionService solutionService, ObservableCollection<ProjectViewModel> projects) {

        _projectHierarchyData = new Dictionary<string, HierarchyData>();
        _solutionService      = solutionService;
        _projects             = projects;
        var subject = new Subject<EventArgs>();

        _updateRequestSource = subject;
        _updateRequestConnection = subject.AsObservable()
                                          .Throttle(TimeSpan.FromMilliseconds(100))
                                          .ObserveOn(SynchronizationContext.Current)
                                          .Subscribe(_ => {
                                               ThreadHelper.ThrowIfNotOnUIThread();
                                               UpdateState();
                                           });

        _hierarchyEvents = new HierarchyEvents(this);

        WireEvents();
    }

    public event EventHandler<EventArgs> ProjectStateChanged;

    public HResult OpenProject(ProjectViewModel viewModel) {
        ThreadHelper.ThrowIfNotOnUIThread();
        return _solutionService.OpenProject(viewModel.Path);
    }

    public HResult CloseProject(ProjectViewModel viewModel) {
        ThreadHelper.ThrowIfNotOnUIThread();
        var hierarchy = HierarchyFromViewModel(viewModel);
        return hierarchy?.CloseProject() ?? HResults.Failed;
    }

    public HResult ReloadProject(ProjectViewModel viewModel) {
        ThreadHelper.ThrowIfNotOnUIThread();
        var hierarchy = HierarchyFromViewModel(viewModel);
        return hierarchy?.ReloadProject() ?? HResults.Failed;
    }

    public HResult UnloadProject(ProjectViewModel viewModel) {
        ThreadHelper.ThrowIfNotOnUIThread();
        var hierarchy = HierarchyFromViewModel(viewModel);
        return hierarchy?.UnloadProject() ?? HResults.Failed;
    }

    public void Dispose() {
        UnwireEvents();
        _updateRequestSource.OnCompleted();
        _updateRequestConnection.Dispose();
    }

    public HResult EnsureSolution() {
        ThreadHelper.ThrowIfNotOnUIThread();
        return _solutionService.EnsureSolution();
    }

    public bool IsSolutionOpen {
        get {
            ThreadHelper.ThrowIfNotOnUIThread();
            return _solutionService.IsSolutionOpen();
        }
    }

    [CanBeNull]
    Hierarchy HierarchyFromViewModel(ProjectViewModel model) {
        ThreadHelper.ThrowIfNotOnUIThread();

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
        _updateRequestSource.OnNext(EventArgs.Empty);
    }

    void UpdateState() {

        ThreadHelper.ThrowIfNotOnUIThread();

        _updateStateRequested = false;

        RebuildHierarchyData();

        var stateChanged = false;

        foreach (var project in _projects) {
            var hierarchy = HierarchyFromViewModel(project);
            stateChanged |= SyncProjectState(hierarchy, project);
        }

        if (stateChanged) {
            ProjectStateChanged?.Invoke(this, EventArgs.Empty);
        }

    }

    static bool SyncProjectState(Hierarchy hierarchy, ProjectViewModel project) {
            
        ThreadHelper.ThrowIfNotOnUIThread();

        var status       = ProjectStatus.Closed;
        var imageMoniker = (ImageMoniker?) null;
        var caption      = "";

        if (hierarchy != null) {
            status       = hierarchy.IsProjectUnloaded() ? ProjectStatus.Unloaded : ProjectStatus.Loaded;
            imageMoniker = hierarchy.GetImageMoniker();
            caption      = hierarchy.Caption;

        }

        return project.SetState(status, imageMoniker, caption);

    }

    void RebuildHierarchyData() {

        ThreadHelper.ThrowIfNotOnUIThread();

        foreach (var hd in _projectHierarchyData.Values) {
            hd.UnadviseHierarchyEvents();
        }

        _projectHierarchyData.Clear();

        foreach (var hierarchy in _solutionService.EnumerateHierarchy()) {

            var fullPath = hierarchy.FullPath;
            if (fullPath != null) {
                var hd = new HierarchyData(hierarchy);
                _projectHierarchyData[fullPath.ToLower()] = hd;

                hd.AdviseHierarchyEvents(_hierarchyEvents);
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
        ThreadHelper.ThrowIfNotOnUIThread();
        Logger.Info($"{nameof(OnBeforeRemoveProject)}: {e.RealHierarchie.FullPath}");
        InvalidateState();

    }

    void OnAfterOpenProject(object sender, ProjectEventArgs e) {
        ThreadHelper.ThrowIfNotOnUIThread();
        Logger.Info($"{nameof(OnAfterOpenProject)}: {e.RealHierarchie.FullPath}");
        InvalidateState();
    }

    void OnBeforeUnloadProject(object sender, ProjectEventArgs e) {
        ThreadHelper.ThrowIfNotOnUIThread();
        Logger.Info($"{nameof(OnBeforeUnloadProject)}: {e.StubHierarchie?.FullPath}");
        InvalidateState();
    }

    void OnAfterLoadProject(object sender, ProjectEventArgs e) {
        ThreadHelper.ThrowIfNotOnUIThread();
        Logger.Info($"{nameof(OnAfterLoadProject)}: {e.RealHierarchie?.FullPath}");
        InvalidateState();
    }

    void OnProjectsChanged(object sender, NotifyCollectionChangedEventArgs e) {
        InvalidateState();
    }

    sealed class HierarchyEvents: IVsHierarchyEvents {

        private readonly ProjectService _projectService;

        public HierarchyEvents(ProjectService projectService) {
            _projectService = projectService;
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
                _projectService.InvalidateState();
            }

            return VSConstants.S_OK;
        }

        int IVsHierarchyEvents.OnInvalidateItems(uint itemidParent) {
            return VSConstants.S_OK;
        }

        int IVsHierarchyEvents.OnInvalidateIcon(IntPtr hicon) {
            _projectService.InvalidateState();
            return VSConstants.S_OK;
        }

    }

    #endregion

}