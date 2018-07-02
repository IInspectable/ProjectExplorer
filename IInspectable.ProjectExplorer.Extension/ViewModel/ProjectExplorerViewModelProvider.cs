#region Using Directives

using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Composition;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    [Export]
    class ProjectExplorerViewModelProvider {
        
        #pragma warning disable 0649
        [Import] readonly SolutionService _solutionService;
        [Import] readonly OptionService _optionService;
        [Import] readonly IWaitIndicator _waitIndicator;
        #pragma warning restore

        public ProjectExplorerViewModel CreateViewModel(IErrorInfoService errorInfoService, OleMenuCommandService oleMenuCommandService) {

            return new ProjectExplorerViewModel(errorInfoService, _solutionService, _optionService, oleMenuCommandService, _waitIndicator);
        }
    }
}