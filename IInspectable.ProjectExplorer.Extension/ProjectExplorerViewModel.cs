#region Using Directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.IO;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using JetBrains.Annotations;
using Microsoft.VisualStudio.Shell;
using IInspectable.Utilities.Logging;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    class ProjectExplorerViewModel: ViewModelBase {

        static readonly Logger Logger = Logger.Create<ProjectExplorerViewModel>();

        private readonly ProjectExplorerWindow _toolWindow;
        readonly SolutionService _solutionService;
        readonly OptionService  _optionService;
        readonly OleMenuCommandService _menuCommandService;
        readonly ObservableCollection<ProjectViewModel> _projects;
        readonly ListCollectionView _projectsView;
        readonly List<Command> _commands;

        internal ProjectExplorerViewModel(ProjectExplorerWindow toolWindow, SolutionService solutionService, OptionService optionService, OleMenuCommandService menuCommandService) {
            _toolWindow = toolWindow;
            _solutionService    = solutionService;
            _optionService      = optionService;
            _menuCommandService = menuCommandService;

            _solutionService.AfterOpenSolution   += OnAfterOpenSolution;
            _solutionService.AfterCloseSolution  += OnAfterCloseSolution;
            _solutionService.AfterLoadProject    += OnAfterLoadProject;
            _solutionService.BeforeUnloadProject += OnBeforeUnloadProject;
            _solutionService.AfterOpenProject    += OnAfterOpenProject;
            _solutionService.BeforeRemoveProject += OnBeforeRemoveProject;

            _commands = new List<Command> {
                // TODO CancelRefreshCommand
                { RefreshCommand            = new RefreshCommand(this)},
                { OpenInFileExplorerCommand = new OpenInFileExplorerCommand(this)},
                { AddProjectCommand         = new AddProjectCommand(this)},
                { RemoveProjectCommand      = new RemoveProjectCommand(this)},
                { UnloadProjectCommand      = new UnloadProjectCommand(this)},
                { LoadProjectCommand        = new LoadProjectCommand(this)},
                { SettingsCommand           = new SettingsCommand(this)},
            };
            
            RegisterCommands();

            PropertyChanged += (o, e) => UpdateCommands();

            SearchOptions = new ProjectSearchOptions();
            _projects     = new ObservableCollection<ProjectViewModel>();
            _projectsView = (ListCollectionView)CollectionViewSource.GetDefaultView(_projects);
            _projectsView.CustomSort = new ProjectItemComparer();

            UpdateCommands();

            if (IsSolutionLoaded) {
                RefreshCommand.Execute();
            }
        }
        
        public RefreshCommand RefreshCommand { get; }
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
        public ObservableCollection<ProjectViewModel> Projects {
            get { return _projects; }
        }

        [NotNull]
        public ListCollectionView ProjectsView {
            get { return _projectsView; }
        }

        [NotNull]
        public ProjectSearchOptions SearchOptions { get; }

        public string ProjectsRoot {
            get { return _optionService.ProjectsRoot; }
        }

        public bool IsSolutionLoaded {
            get { return _solutionService.IsSolutionLoaded(); }
        }

        // TODO Selection Logic
        ProjectViewModel _selectedProject;

        public ProjectViewModel SelectedProject {
            get { return _selectedProject; }
            set {
                if (_selectedProject == value) {
                    return;
                }
                _selectedProject = value;
                NotifyPropertyChanged();
            }
        }

        bool _isLoading;
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

        public string StatusText {
            get {

                if (IsLoading) {
                    return "Loading projects...";
                }

                if (ProjectsView.Count == 1) {
                    return "1 Project found";
                }

                return $"{ProjectsView.Count} Projects found";
            }
        }

        #region Event Handler

        void OnAfterOpenSolution(object sender, EventArgs e) {
            Logger.Info($"{nameof(OnAfterOpenSolution)}: {_solutionService.GetSolutionFile()}");
            RefreshCommand.Execute();
        }

        void OnAfterCloseSolution(object sender, EventArgs e) {
            Logger.Info($"{nameof(OnAfterCloseSolution)}");
            ClearProjects();
        }
        
        void OnBeforeRemoveProject(object sender, ProjectEventArgs e) {
            Logger.Info($"{nameof(OnBeforeRemoveProject)}: {e.RealHierarchie.GetUniqueNameOfProject()}");

            var uniqueName = e.RealHierarchie.GetUniqueNameOfProject();
            
            // Wir können an dieser Stelle nicht unterscheiden, ob das Projekt nur entladen
            // oder entfernt wurde => Wir verzögern das Update. Wenn das Projekt entfernt wurde
            // wird es auch keine Hierarchie mehr geben...
            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() => {

                    var projectVm = FindProjectViewModel(uniqueName);
                    var hier      =SolutionService.GetHierarchyByUniqueNameOfProject(uniqueName);

                    projectVm?.Bind(hier);

                    UpdateCommands();
                }));
        }

        void OnAfterOpenProject(object sender, ProjectEventArgs e) {
            Logger.Info($"{nameof(OnAfterOpenProject)}: {e.RealHierarchie.GetUniqueNameOfProject()}");

            var projectVm = FindProjectViewModel(e.RealHierarchie);

            projectVm?.Bind(e.RealHierarchie);

            UpdateCommands();
        }

        void OnBeforeUnloadProject(object sender, ProjectEventArgs e) {
            Logger.Info($"{nameof(OnBeforeUnloadProject)}: {e.StubHierarchie?.GetUniqueNameOfProject()}");

            var projectVm = FindProjectViewModel(e.StubHierarchie);

            projectVm?.Bind(e.StubHierarchie);

            UpdateCommands();
        }

        void OnAfterLoadProject(object sender, ProjectEventArgs e) {
            Logger.Info($"{nameof(OnAfterLoadProject)}: {e.RealHierarchie?.GetUniqueNameOfProject()}");

            var projectVm= FindProjectViewModel(e.RealHierarchie);

            projectVm?.Bind(e.RealHierarchie);

            UpdateCommands();
        }

        #endregion
        
        public void ApplySearch(string searchString) {

            Dictionary<ProjectStatus, bool> inlcudeStatus = new Dictionary<ProjectStatus, bool> {
                {ProjectStatus.Closed  , SearchOptions.ClosedProjects},
                {ProjectStatus.Unloaded, SearchOptions.UnloadedProjects},
                {ProjectStatus.Loaded  , SearchOptions.LoadedProjects},
            };

            Regex regex = null;
            if (!String.IsNullOrWhiteSpace(searchString)) {
                var regexString = WildcardToRegex(searchString);

                regex = new Regex(regexString, RegexOptions.IgnoreCase);
            }
           
            ProjectsView.Filter = item => {

                var projectVm = (ProjectViewModel) item;

                var status = projectVm.Status;
                if (!inlcudeStatus[status]) {
                    return false;
                }

                return regex == null || regex.IsMatch(projectVm.Name);
            };

            NotifyThisPropertyChanged(nameof(StatusText));
        }
       
        public void ClearSearch() {
            ProjectsView.Filter = null;

            NotifyThisPropertyChanged(nameof(StatusText));
        }
        
        public void ClearProjects(bool clearSearch=true) {
            
            _loadingCancellationToken?.Cancel();
            
            foreach(var project in Projects) {
                project.Dispose();
            }

            Projects.Clear();

            if (clearSearch) {                
                ClearSearch();
            }

            _toolWindow.RemoveErrorInfoBar();

            SelectedProject = null;

            UpdateCommands();
            NotifyAllPropertiesChanged();
        }

        [CanBeNull]
        private CancellationTokenSource _loadingCancellationToken;

        public async System.Threading.Tasks.Task ReloadProjects() {

            if (!IsSolutionLoaded || IsLoading) {
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

            } catch (Exception ex) when (
                    ex is DirectoryNotFoundException || 
                    ex is IOException ||
                    ex is UnauthorizedAccessException ||
                    ex is SecurityException) {

                Logger.Error(ex, $"{nameof(ReloadProjects)}");
                _toolWindow.ShowErrorInfoBar(ex);
            }
            catch (OperationCanceledException) {
                // ist OK
            } finally {

                _loadingCancellationToken?.Dispose();
                _loadingCancellationToken = null;

                IsLoading = false;

                UpdateCommands();
                NotifyAllPropertiesChanged();
            }
        }
        
        public async Task<bool> SetProjectsRoot(string path) {

            if(!IsSolutionLoaded || IsLoading) {
                return false;
            }

            _optionService.ProjectsRoot = path;

            await ReloadProjects();

            return true;
        }

        public void ShowSettingsButtonContextMenu(int x, int y) {
            
            var commandId = new CommandID(PackageGuids.ProjectExplorerWindowPackageCmdSetGuid, 
                                          PackageIds.SettingsButtonContextMenu);

            _menuCommandService.ShowContextMenu(commandId, x, y);
        }

        public void ShowProjectItemContextMenu(int x, int y) {

            var commandId = new CommandID(PackageGuids.ProjectExplorerWindowPackageCmdSetGuid,
                                          PackageIds.ProjectItemContextMenu);

            _menuCommandService.ShowContextMenu(commandId, x, y);
        }

        void RegisterCommands() {
            foreach (var command in _commands) {
                command.Register(_menuCommandService);
            }
        }

        void UpdateCommands() {
            foreach (var command in _commands) {
                command.UpdateState();
            }
        }

        [CanBeNull]
        ProjectViewModel FindProjectViewModel(Hierarchy hierarchy) {
            string uniqueNameOfProject = hierarchy.GetUniqueNameOfProject();
            return FindProjectViewModel(uniqueNameOfProject);
        }

        [CanBeNull]
        ProjectViewModel FindProjectViewModel(string uniqueNameOfProject) {
            return _projects.FirstOrDefault(p => p.UniqueNameOfProject == uniqueNameOfProject);
        }

        static string WildcardToRegex(string searchString) {

            if (!searchString.StartsWith("*")) {
                searchString = "*" + searchString;
            }
            if (!searchString.EndsWith("*")) {
                searchString += "*";
            }

            searchString = "^" + Regex.Escape(searchString)
                               .Replace("\\*", ".*")
                               .Replace("\\?", ".") +
                           "$";
            return searchString;

        }
    }
}