#region Using Directives

using System;
using System.Linq;
using System.Collections.Generic;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    abstract class ProjectSelectionCommand : Command {

        readonly ProjectExplorerViewModel _viewModel;

        protected ProjectSelectionCommand(ProjectExplorerViewModel viewModel, int commandId, Guid? menuGroupOrDefault = null) : 
            base(commandId, menuGroupOrDefault) {

            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));        
        }

        protected IReadOnlyList<ProjectViewModel> SelectedItems => _viewModel.SelectionService.SelectedItems;

        public ProjectExplorerViewModel ViewModel => _viewModel;

        protected IWaitIndicator WaitIndicator => _viewModel.WaitIndicator;

        public sealed override void UpdateState() {
            Enabled = SelectedItems.Any() && SelectedItems.All(EnableOverride);
            Visible = SelectedItems.Any() && SelectedItems.All(VisibleOverride);
        }

        public sealed override void Execute(object parameter = null) {

            UpdateState();

            if(!CanExecute()) {
                return;
            }

            ExecuteOverride(SelectedItems);
        }

        protected abstract bool EnableOverride(ProjectViewModel projectViewModel);
        protected abstract bool VisibleOverride(ProjectViewModel projectViewModel);
        protected abstract void ExecuteOverride(IReadOnlyList<ProjectViewModel> projects);

        protected void ForeachWithWaitIndicatorAndErrorReport(IReadOnlyList<ProjectViewModel> projects,
                                                              string verb, 
                                                              Func<ProjectViewModel, HResult> action,
                                                              bool breakOnFirstError=true) {
            try {

                bool allowCancel = projects.Count > 1;

                using (var indicator = WaitIndicator.StartWait("Project Explorer", "", allowCancel)) {
                    for (int i = 0; i < projects.Count; i++) {
                        var project = projects[i];

                        indicator.Message = $"{verb} project '{project.DisplayName}'.";

                        indicator.CancellationToken.ThrowIfCancellationRequested();

                        if (i == projects.Count - 1) {
                            indicator.AllowCancel = false;
                        }

                        if (ShellUtil.ReportUserOnFailed(action(project).Value) && breakOnFirstError) {
                            break;
                        }
                    }
                }
            } catch (OperationCanceledException) {
            }
        }
    }
}