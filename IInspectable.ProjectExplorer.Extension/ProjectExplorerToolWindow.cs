﻿#region Using Directives

using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using Util= Microsoft.Internal.VisualStudio.PlatformUI.Utilities;
using Microsoft.VisualStudio.Imaging;

#endregion

namespace IInspectable.ProjectExplorer.Extension {
    
    [Guid("65511566-dab1-4298-b5c9-a82c4532001e")]
    class ProjectExplorerToolWindow : ToolWindowPane {

        public ProjectExplorerToolWindow() : base(null) {
            // ReSharper disable VirtualMemberCallInConstructor
  
            var menuCommandService = (OleMenuCommandService)GetService(typeof(IMenuCommandService));
            var viewModelProvider  = ProjectExplorerPackage.GetGlobalService<ProjectExplorerViewModelProvider>();

            ViewModel = viewModelProvider.CreateViewModel(this, menuCommandService);
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            ViewModel.SolutionService.AfterCloseSolution += OnAfterCloseSolution;
            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.

            Content = new ProjectExplorerControl(ViewModel);
            ToolBar = new CommandID(PackageGuids.ProjectExplorerWindowPackageCmdSetGuid, PackageIds.ProjectExplorerToolbar);
            Caption = "Project Explorer";
            BitmapImageMoniker = KnownMonikers.SearchFolderOpened;
            
            // ReSharper restore VirtualMemberCallInConstructor
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                ViewModel.SolutionService.AfterCloseSolution -= OnAfterCloseSolution;
                ViewModel.Dispose();
            }

            base.Dispose(disposing);
        }

        public override void OnToolWindowCreated() {
            base.OnToolWindowCreated();
            UpdateSearchEnabled();
        }
        
        void OnAfterCloseSolution(object sender, EventArgs e) {
           // TODO Clear search text
        }

        InfoBarModel _errorInfoBar;
        
        protected override void OnInfoBarClosed(IVsInfoBarUIElement infoBarUI, IVsInfoBar infoBar) {
            if (infoBar == _errorInfoBar) {
                _errorInfoBar = null;
            }
            base.OnInfoBarClosed(infoBarUI, infoBar);
        }

        public void ShowErrorInfoBar(Exception ex) {
            RemoveErrorInfoBar();
            
            _errorInfoBar = new InfoBarModel(ex.Message, KnownMonikers.StatusError);
            AddInfoBar(_errorInfoBar);
        }

        public void RemoveErrorInfoBar() {
            if (_errorInfoBar != null) {
                RemoveInfoBar(_errorInfoBar);
                _errorInfoBar = null;
            }
        }

        public bool CanActivateSearch {
            get { return SearchHost.IsVisible && SearchHost.IsEnabled; }
        }

        public void ActivateSearch() {
            if(!CanActivateSearch) {
                return;
            }
            SearchHost?.Activate();
        }

        void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if(string.IsNullOrEmpty(e.PropertyName) ||
               e.PropertyName == nameof(ProjectExplorerViewModel.IsLoading)) {
                UpdateSearchEnabled();
            }
        }

        void UpdateSearchEnabled() {
            SearchHost.IsEnabled = !ViewModel.IsLoading && ViewModel.Projects.Count > 0;
        }

        internal ProjectExplorerViewModel ViewModel { get; }

        public override bool SearchEnabled {
            get { return true; }
        }

        public override void ProvideSearchSettings(IVsUIDataSource pSearchSettings) {
            Util.SetValue(pSearchSettings, SearchSettingsDataSource.SearchStartTypeProperty.Name, (uint)VSSEARCHSTARTTYPE.SST_DELAYED);
            Util.SetValue(pSearchSettings, SearchSettingsDataSource.SearchProgressTypeProperty.Name, (uint)VSSEARCHPROGRESSTYPE.SPT_INDETERMINATE);
            Util.SetValue(pSearchSettings, SearchSettingsDataSource.SearchUseMRUProperty.Name, false);
            Util.SetValue(pSearchSettings, SearchSettingsDataSource.SearchPopupAutoDropdownProperty.Name, false);
            Util.SetValue(pSearchSettings, SearchSettingsDataSource.ControlMaxWidthProperty.Name, (uint)500);
            // TODO SHortcut Key anzeigen
            Util.SetValue(pSearchSettings, SearchSettingsDataSource.SearchWatermarkProperty.Name, "Search Project Explorer");
        }

        public override void ClearSearch() {
            ViewModel.ClearSearch();            
        }

        public override IVsEnumWindowSearchOptions SearchOptionsEnum {
            get { return ViewModel.SearchOptions.SearchOptionsEnum; }
        }

        public override IVsSearchTask CreateSearch(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback) {

            if (pSearchQuery == null || pSearchCallback == null)
                return null;

            return new ProjectSearchTask(ViewModel, dwCookie, pSearchQuery, pSearchCallback);
        }    
    }
}