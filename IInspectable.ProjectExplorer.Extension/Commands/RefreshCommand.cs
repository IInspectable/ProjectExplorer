#region Using Directives

using System;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed class RefreshCommand: Command {


        readonly ProjectExplorerViewModel _viewModel;

        public RefreshCommand(ProjectExplorerViewModel viewModel)
            : base(PackageIds.RefreshCommandId) {

            if (viewModel == null) {
                throw new ArgumentNullException(nameof(viewModel));
            }

            _viewModel = viewModel;
        }

        public override void UpdateState() {
            Enabled = _viewModel.IsSolutionLoaded && !_viewModel.IsLoading;
        }

        public override async void Execute(object parameter) {
            await _viewModel.ReloadProjects();
        }
    }
}