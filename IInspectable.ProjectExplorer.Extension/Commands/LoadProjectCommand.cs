#region Using Directives

using System.Collections.Generic;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed class LoadProjectCommand : ProjectSelectionCommand {
        
        public LoadProjectCommand(ProjectExplorerViewModel viewModel):
            base(viewModel, PackageIds.LoadProjectCommandId) {             
        }

        protected override bool EnableOverride(ProjectViewModel projectViewModel) {
            return projectViewModel.Status == ProjectStatus.Unloaded;
        }

        protected override bool VisibleOverride(ProjectViewModel projectViewModel) {
            return EnableOverride(projectViewModel);
        }

        protected override void ExecuteOverride(IReadOnlyList<ProjectViewModel> projects) {

            foreach(var project in projects) {
                if(ShellUtil.ReportUserOnFailed(project.Reload())) {
                    break;
                }
            }
        }
    }
}