#region Using Directives

using System.Collections.Generic;
using System.Diagnostics;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed class OpenInFileExplorerCommand: ProjectSelectionCommand {

        public OpenInFileExplorerCommand(ProjectExplorerViewModel viewModel):
            base(viewModel, PackageIds.OpenInFileExplorerCommandId) {
        }

        protected override bool EnableOverride(ProjectViewModel projectViewModel) {
            return SelectedItems.Count <= 5;
        }

        protected override bool VisibleOverride(ProjectViewModel projectViewModel) {
            return true;
        }

        protected override void ExecuteOverride(IReadOnlyList<ProjectViewModel> projects) {
            foreach (var project in projects) {

                string args = $"/e, /select, \"{project.Path}\"";

                ProcessStartInfo info = new ProcessStartInfo {
                    FileName  = "explorer",
                    Arguments = args
                };
                Process.Start(info);
            }
        }

    }

}