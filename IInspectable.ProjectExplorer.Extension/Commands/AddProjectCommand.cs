#region Using Directives

using System.Collections.Generic;

using Microsoft.VisualStudio.Shell;

#endregion

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
            
            ThreadHelper.ThrowIfNotOnUIThread();

            if (ShellUtil.ReportUserOnFailed(ViewModel.EnsureSolution())) {
                return;
            }

            ForeachWithWaitIndicatorAndErrorReport(projects, "Adding", p => p.Open());
        }        
    }
}