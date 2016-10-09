using System;

namespace IInspectable.ProjectExplorer.Extension {

    sealed class UnloadProjectCommand : Command {

        readonly ProjectExplorerViewModel _viewModel;

        public UnloadProjectCommand(ProjectExplorerViewModel viewModel)
            : base(PackageIds.UnloadProjectCommandId) {

            if (viewModel == null) {
                throw new ArgumentNullException(nameof(viewModel));
            }

            _viewModel = viewModel;
        }

        public override void UpdateState() {
            Enabled = _viewModel.SelectedProject?.Status == ProjectStatus.Loaded;
        }

        public override void Execute(object parameter) {
            _viewModel.SelectedProject?.Unload();
        }
    }
}