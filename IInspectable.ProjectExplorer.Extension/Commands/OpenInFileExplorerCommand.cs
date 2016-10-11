using System;

namespace IInspectable.ProjectExplorer.Extension {

    sealed class OpenInFileExplorerCommand : Command {

        readonly ProjectExplorerViewModel _viewModel;

        public OpenInFileExplorerCommand(ProjectExplorerViewModel viewModel)
            : base(PackageIds.OpenInFileExplorerCommandId) {

            if (viewModel == null) {
                throw new ArgumentNullException(nameof(viewModel));
            }

            _viewModel = viewModel;
        }

        public override void UpdateState() {
            Enabled = _viewModel.IsSolutionLoaded && _viewModel?.SelectedProject != null;
        }

        public override void Execute(object parameter = null) {

            _viewModel?.SelectedProject?.OpenFolderInFileExplorer();
        }
    }

}