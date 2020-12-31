#region Using Directives

using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Windows.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Collections.ObjectModel;

using JetBrains.Annotations;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;

using System.ComponentModel;

using IInspectable.ProjectExplorer.Extension.UI;

using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;
// ReSharper disable ConvertToAutoProperty

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    class ProjectExplorerViewModel: ViewModelBase {

        static readonly Logger Logger = Logger.Create<ProjectExplorerViewModel>();

        readonly IErrorInfoService                      _errorInfoService;
        readonly SolutionService                        _solutionService;
        readonly OptionService                          _optionService;
        readonly OleMenuCommandService                  _oleMenuCommandService;
        readonly IWaitIndicator                         _waitIndicator;
        readonly TextBlockBuilderService                _textBlockBuilderService;
        readonly ObservableCollection<ProjectViewModel> _projects;
        readonly ListCollectionView                     _projectsView;
        readonly List<Command>                          _commands;
        readonly ProjectViewModelSelectionService       _selectionService;
        readonly ProjectService                         _projectService;
        readonly ProjectFileService                     _projectFileService;
        readonly SearchContextFactory                   _searchContextFactory;

        [CanBeNull]
        CancellationTokenSource _loadingCancellationToken;

        bool _suspendReload;

        bool          _isLoading;
        SearchContext _searchContext;

        internal ProjectExplorerViewModel(ProjectExplorerPackage package, 
                                          IErrorInfoService errorInfoService,
                                          SolutionService solutionService,
                                          OptionService optionService,
                                          OleMenuCommandService oleMenuCommandService,
                                          IWaitIndicator waitIndicator,
                                          TextBlockBuilderService textBlockBuilderService,
                                          SearchContextFactory searchContextFactory) {

            ThreadHelper.ThrowIfNotOnUIThread();

            _errorInfoService        = errorInfoService;
            _solutionService         = solutionService;
            _optionService           = optionService;
            _oleMenuCommandService   = oleMenuCommandService;
            _waitIndicator           = waitIndicator;
            _textBlockBuilderService = textBlockBuilderService;
            _searchContextFactory    = searchContextFactory;

            _commands = new List<Command> {
                {RefreshCommand            = new RefreshCommand(this)},
                {CancelRefreshCommand      = new CancelRefreshCommand(this)},
                {OpenInFileExplorerCommand = new OpenInFileExplorerCommand(this)},
                {AddProjectCommand         = new AddProjectCommand(this)},
                {RemoveProjectCommand      = new RemoveProjectCommand(this)},
                {UnloadProjectCommand      = new UnloadProjectCommand(this)},
                {LoadProjectCommand        = new LoadProjectCommand(this)},
                {SettingsCommand           = new SettingsCommand(this)},
                {ExceuteDefaultCommand     = new ExceuteDefaultCommand(this)},
            };

            // View
            _projects     = new ObservableCollection<ProjectViewModel>();
            _projectsView = (ListCollectionView) CollectionViewSource.GetDefaultView(_projects);
            // Sortierung
            _projectsView.CustomSort    = new ProjectViewModelComparer();
            _projectsView.IsLiveSorting = true;
            _projectsView.LiveSortingProperties.Add(nameof(ProjectViewModel.Status));
            _projectsView.LiveSortingProperties.Add(nameof(ProjectViewModel.PatternMatch));
            // Filter
            _projectsView.IsLiveFiltering = true;
            _projectsView.LiveFilteringProperties.Add(nameof(ProjectViewModel.Visible));
            _projectsView.Filter = vm => (vm as ProjectViewModel)?.Visible == true;

            _selectionService   = new ProjectViewModelSelectionService(_projects);
            _projectService     = new ProjectService(_solutionService, _projects);
            _projectFileService = new ProjectFileService(package);

            WireEvents();
            RegisterCommands();
            UpdateCommands();

            if (IsSolutionOpen) {
                RefreshCommand.Execute();
            }
        }

        void WireEvents() {
            _solutionService.AfterOpenSolution  += OnAfterOpenSolution;
            _selectionService.SelectionChanged  += OnSelectionChanged;
            _projectService.ProjectStateChanged += OnProjectStateChanged;

        }

        void UnwireEvents() {
            _solutionService.AfterOpenSolution  -= OnAfterOpenSolution;
            _selectionService.SelectionChanged  -= OnSelectionChanged;
            _projectService.ProjectStateChanged -= OnProjectStateChanged;

        }

        public void Dispose() {
            UnwireEvents();
            UnregisterCommands();
            ClearProjects();
            _projectService.Dispose();
        }

        public RefreshCommand            RefreshCommand            { get; }
        public CancelRefreshCommand      CancelRefreshCommand      { get; }
        public OpenInFileExplorerCommand OpenInFileExplorerCommand { get; }
        public AddProjectCommand         AddProjectCommand         { get; }
        public RemoveProjectCommand      RemoveProjectCommand      { get; }
        public UnloadProjectCommand      UnloadProjectCommand      { get; }
        public LoadProjectCommand        LoadProjectCommand        { get; }
        public SettingsCommand           SettingsCommand           { get; }
        public ExceuteDefaultCommand     ExceuteDefaultCommand     { get; }

        [NotNull]
        internal SolutionService SolutionService => _solutionService;

        [NotNull]
        internal TextBlockBuilderService TextBlockBuilderService => _textBlockBuilderService;

        [NotNull]
        public ProjectViewModelSelectionService SelectionService => _selectionService;

        [NotNull]
        public IWaitIndicator WaitIndicator => _waitIndicator;

        [NotNull]
        public ObservableCollection<ProjectViewModel> Projects => _projects;

        [NotNull]
        public ListCollectionView ProjectsView => _projectsView;

        public string ProjectsRoot {
            get {
                ThreadHelper.ThrowIfNotOnUIThread();
                return _optionService.ProjectsRoot;
            }
        }

        public string ProjectsRootLabel {
            get { 
                ThreadHelper.ThrowIfNotOnUIThread();
                return _optionService.ProjectsRoot.NullIfEmpty() ?? "Choose Search Folder…";
            }
        }

        public bool IsSolutionOpen {
            get {
                ThreadHelper.ThrowIfNotOnUIThread();
                return _solutionService.IsSolutionOpen();
            }
        }

        public string StatusText {
            get {

                if (IsLoading) {
                    return String.Empty;
                }

                var stats = GetProjectStats();
                if (stats.Total == 0) {
                    return String.Empty;
                }

                var totalProject = Pluralize("project", stats.Total);
                var statusText   = $"{stats.Total} {totalProject}";

                if (stats.Added != 0) {

                    var addedProject = Pluralize("project", stats.Added);
                    statusText += $" ({stats.Loaded} of {stats.Added} {addedProject} loaded)";
                }

                return statusText;

                static string Pluralize(string name, int count) {
                    if (count == 1) {
                        return name;
                    }

                    return $"{name}s";
                }
            }
        }

        public bool IsLoading {
            get => _isLoading;
            private set {
                if (value == _isLoading) {
                    return;
                }

                _isLoading = value;
                NotifyPropertyChanged();
            }
        }

        [CanBeNull]
        public SearchContext SearchContext {
            get => _searchContext ??= _searchContextFactory.Create(null);
            private set => _searchContext = value;
        }

        #region Event Handler

        protected override void OnPropertyChanged(PropertyChangedEventArgs e) {
            ThreadHelper.ThrowIfNotOnUIThread();
            base.OnPropertyChanged(e);
            UpdateCommands();
        }

        void OnSelectionChanged(object sender, EventArgs e) {
            ThreadHelper.ThrowIfNotOnUIThread();
            UpdateCommands();
        }

        void OnAfterOpenSolution(object sender, EventArgs e) {
            ThreadHelper.ThrowIfNotOnUIThread();
            Logger.Info($"{nameof(OnAfterOpenSolution)}: {_solutionService.GetSolutionFile()}");
            RefreshCommand.Execute();
        }

        void OnProjectStateChanged(object sender, EventArgs e) {
            ThreadHelper.ThrowIfNotOnUIThread();
            UpdateCommands();
            NotifyThisPropertyChanged(nameof(StatusText));
        }

        #endregion

        public ProjectService ProjectService => _projectService;

        public int EnsureSolution() {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Falls eine neue Solution erstellt wird, soll die jetzige ProjectsRoot übernommen werden
            using (Suspend.Reload(this))
            using (Capture.ProjectsRoot(this)) {

                var hr = _solutionService.EnsureSolution();
                if (ErrorHandler.Failed(hr)) {
                    return hr;
                }

                return hr;
            }
        }

        public void ApplySearch(string searchString) {

            SearchContext = _searchContextFactory.Create(searchString);

            ApplySearch();
        }

        void ApplySearch() {

            foreach (var p in Projects) {
                p.Filter(SearchContext);
            }

            NotifyThisPropertyChanged(nameof(StatusText));
        }

        public void ClearSearch() {
            ApplySearch(null);
            NotifyThisPropertyChanged(nameof(StatusText));
        }

        public void ClearProjects(bool clearSearch = true) {

            CancelReloadProjects();

            Projects.Clear();

            if (clearSearch) {
                ClearSearch();
            }

            _errorInfoService.RemoveErrorInfoBar();

            NotifyAllPropertiesChanged();
        }

        public void CancelReloadProjects() {
            _loadingCancellationToken?.Cancel();
        }

        public async System.Threading.Tasks.Task ReloadProjectsAsync() {
            

            if (IsLoading || _suspendReload) {
                return;
            }

            IsLoading = true;
            try {

                ClearProjects(clearSearch: false);

                _loadingCancellationToken = new CancellationTokenSource();

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var projectFiles = await _projectFileService.GetProjectFilesAsync(ProjectsRoot, _loadingCancellationToken.Token);

                foreach (var viewModel in projectFiles.Select(pf => new ProjectViewModel(pf))) {
                    viewModel.SetParent(this);
                    Projects.Add(viewModel);
                }

                ApplySearch();

            } catch (Exception ex) when (
                ex is DirectoryNotFoundException  ||
                ex is IOException                 ||
                ex is UnauthorizedAccessException ||
                ex is SecurityException) {

                Logger.Error(ex, $"{nameof(ReloadProjectsAsync)}");
                _errorInfoService.ShowErrorInfoBar(ex);
            } catch (OperationCanceledException) {
                // ist OK
            } finally {

                _loadingCancellationToken?.Dispose();
                _loadingCancellationToken = null;

                IsLoading = false;

                NotifyAllPropertiesChanged();
            }
        }

        public async Task<bool> SetProjectsRootAsync(string path) {

            if (IsLoading) {
                return false;
            }

            _optionService.ProjectsRoot = path;

            await ReloadProjectsAsync();

            return true;
        }

        public void ShowSettingsButtonContextMenu(int x, int y) {

            var commandId = new CommandID(PackageGuids.ProjectExplorerWindowPackageCmdSetGuid,
                                          PackageIds.SettingsButtonContextMenu);

            _oleMenuCommandService.ShowContextMenu(commandId, x, y);
        }

        public void ShowProjectItemContextMenu(int x, int y) {

            var commandId = new CommandID(PackageGuids.ProjectExplorerWindowPackageCmdSetGuid,
                                          PackageIds.ProjectItemContextMenu);

            _oleMenuCommandService.ShowContextMenu(commandId, x, y);
        }

        void RegisterCommands() {
            foreach (var command in _commands) {
                command.Register(_oleMenuCommandService);
            }
        }

        void UnregisterCommands() {
            foreach (var command in _commands) {
                command.Unregister(_oleMenuCommandService);
            }
        }

        void UpdateCommands() {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (var command in _commands) {
                command.UpdateState();
            }
            
            ShellUtil.UpdateCommandUI(immediateUpdate: false);
        }

        public event EventHandler<EventArgs> RequestBringSelectionIntoView;
       
        public void BringSelectionIntoView() {
            RequestBringSelectionIntoView?.Invoke(this, EventArgs.Empty);
        }

        ProjectStats GetProjectStats(bool onlyVisible = true) {

            var stats = new ProjectStats(Loaded: 0, Unloaded: 0, Closed: 0);
            
            foreach (var p in EnumerateProjects()) {
                stats = p.Status switch {
                    ProjectStatus.Loaded   => stats with {Loaded   = stats.Loaded   + 1},
                    ProjectStatus.Unloaded => stats with {Unloaded = stats.Unloaded + 1},
                    ProjectStatus.Closed   => stats with {Closed   = stats.Closed   + 1},
                    _ => stats
                };
            }

            return stats;

            IEnumerable<ProjectViewModel> EnumerateProjects() {
                return onlyVisible ? Projects.Where(p => p.Visible) : Projects;
            }
        }

        sealed record ProjectStats(int Loaded, int Unloaded, int Closed) {

            public int Total => Loaded + Unloaded + Closed;
            public int Added => Loaded + Unloaded;

        }

        static class Capture {

            public static IDisposable ProjectsRoot(ProjectExplorerViewModel model) {
                ThreadHelper.ThrowIfNotOnUIThread();
                return new StateSaver<string>(
                    value: model._optionService.ProjectsRoot,
                    getter: () => model._optionService.ProjectsRoot,
                    setter: value => model._optionService.ProjectsRoot = value);
            }

        }

        static class Suspend {

            public static IDisposable Reload(ProjectExplorerViewModel model) {
                return new StateSaver<bool>(
                    value: true,
                    getter: () => model._suspendReload,
                    setter: value => model._suspendReload = value);
            }

        }

    }

}