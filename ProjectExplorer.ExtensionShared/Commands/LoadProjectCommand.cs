#region Using Directives

using System.Collections.Generic;

using Microsoft.VisualStudio.Shell;

#endregion

namespace IInspectable.ProjectExplorer.Extension; 

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
        ThreadHelper.ThrowIfNotOnUIThread();

        ForeachWithWaitIndicatorAndErrorReport(projects, "Reloading", p => p.Reload());            
    }
}