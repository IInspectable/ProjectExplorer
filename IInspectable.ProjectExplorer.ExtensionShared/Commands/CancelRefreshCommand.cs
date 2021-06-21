#region Using Directives

using System;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed class CancelRefreshCommand: Command {

        readonly ProjectExplorerViewModel _viewModel;

        public CancelRefreshCommand(ProjectExplorerViewModel viewModel)
            : base(PackageIds.CancelRefreshCommandId) {

            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public override void UpdateState() {
            Enabled = _viewModel.IsLoading;
            Visible = _viewModel.IsLoading;
        }

        public override void Execute(object parameter=null) {
            _viewModel.CancelReloadProjects();
        }
    }
}