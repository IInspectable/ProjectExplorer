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
            Enabled = !_viewModel.IsLoading && !String.IsNullOrEmpty(_viewModel.ProjectsRoot);
            Visible = !_viewModel.IsLoading;
        }

        public override async void Execute(object parameter=null) {
            await _viewModel.ReloadProjects();
        }
    }
}