#region Using Directives

using System.Linq;
using System.Collections.Generic;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed class RemoveProjectCommand : ProjectSelectionCommand {

        public RemoveProjectCommand(ProjectExplorerViewModel viewModel):
            base(viewModel, PackageIds.RemoveProjectCommandId) {
        }

        protected override bool EnableOverride(ProjectItemViewModel projectItemViewModel) {
            return projectItemViewModel.Status == ProjectStatus.Loaded ||
                   projectItemViewModel.Status == ProjectStatus.Unloaded; 
        }

        protected override bool VisibleOverride(ProjectItemViewModel projectItemViewModel) {
            return true;
        }

        protected override void ExecuteOverride(IReadOnlyList<ProjectItemViewModel> projects) {

            string itemsList =  string.Join(", ", projects.Select(project => $"'{project.DisplayName}'"));

            if (!ShellUtil.ConfirmOkCancel($"{itemsList}{(projects.Count == 1 ? " " : "\r\n")}will be removed.")) {
                return;
            }

            ForeachWithWaitIndicatorAndErrorReport(projects, "Removing", p => p.Close());
        }        
    }
}