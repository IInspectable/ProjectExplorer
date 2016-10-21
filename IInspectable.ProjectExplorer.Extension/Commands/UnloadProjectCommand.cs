#region Using Directives

using System.Collections.Generic;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed class UnloadProjectCommand : ProjectSelectionCommand {

        public UnloadProjectCommand(ProjectExplorerViewModel viewModel):
            base(viewModel, PackageIds.UnloadProjectCommandId) {
        }

        protected override bool EnableOverride(ProjectItemViewModel projectItemViewModel) {
            return projectItemViewModel?.Status == ProjectStatus.Loaded;
        }

        protected override bool VisibleOverride(ProjectItemViewModel projectItemViewModel) {
            return EnableOverride(projectItemViewModel);
        }

        protected override void ExecuteOverride(IReadOnlyList<ProjectItemViewModel> projects) {

            ForeachWithWaitIndicatorAndErrorReport(projects, "Unloading", p => p.Unload());            
        }
    }
}