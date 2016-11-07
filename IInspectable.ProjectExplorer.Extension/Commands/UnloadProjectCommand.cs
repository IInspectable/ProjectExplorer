#region Using Directives

using System.Collections.Generic;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed class UnloadProjectCommand : ProjectSelectionCommand {

        public UnloadProjectCommand(ProjectExplorerViewModel viewModel):
            base(viewModel, PackageIds.UnloadProjectCommandId) {
        }

        protected override bool EnableOverride(ProjectViewModel projectViewModel) {
            return projectViewModel?.Status == ProjectStatus.Loaded;
        }

        protected override bool VisibleOverride(ProjectViewModel projectViewModel) {
            return EnableOverride(projectViewModel);
        }

        protected override void ExecuteOverride(IReadOnlyList<ProjectViewModel> projects) {

            ForeachWithWaitIndicatorAndErrorReport(projects, "Unloading", p => p.Unload());            
        }
    }
}