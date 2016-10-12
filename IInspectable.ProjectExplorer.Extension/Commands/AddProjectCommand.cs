using System;
using Microsoft.VisualStudio;

namespace IInspectable.ProjectExplorer.Extension {

    sealed class AddProjectCommand : Command {

        readonly ProjectExplorerViewModel _viewModel;

        public AddProjectCommand(ProjectExplorerViewModel viewModel)
            : base(PackageIds.AddProjectCommandId) {

            if (viewModel == null) {
                throw new ArgumentNullException(nameof(viewModel));
            }

            _viewModel = viewModel;
        }

        public override void UpdateState() {
           Enabled = _viewModel.SelectedProject ?.Status==ProjectStatus.Closed;
        }

        public override void Execute(object parameter = null) {

            int hr=_viewModel.SelectedProject?.Open() ?? VSConstants.S_OK;

            if (ErrorHandler.Failed(hr)) {
                ShellUtil.ReportErrorInfo(hr);
            }
        }
    }
}