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

        readonly HashSet<ProjectViewModel> _selectedItems;

        public ProjectViewModelSelectionService(ObservableCollection<ProjectViewModel> projects) {
            if (projects == null) {
                throw new ArgumentNullException(nameof(projects));
            }

            _selectedItems             =  new HashSet<ProjectViewModel>();
            projects.CollectionChanged += OnProjectCollectionChanged;
        }

        public event EventHandler SelectionChanged;

        public ImmutableList<ProjectViewModel> SelectedItems => _selectedItems.ToImmutableList();

        public void AddSelection(ProjectViewModel projectViewModel) {
            if (projectViewModel == null) {
                return;
            }

            if (_selectedItems.Add(projectViewModel)) {
                projectViewModel.NotifyIsSelectedChanged();
                NotifySelectionChanged();
            }

        }

        public void RemoveSelection(ProjectViewModel projectViewModel) {
            if (_selectedItems.Remove(projectViewModel)) {
                projectViewModel.NotifyIsSelectedChanged();
                NotifySelectionChanged();
            }
        }

        public void RemoveSelection(IEnumerable<ProjectViewModel> projectViewModels) {

            foreach (var projectViewModel in projectViewModels) {
                RemoveSelection(projectViewModel);
            }

        }

        public void ClearSelection() {

            var projectViewModels = _selectedItems.ToList();

            _selectedItems.Clear();

            foreach (var projectViewModel in projectViewModels) {
                projectViewModel.NotifyIsSelectedChanged();
            }

            NotifySelectionChanged();
        }

        internal bool IsSelected(ProjectViewModel viewmodel) {
            return _selectedItems.Contains(viewmodel);
        }

        void OnProjectCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    break;
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Move:
                    RemoveSelection(e.OldItems.OfType<ProjectViewModel>());
                    break;
                case NotifyCollectionChangedAction.Reset:
                    ClearSelection();
                    break;
            }

        }

        void NotifySelectionChanged() {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

    }

}