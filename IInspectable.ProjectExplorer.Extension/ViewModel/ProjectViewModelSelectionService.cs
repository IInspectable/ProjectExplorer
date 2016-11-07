#region Using Directives

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed class ProjectViewModelSelectionService {

        readonly HashSet<ProjectViewModel> _selectedItems;

        public ProjectViewModelSelectionService() {
            _selectedItems = new HashSet<ProjectViewModel>();
        }

        public event EventHandler SelectionChanged;

        public IReadOnlyList<ProjectViewModel> SelectedItems {
            get { return _selectedItems.ToImmutableList(); }
        }

        public void AddSelection(ProjectViewModel projectViewModel) {
            if(IsSelected(projectViewModel) || projectViewModel==null) {
                return;
            }
            _selectedItems.Add(projectViewModel);
            OnSelectionChanged(projectViewModel);
        }

        public void RemoveSelection(ProjectViewModel projectViewModel) {
            if (!IsSelected(projectViewModel)) {
                return;
            }
            _selectedItems.Remove(projectViewModel);
            OnSelectionChanged(projectViewModel);
        }

        public void ClearSelection() {

            var projectViewModels = _selectedItems.ToList();

            _selectedItems.Clear();

            foreach(var projectViewModel in projectViewModels) {
                projectViewModel.NotifyIsSelectedChanged();
            }

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        internal bool IsSelected(ProjectViewModel viewmodel) {
            return _selectedItems.Contains(viewmodel);
        }

        void OnSelectionChanged(ProjectViewModel projectViewModel) {

            projectViewModel.NotifyIsSelectedChanged();

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

}