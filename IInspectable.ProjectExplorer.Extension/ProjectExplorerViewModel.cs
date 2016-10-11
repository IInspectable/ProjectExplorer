#region Using Directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;
using JetBrains.Annotations;
using Microsoft.VisualStudio.Shell;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    class ProjectExplorerViewModel: ViewModelBase {

        readonly SolutionService _solutionService;
        readonly OptionService  _optionService;
        readonly OleMenuCommandService _menuCommandService;
        readonly ObservableCollection<ProjectViewModel> _projects;
        readonly ListCollectionView _projectsView;
        readonly List<Command> _commands;

        internal ProjectExplorerViewModel(SolutionService solutionService, OptionService optionService, OleMenuCommandService menuCommandService) {

            _solutionService    = solutionService;
            _optionService      = optionService;
            _menuCommandService = menuCommandService;

            _solutionService.AfterOpenSolution += OnAfterOpenSolution;
            _solutionService.AfterCloseSolution += OnAfterCloseSolution;
            _solutionService.AfterLoadProject    += OnAfterLoadProject;
            _solutionService.BeforeUnloadProject += OnBeforeUnloadProject;
            _solutionService.AfterOpenProject    += OnAfterOpenProject;
            _solutionService.BeforeRemoveProject += OnBeforeRemoveProject;

            _commands = new List<Command> {
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
        
        void RegisterCommands() {
            foreach(var command in _commands) {
                command.Register(_menuCommandService);
            }
        }

        public void UpdateCommands() {
            foreach (var command in _commands) {
                command.UpdateState();
            }
        }

        // TODO Selection Logic
        ProjectViewModel _selectedProject;

        public ProjectViewModel SelectedProject {
            get { return _selectedProject; }
            set {
                if(_selectedProject == value) {
                    return;
                }
                _selectedProject = value;
                NotifyPropertyChanged();
            }
        }

        public RefreshCommand RefreshCommand { get; }
        public OpenInFileExplorerCommand OpenInFileExplorerCommand { get; }
        public AddProjectCommand AddProjectCommand { get; }
        public RemoveProjectCommand RemoveProjectCommand { get; }
        public UnloadProjectCommand UnloadProjectCommand { get; }
        public LoadProjectCommand LoadProjectCommand { get; }
        public SettingsCommand SettingsCommand { get; }

        void OnAfterOpenSolution(object sender, EventArgs e) {
            RefreshCommand.Execute();
        }

        void OnAfterCloseSolution(object sender, EventArgs e) {
            ClearProjects();
        }
        
        void OnBeforeRemoveProject(object sender, ProjectEventArgs e) {

            var guid= e.RealHierarchie.GetProjectGuid();

            // Wir können an dieser Stelle nicht unterscheiden, ob das Projekt nur entladen
            // oder entfernt wurde => Wir verzögern das Update. Wenn das Projekt entfernt wurde
            // wird es auch keine Hierarchie mehr geben...
            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() => {
                    var hier=_solutionService.GetHierarchyByProjectGuid(guid);
                    var projectVm = FindProjectViewModel(guid);
                    projectVm?.Bind(hier);                    
                }));
        }

        void OnAfterOpenProject(object sender, ProjectEventArgs e) {

            var projectVm = FindProjectViewModel(e.RealHierarchie);

            projectVm?.Bind(e.RealHierarchie);
        }

        void OnBeforeUnloadProject(object sender, ProjectEventArgs e) {

            var projectVm = FindProjectViewModel(e.StubHierarchie);

            projectVm?.Bind(e.StubHierarchie);
        }

        void OnAfterLoadProject(object sender, ProjectEventArgs e) {

            var projectVm= FindProjectViewModel(e.RealHierarchie);

            projectVm?.Bind(e.RealHierarchie);
        }

        [CanBeNull]
        ProjectViewModel FindProjectViewModel(Hierarchy hierarchy) {
            var projectGuid = hierarchy.GetProjectGuid();
            return FindProjectViewModel(projectGuid);
        }

        [CanBeNull]
        ProjectViewModel FindProjectViewModel(Guid projectGuid) {
            // TODO Performance Optimierung
            return _projects.FirstOrDefault(p => p.ProjectGuid == projectGuid);
        }

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

        private static string WildcardToRegex(string searchString) {

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

        public void ClearSearch() {
            ProjectsView.Filter = null;

            NotifyThisPropertyChanged(nameof(StatusText));
        }

        public bool IsSolutionLoaded {
            get { return _solutionService.IsSolutionLoaded(); }
        }

        public void ClearProjects(bool clearSearch=true) {
            Projects.Clear();
            if (clearSearch) {                
                ClearSearch();
            }
            SelectedProject = null;
            NotifyAllPropertiesChanged();
        }

        private bool _isLoading;
        public bool IsLoading {
            get { return _isLoading; }
            set {
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

        public async System.Threading.Tasks.Task ReloadProjects() {

            if (!IsSolutionLoaded || IsLoading) {
                return;
            }

            IsLoading = true;
            try {

                ClearProjects(clearSearch:false);

                // TODO Error Handling
                var projectFiles = await _solutionService.GetProjectFilesAsync(ProjectsRoot);

                var projects = _solutionService.BindToHierarchy(projectFiles);

                foreach (var projectVm in projects) {
                    projectVm.SetParent(this);
                }

                var oldProjects = Projects.ToList();

                Projects.Clear();
                foreach (var project in projects) {
                    Projects.Add(project);
                }
                SelectedProject = null;

                foreach (var projectVm in oldProjects) {
                    projectVm.Dispose();
                }

            } finally {
                IsLoading = false;
            }

            NotifyAllPropertiesChanged();
        }

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
    }

    sealed class ProjectItemComparer: IComparer<ProjectViewModel>, IComparer {

        public int Compare(ProjectViewModel x, ProjectViewModel y) {

            if (x == null && y != null) {
                return -1;
            }

            if (x != null && y == null) {
                return 1;
            }
            if (x == null) {
                return 0;
            }

            var statusCmp= y.Status - x.Status;
            if (statusCmp != 0) {
                return statusCmp;
            }

            return String.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
        }

        public int Compare(object x, object y) {
            return Compare(x as ProjectViewModel, y as ProjectViewModel);
        }

    }
}