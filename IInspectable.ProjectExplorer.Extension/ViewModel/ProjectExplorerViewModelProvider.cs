#region Using Directives

using System.Collections.ObjectModel;

using Microsoft.VisualStudio.Shell;

using System.ComponentModel.Composition;

using IInspectable.ProjectExplorer.Extension.UI;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    [Export]
    class ProjectExplorerViewModelProvider {

        #pragma warning disable 0649
        [Import]
        readonly SolutionService _solutionService;

        [Import]
        readonly OptionService _optionService;

        [Import]
        readonly IWaitIndicator _waitIndicator;

        [Import]
        readonly TextBlockBuilderService _textBlockBuilderService;

        [Import]
        private readonly SearchContextFactory _searchContextFactory;

        #pragma warning restore

        public ProjectExplorerViewModel CreateViewModel(
            ProjectExplorerPackage package,
            IErrorInfoService errorInfoService,
            OleMenuCommandService oleMenuCommandService) {

            var projects           = new ObservableCollection<ProjectViewModel>();
            var selectionService   = new ProjectViewModelSelectionService(projects);
            var projectService     = new ProjectService(_solutionService, projects);
            var projectFileService = new ProjectFileService(package);

            return new(
                projects               : projects,
                selectionService       : selectionService,
                projectService         : projectService,
                projectFileService     : projectFileService,
                errorInfoService       : errorInfoService,
                optionService          : _optionService,
                oleMenuCommandService  : oleMenuCommandService,
                waitIndicator          : _waitIndicator,
                textBlockBuilderService: _textBlockBuilderService,
                searchContextFactory   : _searchContextFactory);
        }

    }

}