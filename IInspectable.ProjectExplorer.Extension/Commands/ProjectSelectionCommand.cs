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

            if (viewModel == null) {
                throw new ArgumentNullException(nameof(viewModel));
            }

            _viewModel = viewModel;        
        }

        protected IReadOnlyList<ProjectViewModel> SelectedItems {
            get { return _viewModel.SelectionService.SelectedItems; }
        }

        public ProjectExplorerViewModel ViewModel {
            get { return _viewModel; }
        }

        protected IWaitIndicator WaitIndicator {
            get { return _viewModel.WaitIndicator; }
        }

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
    }
}