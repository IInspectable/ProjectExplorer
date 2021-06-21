#region Using Directives

using System.Linq;
using System.Collections.Generic;

using Microsoft.VisualStudio.Shell;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed class RemoveProjectCommand : ProjectSelectionCommand {

        public RemoveProjectCommand(ProjectExplorerViewModel viewModel):
            base(viewModel, PackageIds.RemoveProjectCommandId) {
        }

        protected override bool EnableOverride(ProjectViewModel projectViewModel) {
            return projectViewModel.Status == ProjectStatus.Loaded ||
                   projectViewModel.Status == ProjectStatus.Unloaded; 
        }

        protected override bool VisibleOverride(ProjectViewModel projectViewModel) {
            return true;
        }

        protected override void ExecuteOverride(IReadOnlyList<ProjectViewModel> projects) {

            ThreadHelper.ThrowIfNotOnUIThread();

            string itemsList =  string.Join(", ", projects.Select(project => $"'{project.DisplayName}'"));

            if (!ShellUtil.ConfirmOkCancel($"{itemsList}{(projects.Count == 1 ? " " : "\r\n")}will be removed.")) {
                return;
            }

            ForeachWithWaitIndicatorAndErrorReport(projects, "Removing", p => p.Close());
        }        
    }
}