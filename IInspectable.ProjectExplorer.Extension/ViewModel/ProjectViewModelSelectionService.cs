#region Using Directives

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed class ProjectViewModelSelectionService {

        readonly HashSet<ProjectItemViewModel> _selectedItems;

        public ProjectViewModelSelectionService() {
            _selectedItems = new HashSet<ProjectItemViewModel>();
        }

        public event EventHandler SelectionChanged;

        public IReadOnlyList<ProjectItemViewModel> SelectedItems {
            get { return _selectedItems.ToImmutableList(); }
        }

        public void AddSelection(ProjectItemViewModel projectItemViewModel) {
            if(IsSelected(projectItemViewModel) || projectItemViewModel==null) {
                return;
            }
            _selectedItems.Add(projectItemViewModel);
            OnSelectionChanged(projectItemViewModel);
        }

        public void RemoveSelection(ProjectItemViewModel projectItemViewModel) {
            if (!IsSelected(projectItemViewModel)) {
                return;
            }
            _selectedItems.Remove(projectItemViewModel);
            OnSelectionChanged(projectItemViewModel);
        }

        public void ClearSelection() {

            var projectViewModels = _selectedItems.ToList();

            _selectedItems.Clear();

            foreach(var projectViewModel in projectViewModels) {
                projectViewModel.NotifyIsSelectedChanged();
            }

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        internal bool IsSelected(ProjectItemViewModel viewmodel) {
            return _selectedItems.Contains(viewmodel);
        }

        void OnSelectionChanged(ProjectItemViewModel projectItemViewModel) {

            projectItemViewModel.NotifyIsSelectedChanged();

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

}