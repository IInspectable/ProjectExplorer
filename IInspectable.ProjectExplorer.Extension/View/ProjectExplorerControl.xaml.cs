#region Using Directives

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.Internal.VisualStudio.Shell;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Util = Microsoft.Internal.VisualStudio.PlatformUI.Utilities;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    partial class ProjectExplorerControl: UserControl, IVsWindowSearch, IDisposable {

        public ProjectExplorerControl(IVsWindowSearchHostFactory windowSearchHostFactory, ProjectExplorerViewModel viewModel) {
            ThreadHelper.ThrowIfNotOnUIThread();
            DataContext = viewModel;

            InitializeComponent();

            ViewModel.PropertyChanged                    += OnViewModelPropertyChanged;
            ViewModel.SolutionService.AfterCloseSolution += OnAfterCloseSolution;

            SearchHost = windowSearchHostFactory.CreateWindowSearchHost(SearchControlHost);
            SearchHost.SetupSearch(this);

            UpdateSearchEnabled();
        }

        IVsWindowSearchHost SearchHost { get; }

        ProjectExplorerViewModel ViewModel {
            get { return DataContext as ProjectExplorerViewModel; }
        }

        public void Dispose() {
            ViewModel.PropertyChanged                    -= OnViewModelPropertyChanged;
            ViewModel.SolutionService.AfterCloseSolution -= OnAfterCloseSolution;
        }

        void OnProjectListMouseDoubleClick(object sender, MouseButtonEventArgs e) {
            ViewModel.ExecuteDefaultAction();
        }

        void OnSettingsContextMenuOpening(object sender, ContextMenuEventArgs e) {
            // TODO Tastaturfall berücksichtigen (-1, -1)
            var source = e.OriginalSource as FrameworkElement;
            if (source == null) {
                return;
            }

            var ptScreen = source.PointToScreen(new Point(e.CursorLeft, e.CursorTop));
            ViewModel.ShowSettingsButtonContextMenu((int) ptScreen.X, (int) ptScreen.Y);

            e.Handled = true;
        }

        void OnProjectItemContextMenuOpening(object sender, ContextMenuEventArgs e) {
            // TODO Tastaturfall berücksichtigen (-1, -1)
            var source = e.OriginalSource as FrameworkElement;
            if (source == null) {
                return;
            }

            var ptScreen = source.PointToScreen(new Point(e.CursorLeft, e.CursorTop));
            ViewModel.ShowProjectItemContextMenu((int) ptScreen.X, (int) ptScreen.Y);

            e.Handled = true;
        }

        #region IVsWindowSearch

        public bool CanActivateSearch {
            get {
                ThreadHelper.ThrowIfNotOnUIThread();
                return SearchHost.IsVisible && SearchHost.IsEnabled;
            }
        }

        public void ActivateSearch() {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!CanActivateSearch) {
                return;
            }

            SearchHost?.Activate();
        }

        public void ActivateIfIsKeyboardFocusWithin() {
            if (SearchHost.IsEnabled &&
                (IsKeyboardFocusWithin || Keyboard.FocusedElement == null)) {
                ActivateSearch();
            }
        }

        void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (string.IsNullOrEmpty(e.PropertyName) ||
                e.PropertyName == nameof(ProjectExplorerViewModel.IsLoading)) {
                UpdateSearchEnabled();
                ActivateIfIsKeyboardFocusWithin();
            }
        }

        void UpdateSearchEnabled() {
            ThreadHelper.ThrowIfNotOnUIThread();
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
            Util.SetValue(pSearchSettings, SearchSettingsDataSource.SearchStartTypeProperty.Name,         (uint) VSSEARCHSTARTTYPE.SST_DELAYED);
            Util.SetValue(pSearchSettings, SearchSettingsDataSource.SearchProgressTypeProperty.Name,      (uint) VSSEARCHPROGRESSTYPE.SPT_INDETERMINATE);
            Util.SetValue(pSearchSettings, SearchSettingsDataSource.SearchUseMRUProperty.Name,            false);
            Util.SetValue(pSearchSettings, SearchSettingsDataSource.SearchPopupAutoDropdownProperty.Name, false);
            Util.SetValue(pSearchSettings, SearchSettingsDataSource.ControlMaxWidthProperty.Name,         (uint) 2000);
            Util.SetValue(pSearchSettings, SearchSettingsDataSource.SearchWatermarkProperty.Name,         GetWatermark());
        }

        static string GetWatermark() {

            var watermarkText = "Search Project Explorer";

            var keyBinding = KeyBindingHelper.GetGlobalKeyBinding(PackageGuids.ProjectExplorerWindowPackageCmdSetGuid, PackageIds.ProjectExplorerSearchCommandId);
            if (!String.IsNullOrEmpty(keyBinding)) {
                watermarkText += $" ({keyBinding})";
            }

            return watermarkText;
        }

        public bool OnNavigationKeyDown(uint dwNavigationKey, uint dwModifiers) {
            var modifier      = (__VSUIACCELMODIFIERS) dwModifiers;
            var navigationKey = (__VSSEARCHNAVIGATIONKEY) dwNavigationKey;

            if (modifier != __VSUIACCELMODIFIERS.VSAM_NONE) {
                return false;
            }

            switch (navigationKey) {
                case __VSSEARCHNAVIGATIONKEY.SNK_DOWN:
                    return Navigate(up: false);
                case __VSSEARCHNAVIGATIONKEY.SNK_UP:
                    return Navigate(up: true);
            }

            return false;
        }

        bool Navigate(bool up) {

            if (ProjectsControl.Items.Count == 0) {
                return false;
            }

            var nextSelectedIndex = ProjectsControl.SelectedIndex;

            if (up) {
                // keine Selektion
                if (nextSelectedIndex < 0) {
                    nextSelectedIndex = ProjectsControl.Items.Count;
                }

                nextSelectedIndex -= 1;

                if (nextSelectedIndex < 0) {
                    nextSelectedIndex = ProjectsControl.Items.Count - 1;
                }
            } else {
                // keine Selektion
                if (nextSelectedIndex < 0) {
                    nextSelectedIndex = -1;
                }

                nextSelectedIndex += 1;

                if (nextSelectedIndex >= ProjectsControl.Items.Count) {
                    nextSelectedIndex = 0;
                }
            }

            ProjectsControl.SelectedIndex = nextSelectedIndex;
            return true;

        }

        public bool SearchEnabled {
            get { return true; }
        }

        public Guid Category { get; } = new Guid("65511566-dab1-4298-b5c9-a82c4532001e");

        public IVsEnumWindowSearchFilters SearchFiltersEnum {
            get { return null; }
        }

        public IVsEnumWindowSearchOptions SearchOptionsEnum {
            get { return null; }
        }

        #endregion

    }

}