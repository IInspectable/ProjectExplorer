#region Using Directives

using System.ComponentModel.Design;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Internal.VisualStudio.PlatformUI;

#endregion

namespace IInspectable.ProjectExplorer.Extension {
    
    [Guid("65511566-dab1-4298-b5c9-a82c4532001e")]
     class ProjectExplorerWindow : ToolWindowPane {

        public ProjectExplorerWindow() : base(null) {
            // ReSharper disable VirtualMemberCallInConstructor
            var solutionService = ProjectExplorerPackage.GetGlobalService<SolutionService, SolutionService>();
            var optionService   = ProjectExplorerPackage.GetGlobalService<OptionService, OptionService>();

            var menuCommandService= (OleMenuCommandService)GetService(typeof(IMenuCommandService));

            ViewModel = new ProjectExplorerViewModel(solutionService, optionService, menuCommandService);
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.

            Content = new ProjectExplorerControl(ViewModel);
            ToolBar = new CommandID(PackageGuids.ProjectExplorerWindowPackageCmdSetGuid, PackageIds.ProjectExplorerToolbar);
            Caption = "Project Explorer";

            // ReSharper restore VirtualMemberCallInConstructor
        }

        public override void OnToolWindowCreated() {
            base.OnToolWindowCreated();
            SearchHost.IsVisible = ViewModel.IsSolutionLoaded;
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
            if (string.IsNullOrEmpty(e.PropertyName) || 
                e.PropertyName == nameof(ProjectExplorerViewModel.IsSolutionLoaded)) {

                SearchHost.IsVisible = ViewModel.IsSolutionLoaded;
            }
            if(string.IsNullOrEmpty(e.PropertyName) ||
               e.PropertyName == nameof(ProjectExplorerViewModel.IsLoading)) {

                SearchHost.IsEnabled = !ViewModel.IsLoading;
            }
        }

        internal ProjectExplorerViewModel ViewModel { get; }

        public override bool SearchEnabled {
            get { return true; }
        }

        public override void ProvideSearchSettings(IVsUIDataSource pSearchSettings) {
            Utilities.SetValue(pSearchSettings, SearchSettingsDataSource.SearchStartTypeProperty.Name, (uint)VSSEARCHSTARTTYPE.SST_DELAYED);
            Utilities.SetValue(pSearchSettings, SearchSettingsDataSource.SearchProgressTypeProperty.Name, (uint)VSSEARCHPROGRESSTYPE.SPT_INDETERMINATE);
            Utilities.SetValue(pSearchSettings, SearchSettingsDataSource.SearchUseMRUProperty.Name, false);
            Utilities.SetValue(pSearchSettings, SearchSettingsDataSource.SearchPopupAutoDropdownProperty.Name, false);
            // TODO SHortcut Key anzeigen
            Utilities.SetValue(pSearchSettings, SearchSettingsDataSource.SearchWatermarkProperty.Name, "Search Project Explorer");
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