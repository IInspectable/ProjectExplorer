using System;

namespace IInspectable.ProjectExplorer.Extension {

    sealed class RemoveProjectCommand : Command {

        readonly ProjectExplorerViewModel _viewModel;

        public RemoveProjectCommand(ProjectExplorerViewModel viewModel)
            : base(PackageIds.RemoveProjectCommandId) {

            if (viewModel == null) {
                throw new ArgumentNullException(nameof(viewModel));
            }

            _viewModel = viewModel;
        }

        public override void UpdateState() {
            Enabled = _viewModel.IsSolutionLoaded && 
                      _viewModel.SelectedProject!=null && 
                      _viewModel.SelectedProject.Status != ProjectStatus.Closed;
        }

        public override void Execute(object parameter = null) {

            if (_viewModel.SelectedProject == null) {
                return;
            }

            if (!ShellUtil.ConfirmOkCancel($"'{_viewModel.SelectedProject.Name}' will be removed.")) {
                return;
            }

            ShellUtil.ReportUserOnFailed(_viewModel.SelectedProject.Remove());
        }
    }
}