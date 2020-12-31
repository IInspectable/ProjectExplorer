#region Using Directives

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

        public ProjectExplorerViewModel CreateViewModel(ProjectExplorerPackage package, IErrorInfoService errorInfoService, OleMenuCommandService oleMenuCommandService) {
            return new(
                package                : package,
                errorInfoService       : errorInfoService,
                solutionService        : _solutionService,
                optionService          : _optionService,
                oleMenuCommandService  : oleMenuCommandService,
                waitIndicator          : _waitIndicator,
                textBlockBuilderService: _textBlockBuilderService,
                searchContextFactory   : _searchContextFactory);
        }

    }

}