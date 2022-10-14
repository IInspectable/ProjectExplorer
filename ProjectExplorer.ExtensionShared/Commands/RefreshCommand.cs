#region Using Directives

using System;

using Microsoft.VisualStudio.Shell;

#endregion

namespace IInspectable.ProjectExplorer.Extension; 

sealed class RefreshCommand: Command {

    readonly ProjectExplorerViewModel _viewModel;

    public RefreshCommand(ProjectExplorerViewModel viewModel)
        : base(PackageIds.RefreshCommandId) {

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }

    public override void UpdateState() {
        ThreadHelper.ThrowIfNotOnUIThread();

        Enabled = !_viewModel.IsLoading && !String.IsNullOrEmpty(_viewModel.ProjectsRoot);
        Visible = !_viewModel.IsLoading;
    }

    public override void Execute(object parameter = null) {

        ThreadHelper.JoinableTaskFactory.RunAsync(async () => { await _viewModel.ReloadProjectsAsync(); })
                    .FileAndForget("ProjectExplorer/RefreshCommand.Execute");
    }

}