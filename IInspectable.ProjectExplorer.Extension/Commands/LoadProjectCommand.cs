using System;

namespace IInspectable.ProjectExplorer.Extension {

    sealed class LoadProjectCommand : Command {

        readonly ProjectExplorerViewModel _viewModel;

        public LoadProjectCommand(ProjectExplorerViewModel viewModel)
            : base(PackageIds.LoadProjectCommandId) {

            if (viewModel == null) {
                throw new ArgumentNullException(nameof(viewModel));
            }

            _viewModel = viewModel;
        }

        public override void UpdateState() {
            Enabled = _viewModel.SelectedProject?.Status == ProjectStatus.Unloaded;
            Visible = Enabled;
        }

        public override void Execute(object parameter = null) {
            ShellUtil.ReportUserOnFailed(_viewModel.SelectedProject?.Reload());
        }
    }
}