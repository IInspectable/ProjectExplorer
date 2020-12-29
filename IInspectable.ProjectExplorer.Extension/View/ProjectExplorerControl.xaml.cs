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

        ProjectExplorerViewModel ViewModel => DataContext as ProjectExplorerViewModel;

        public void Dispose() {
            ViewModel.PropertyChanged                    -= OnViewModelPropertyChanged;
            ViewModel.SolutionService.AfterCloseSolution -= OnAfterCloseSolution;
        }

        void OnProjectItemContextMenuOpening(object sender, ContextMenuEventArgs e) {

            var source = FindAncestor(e.OriginalSource);
            if (source == null) {
                return;
            }

            var ptScreen = source.PointToScreen(new Point(e.CursorLeft, e.CursorTop));

            // Tastaturfall
            if (e.CursorLeft < 0 && e.CursorTop < 0) {
                ptScreen.X += source.ActualWidth  / 2;
                ptScreen.Y += source.ActualHeight / 2;
            }

            ViewModel.ShowProjectItemContextMenu((int) ptScreen.X, (int) ptScreen.Y);

            e.Handled = true;

            FrameworkElement FindAncestor(object element) {
                while (element is FrameworkContentElement frameworkContentElement) {
                    element = frameworkContentElement.Parent;
                }

                return element as FrameworkElement;
            }
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
                    return ProjectsControl.Navigate(up: false);
                case __VSSEARCHNAVIGATIONKEY.SNK_UP:
                    return ProjectsControl.Navigate(up: true);
            }

            return false;
        }

        public bool SearchEnabled => true;

        public Guid Category { get; } = new("65511566-dab1-4298-b5c9-a82c4532001e");

        public IVsEnumWindowSearchFilters SearchFiltersEnum => null;

        public IVsEnumWindowSearchOptions SearchOptionsEnum => null;

        #endregion

    }

}