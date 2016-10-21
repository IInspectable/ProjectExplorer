#region Using Directives

using System.Collections.Generic;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed class AddProjectCommand : ProjectSelectionCommand {
        
        public AddProjectCommand(ProjectExplorerViewModel viewModel)
            : base(viewModel, PackageIds.AddProjectCommandId) {        
        }

        protected override bool EnableOverride(ProjectItemViewModel projectItemViewModel) {
            return projectItemViewModel.Status == ProjectStatus.Closed;
        }

        protected override bool VisibleOverride(ProjectItemViewModel projectItemViewModel) {
            return true;
        }

        protected override void ExecuteOverride(IReadOnlyList<ProjectItemViewModel> projects) {

            if (ShellUtil.ReportUserOnFailed(ViewModel.EnsureSolution())) {
                return;
            }

            ForeachWithWaitIndicatorAndErrorReport(projects, "Adding", p => p.Open());
        }        
    }
}