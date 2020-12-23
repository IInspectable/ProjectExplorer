#region Using Directives

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed class ProjectViewModelSelectionService {

        readonly ObservableCollection<ProjectViewModel> _projects;

        readonly HashSet<ProjectViewModel> _selectedItems;

        public ProjectViewModelSelectionService(ObservableCollection<ProjectViewModel> projects) {
            _projects                   =  projects ?? throw new ArgumentNullException(nameof(projects));
            _selectedItems              =  new HashSet<ProjectViewModel>();
            _projects.CollectionChanged += OnProjectCollectionChanged;
        }

        public event EventHandler SelectionChanged;

        public ImmutableList<ProjectViewModel> SelectedItems => _selectedItems.ToImmutableList();

        public void AddSelection(ProjectViewModel projectViewModel) {
            if (IsSelected(projectViewModel) || projectViewModel == null) {
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

            foreach (var projectViewModel in projectViewModels) {
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

        void OnProjectCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            // ReSharper disable once UnusedVariable
            var removed = _selectedItems.RemoveWhere(vm => !_projects.Contains(vm));
        }

    }

}