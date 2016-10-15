#region Using Directives

using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Composition;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    [Export]
    class ProjectExplorerViewModelProvider {

        [Import] readonly SolutionService _solutionService;
        [Import] readonly OptionService _optionService;
        [Import] readonly IWaitIndicator _waitIndicator;

        public ProjectExplorerViewModel CreateViewModel(ProjectExplorerToolWindow toolWindow, OleMenuCommandService oleMenuCommandService) {

            return new ProjectExplorerViewModel(toolWindow, _solutionService, _optionService, oleMenuCommandService, _waitIndicator);
        }
    }
}