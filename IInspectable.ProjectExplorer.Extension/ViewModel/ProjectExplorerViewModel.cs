#region Using Directives

using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Security;
using System.Threading;
using System.Windows.Data;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Design;
using System.Collections.ObjectModel;

using JetBrains.Annotations;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    class ProjectExplorerViewModel: ViewModelBase {

        static readonly Logger Logger = Logger.Create<ProjectExplorerViewModel>();

        readonly IErrorInfoService _errorInfoService;
        readonly SolutionService _solutionService;
        readonly OptionService  _optionService;
        readonly OleMenuCommandService _oleMenuCommandService;
        readonly IWaitIndicator _waitIndicator;
        readonly ObservableCollection<ProjectItemViewModel> _projects;
        readonly ListCollectionView _projectsView;
        readonly List<Command> _commands;
        readonly ProjectViewModelSelectionService _selectionService;

        readonly ImmutableList<StatusSectionViewModel> _statusSections;

        [CanBeNull]
        CancellationTokenSource _loadingCancellationToken;
        bool _suspendReload;

        bool _isLoading;
        SearchContext _searchContext;

        internal ProjectExplorerViewModel(IErrorInfoService errorInfoService, 
                                          SolutionService solutionService, 
                                          OptionService optionService, 
                                          OleMenuCommandService oleMenuCommandService, 
                                          IWaitIndicator waitIndicator) {
            _errorInfoService      = errorInfoService;
            _solutionService       = solutionService;
            _optionService         = optionService;
            _oleMenuCommandService = oleMenuCommandService;
            _waitIndicator         = waitIndicator;
            _selectionService      = new ProjectViewModelSelectionService();
            
            _commands = new List<Command> {
                { RefreshCommand            = new RefreshCommand(this)},
                { CancelRefreshCommand      = new CancelRefreshCommand(this)},
                { OpenInFileExplorerCommand = new OpenInFileExplorerCommand(this)},
                { AddProjectCommand         = new AddProjectCommand(this)},
                { RemoveProjectCommand      = new RemoveProjectCommand(this)},
                { UnloadProjectCommand      = new UnloadProjectCommand(this)},
                { LoadProjectCommand        = new LoadProjectCommand(this)},
                { SettingsCommand           = new SettingsCommand(this)},
            };
            
            _statusSections = new List<StatusSectionViewModel> {
                new StatusSectionViewModel(ProjectStatus.Loaded),
                new StatusSectionViewModel(ProjectStatus.Unloaded),
                new StatusSectionViewModel(ProjectStatus.Closed),
            }.ToImmutableList();

            _projects      = new ObservableCollection<ProjectItemViewModel>();
            _projectsView  = (ListCollectionView)CollectionViewSource.GetDefaultView(_projects);
            _projectsView.CustomSort = new ProjectItemComparer();

            WireEvents();
            RegisterCommands();
            UpdateCommands();

            if (IsSolutionOpen) {
                RefreshCommand.Execute();
            }
        }

        void WireEvents() {
            _solutionService.AfterOpenSolution   += OnAfterOpenSolution;
            _solutionService.AfterCloseSolution  += OnAfterCloseSolution;
            _solutionService.AfterLoadProject    += OnAfterLoadProject;
            _solutionService.BeforeUnloadProject += OnBeforeUnloadProject;
            _solutionService.AfterOpenProject    += OnAfterOpenProject;
            _solutionService.BeforeRemoveProject += OnBeforeRemoveProject;
            _selectionService.SelectionChanged   += OnSelectionChanged;
        }
        
        void UnwireEvents() {
            _solutionService.AfterOpenSolution   -= OnAfterOpenSolution;
            _solutionService.AfterCloseSolution  -= OnAfterCloseSolution;
            _solutionService.AfterLoadProject    -= OnAfterLoadProject;
            _solutionService.BeforeUnloadProject -= OnBeforeUnloadProject;
            _solutionService.AfterOpenProject    -= OnAfterOpenProject;
            _solutionService.BeforeRemoveProject -= OnBeforeRemoveProject;
            _selectionService.SelectionChanged   -= OnSelectionChanged;
        }

        public void Dispose() {
            UnwireEvents();
            UnregisterCommands();
            ClearProjects();
        }
        
        public RefreshCommand RefreshCommand { get; }
        public CancelRefreshCommand CancelRefreshCommand { get; }
        public OpenInFileExplorerCommand OpenInFileExplorerCommand { get; }
        public AddProjectCommand AddProjectCommand { get; }
        public RemoveProjectCommand RemoveProjectCommand { get; }
        public UnloadProjectCommand UnloadProjectCommand { get; }
        public LoadProjectCommand LoadProjectCommand { get; }
        public SettingsCommand SettingsCommand { get; }

        [NotNull]
        internal SolutionService SolutionService {
            get { return _solutionService; }
        }

        [NotNull]
        public ProjectViewModelSelectionService SelectionService {
            get { return _selectionService; }
        }

        [NotNull]
        public IWaitIndicator WaitIndicator {
            get { return _waitIndicator; }
        }

        [NotNull]
        public ObservableCollection<ProjectItemViewModel> Projects {
            get { return _projects; }
        }

        [NotNull]
        public ListCollectionView ProjectsView {
            get { return _projectsView; }
        }
        
        public string ProjectsRoot {
            get { return _optionService.ProjectsRoot; }
        }

        public string ProjectsRootLabel {
            get {
                if(String.IsNullOrEmpty(_optionService.ProjectsRoot)) {
                    return "Select Folder...";
                }
                return _optionService.ProjectsRoot;
            }
        }

        public string StatusText {
            get {

                int projectCount = Projects.Count(p => p.Visible);
                if (IsLoading || projectCount == 0) {
                    return String.Empty;
                }

                if (projectCount == 1) {
                    return "1 Project";
                }

                return $"{projectCount} Projects";
            }
        }

        public bool IsSolutionOpen {
            get { return _solutionService.IsSolutionOpen(); }
        }

        

        public bool IsLoading {
            get { return _isLoading; }
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
            get {
                if(_searchContext == null) {
                    _searchContext = new SearchContext();
                }
                return _searchContext;
            }
            private set { _searchContext = value; }
        }

        public IImmutableList<StatusSectionViewModel> StatusSections {
            get { return _statusSections; }
        }

        #region Event Handler

        protected override void OnPropertyChanged(PropertyChangedEventArgs e) {
            base.OnPropertyChanged(e);
            UpdateCommands();
        }

        void OnSelectionChanged(object sender, EventArgs e) {
            UpdateCommands();
        }

        void OnAfterOpenSolution(object sender, EventArgs e) {            
            Logger.Info($"{nameof(OnAfterOpenSolution)}: {_solutionService.GetSolutionFile()}");           
            RefreshCommand.Execute();
        }

        void OnAfterCloseSolution(object sender, EventArgs e) {
            Logger.Info($"{nameof(OnAfterCloseSolution)}");
            UnbindProjects();
        }
        
        void OnBeforeRemoveProject(object sender, ProjectEventArgs e) {
            Logger.Info($"{nameof(OnBeforeRemoveProject)}: {e.RealHierarchie.FullPath}");

            var fullPath   = e.RealHierarchie.FullPath;
            var uniqueName = e.RealHierarchie.GetUniqueNameOfProject();
            // Wir können an dieser Stelle nicht unterscheiden, ob das Projekt nur entladen
            // oder entfernt wurde => Wir verzögern das Update. Wenn das Projekt entfernt wurde
            // wird es auch keine Hierarchie mehr geben...
            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() => {

                    var projectVm = FindProjectViewModel(fullPath);
                    var hier      = SolutionService.GetHierarchyByUniqueNameOfProject(uniqueName);

                    projectVm?.BindToHierarchy(hier);

                    UpdateCommands();
                }));
        }

        void OnAfterOpenProject(object sender, ProjectEventArgs e) {
            Logger.Info($"{nameof(OnAfterOpenProject)}: {e.RealHierarchie.FullPath}");

            var projectVm = FindProjectViewModel(e.RealHierarchie);

            projectVm?.BindToHierarchy(e.RealHierarchie);

            UpdateCommands();
        }

        void OnBeforeUnloadProject(object sender, ProjectEventArgs e) {
            Logger.Info($"{nameof(OnBeforeUnloadProject)}: {e.StubHierarchie?.FullPath}");

            var projectVm = FindProjectViewModel(e.StubHierarchie);

            projectVm?.BindToHierarchy(e.StubHierarchie);

            UpdateCommands();
        }

        void OnAfterLoadProject(object sender, ProjectEventArgs e) {
            Logger.Info($"{nameof(OnAfterLoadProject)}: {e.RealHierarchie?.FullPath}");

            var projectVm= FindProjectViewModel(e.StubHierarchie);

            projectVm?.BindToHierarchy(e.RealHierarchie);

            UpdateCommands();
        }

        public void OnHierarchyStatusChanged() {
            if(IsLoading) {
                return;
            }
            GroupAndSort();
        }

        #endregion

        public void ExecuteDefaultAction() {

            var selectedProject = SelectionService.SelectedItems.LastOrDefault();
            if(selectedProject == null) {
                return;
            }

            Command command = null;
            switch (selectedProject.Status) {
                case ProjectStatus.Closed:
                    command = AddProjectCommand;
                    break;
                case ProjectStatus.Unloaded:
                    command = LoadProjectCommand;
                    break;
                case ProjectStatus.Loaded:
                    command = UnloadProjectCommand;
                    break;
            }

            if(command?.CanExecute()==false) {
                return;
            }

            command?.Execute();
        }

        public int EnsureSolution() {

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

            SearchContext = new SearchContext(searchString);

            ApplySearch();
        }

        void ApplySearch() {
            
            foreach(var section in StatusSections) {
                section.Filter(SearchContext);
            }

            NotifyThisPropertyChanged(nameof(StatusText));
        }

        public void ClearSearch() {
            ApplySearch(null);
            NotifyThisPropertyChanged(nameof(StatusText));
        }

        public void UnbindProjects() {
            foreach (var project in Projects) {
                project.BindToHierarchy(null);
            }
            UpdateCommands();
        }
        
        public void ClearProjects(bool clearSearch=true) {

            CancelReloadProjects();
            
            foreach(var project in Projects) {
                project.Dispose();
            }

            Projects.Clear();

            if (clearSearch) {                
                ClearSearch();
            }

            _errorInfoService.RemoveErrorInfoBar();

            UpdateCommands();
            NotifyAllPropertiesChanged();
            ShellUtil.UpdateCommandUI();
        }
        
        public void CancelReloadProjects() {
            _loadingCancellationToken?.Cancel();
        }

        public async System.Threading.Tasks.Task ReloadProjects() {

            if (IsLoading || _suspendReload) {
                return;
            }

            IsLoading = true;
            try {

                ClearProjects(clearSearch: false);

                _loadingCancellationToken = new CancellationTokenSource();
                var projectFiles = await _solutionService.GetProjectFilesAsync(ProjectsRoot, _loadingCancellationToken.Token);

                var projectViewModels = _solutionService.BindToHierarchy(projectFiles);

                foreach (var viewModel in projectViewModels) {
                    viewModel.SetParent(this);
                }

                foreach (var viewModel in projectViewModels) {
                    Projects.Add(viewModel);
                }

                GroupAndSort();

            } catch (Exception ex) when (
                    ex is DirectoryNotFoundException || 
                    ex is IOException ||
                    ex is UnauthorizedAccessException ||
                    ex is SecurityException) {

                Logger.Error(ex, $"{nameof(ReloadProjects)}");
                _errorInfoService.ShowErrorInfoBar(ex);
            }
            catch (OperationCanceledException) {
                // ist OK
            } finally {

                _loadingCancellationToken?.Dispose();
                _loadingCancellationToken = null;

                IsLoading = false;

                UpdateCommands();
                NotifyAllPropertiesChanged();
                ShellUtil.UpdateCommandUI();
            }
        }

        void GroupAndSort() {
            foreach(var section in StatusSections) {
                section.Populate(Projects, SearchContext);
            }
        }
        
        public async Task<bool> SetProjectsRoot(string path) {

            if(IsLoading) {
                return false;
            }

            _optionService.ProjectsRoot = path;

            await ReloadProjects();

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
            foreach (var command in _commands) {
                command.UpdateState();
            }
        }

        [CanBeNull]
        ProjectItemViewModel FindProjectViewModel(Hierarchy hierarchy) {

            string fullPath = hierarchy.FullPath;
            var viewModel = FindProjectViewModel(fullPath);

            if(viewModel != null || IsLoading) {
                return viewModel;
            }

            if (fullPath == null) {
                return null;
            }

            viewModel = _solutionService.LoadAndBind(fullPath, hierarchy);
            if(viewModel == null) {
                return null;
            }

            viewModel.SetParent(this);
            Projects.Add(viewModel);

            return viewModel;
        }

        [CanBeNull]
        ProjectItemViewModel FindProjectViewModel(string path) {
            return _projects.FirstOrDefault(p => p.Path.ToLower() == path?.ToLower());
        }

        sealed class StateSaver<T> : IDisposable {

            readonly T _previousState;
            readonly Action<T> _setter;

            public StateSaver(T value, Func<T> getter, Action<T> setter) {
                _previousState = getter();
                _setter        = setter;
                _setter(value);
            }

            public void Dispose() {
                _setter(_previousState);
            }
        }

        static class Capture {

            public static IDisposable ProjectsRoot(ProjectExplorerViewModel model) {
                return new StateSaver<string>(
                    value : model._optionService.ProjectsRoot,
                    getter: () => model._optionService.ProjectsRoot,
                    setter: value => model._optionService.ProjectsRoot = value);
            }
        }

        static class Suspend {

            public static IDisposable Reload(ProjectExplorerViewModel model) {
                return new StateSaver<bool>(
                    value : true,
                    getter: () => model._suspendReload,
                    setter: value => model._suspendReload = value);
            }
        }       
    }
}