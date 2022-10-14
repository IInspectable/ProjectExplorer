#region Using Directives

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

#endregion

namespace IInspectable.ProjectExplorer.Extension; 

sealed class ProjectSearchTask: VsSearchTask {

    readonly ProjectExplorerViewModel _viewModel;

    public ProjectSearchTask(ProjectExplorerViewModel viewModel, uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback)
        : base(dwCookie, pSearchQuery, pSearchCallback) {

        _viewModel = viewModel;
    }

    protected override void OnStartSearch() {

        ThreadHelper.JoinableTaskFactory.RunAsync(async () => {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _viewModel.ApplySearch(SearchQuery.SearchString);

        });

        base.OnStartSearch();
    }

}