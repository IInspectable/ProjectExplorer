#region Using Directives

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using Util = Microsoft.Internal.VisualStudio.PlatformUI.Utilities;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    partial class ProjectExplorerControl : UserControl, IVsWindowSearch, IDisposable {

        readonly Guid _searchCategory = new Guid("65511566-dab1-4298-b5c9-a82c4532001e");

        public ProjectExplorerControl(IVsWindowSearchHostFactory windowSearchHostFactory, ProjectExplorerViewModel viewModel) {

            DataContext = viewModel;

            InitializeComponent();

            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            ViewModel.SolutionService.AfterCloseSolution += OnAfterCloseSolution;

            SearchHost = windowSearchHostFactory.CreateWindowSearchHost(SearchControlHost);
            SearchHost.SetupSearch(this);

            UpdateSearchEnabled();
        }

        IVsWindowSearchHost SearchHost { get; }
        ProjectExplorerViewModel ViewModel { get { return DataContext as ProjectExplorerViewModel; } }

        public void Dispose() {
            ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            ViewModel.SolutionService.AfterCloseSolution -= OnAfterCloseSolution;
        }

        void OnProjectListMouseDoubleClick(object sender, MouseButtonEventArgs e) {
            ViewModel.ExecuteDefaultAction();
        }

        void OnSettingsContextMenuOpening(object sender, ContextMenuEventArgs e) {
            // TODO Tastaturfall berücksichtigen (-1, -1)
            var source = e.OriginalSource as FrameworkElement;
            if(source == null) {
                return;
            }

            var ptScreen=source.PointToScreen(new Point(e.CursorLeft, e.CursorTop));
            ViewModel.ShowSettingsButtonContextMenu((int)ptScreen.X, (int)ptScreen.Y);

            e.Handled = true;
        }

        void OnProjectItemContextMenuOpening(object sender, ContextMenuEventArgs e) {
            // TODO Tastaturfall berücksichtigen (-1, -1)
            var source = e.OriginalSource as FrameworkElement;
            if (source == null) {
                return;
            }

            var ptScreen = source.PointToScreen(new Point(e.CursorLeft, e.CursorTop));
            ViewModel.ShowProjectItemContextMenu((int)ptScreen.X, (int)ptScreen.Y);

            e.Handled = true;
        }

        #region IVsWindowSearch

        public bool CanActivateSearch {
            get { return SearchHost.IsVisible && SearchHost.IsEnabled; }
        }

        public void ActivateSearch() {
            if (!CanActivateSearch) {
                return;
            }
            SearchHost?.Activate();
        }

        void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (string.IsNullOrEmpty(e.PropertyName) ||
               e.PropertyName == nameof(ProjectExplorerViewModel.IsLoading)) {
                UpdateSearchEnabled();
            }
        }

        void UpdateSearchEnabled() {
            SearchHost.IsEnabled = !ViewModel.IsLoading && ViewModel.Projects.Count > 0;
        }

        void OnAfterCloseSolution(object sender, EventArgs e) {
            // TODO Clear search text
        }

        public IVsSearchTask CreateSearch(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback) {
            if (pSearchQuery == null || pSearchCallback == null)
                return null;

            return new ProjectSearchTask(ViewModel, dwCookie, pSearchQuery, pSearchCallback);
        }

        public void ClearSearch() {
            ViewModel.ClearSearch();
        }

        public void ProvideSearchSettings(IVsUIDataSource pSearchSettings) {
            Util.SetValue(pSearchSettings, SearchSettingsDataSource.SearchStartTypeProperty.Name, (uint)VSSEARCHSTARTTYPE.SST_DELAYED);
            Util.SetValue(pSearchSettings, SearchSettingsDataSource.SearchProgressTypeProperty.Name, (uint)VSSEARCHPROGRESSTYPE.SPT_INDETERMINATE);
            Util.SetValue(pSearchSettings, SearchSettingsDataSource.SearchUseMRUProperty.Name, false);
            Util.SetValue(pSearchSettings, SearchSettingsDataSource.SearchPopupAutoDropdownProperty.Name, false);
            Util.SetValue(pSearchSettings, SearchSettingsDataSource.ControlMaxWidthProperty.Name, (uint)2000);
            // TODO Shortcut Key anzeigen
            Util.SetValue(pSearchSettings, SearchSettingsDataSource.SearchWatermarkProperty.Name, "Search Project Explorer");
        }

        public bool OnNavigationKeyDown(uint dwNavigationKey, uint dwModifiers) {
            return false;
        }
        
        public bool SearchEnabled {
            get { return true; }
        }

        public Guid Category {
            get { return _searchCategory; }
        }

        public IVsEnumWindowSearchFilters SearchFiltersEnum {
            get { return null; }
        }

        public IVsEnumWindowSearchOptions SearchOptionsEnum {
            get { return null; }
        }

        #endregion
    }
}