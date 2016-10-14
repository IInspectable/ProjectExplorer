#region Using Directives

using System.Linq;
using System.Collections.Generic;

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

            string itemsList =  string.Join(", ", projects.Select(project => $"'{project.Name}'"));

            if (!ShellUtil.ConfirmOkCancel($"{itemsList}{(projects.Count == 1 ? " " : "\r\n")}will be removed.")) {
                return;
            }
            // TODO Wait Dialog
            foreach (var project in projects) {
                ShellUtil.ReportUserOnFailed(project.Close());
            }
        }        
    }
}