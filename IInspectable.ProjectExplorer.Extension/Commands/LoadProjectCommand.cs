#region Using Directives

using System.Collections.Generic;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed class LoadProjectCommand : ProjectSelectionCommand {
        
        public LoadProjectCommand(ProjectExplorerViewModel viewModel):
            base(viewModel, PackageIds.LoadProjectCommandId) {             
        }

        protected override bool EnableOverride(ProjectItemViewModel projectItemViewModel) {
            return projectItemViewModel.Status == ProjectStatus.Unloaded;
        }

        protected override bool VisibleOverride(ProjectItemViewModel projectItemViewModel) {
            return EnableOverride(projectItemViewModel);
        }

        protected override void ExecuteOverride(IReadOnlyList<ProjectItemViewModel> projects) {

            ForeachWithWaitIndicatorAndErrorReport(projects, "Reloading", p => p.Reload());            
        }
    }
}