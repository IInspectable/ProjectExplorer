using System;
using System.Collections.Generic;

namespace IInspectable.ProjectExplorer.Extension {

    sealed class AddProjectCommand : ProjectSelectionCommand {
        
        public AddProjectCommand(ProjectExplorerViewModel viewModel)
            : base(viewModel, PackageIds.AddProjectCommandId) {        
        }

        protected override bool EnableOverride(ProjectViewModel projectViewModel) {
            return projectViewModel.Status == ProjectStatus.Closed;
        }

        protected override bool VisibleOverride(ProjectViewModel projectViewModel) {
            return true;
        }

        protected override void ExecuteOverride(IReadOnlyList<ProjectViewModel> projects) {

            if (ShellUtil.ReportUserOnFailed(ViewModel.EnsureSolution())) {
                return;
            }

            try {
                // TODO message/disable cancel
                using(var indicator = WaitIndicator.StartWait("Project Explorer", "message", true)) {
                    foreach(var project in projects) {

                        indicator.Message = $"Adding project '{project.Name}'.";

                        indicator.CancellationToken.ThrowIfCancellationRequested();

                        if(ShellUtil.ReportUserOnFailed(project.Open())) {
                            break;
                        }
                    }
                }
            } catch(OperationCanceledException) {
            }

        }        
    }
}