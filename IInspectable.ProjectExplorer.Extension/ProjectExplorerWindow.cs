#region Using Directives

using System.ComponentModel.Design;

using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

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
            
            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            
            Content = new ProjectExplorerControl(ViewModel);
            ToolBar = new CommandID(PackageGuids.ProjectExplorerWindowPackageCmdSetGuid, PackageIds.ProjectExplorerToolbar);
            Caption = "Project Explorer";


            // ReSharper restore VirtualMemberCallInConstructor
        }

        
        internal ProjectExplorerViewModel ViewModel { get; }

        public override bool SearchEnabled {
            get { return true; }
        }

        public override void ProvideSearchSettings(IVsUIDataSource pSearchSettings) {
            ViewModel.SearchOptions.ProvideSearchSettings(pSearchSettings);
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

    class ProjectSearchTask: VsSearchTask {

        readonly ProjectExplorerViewModel _viewModel;

        public ProjectSearchTask(ProjectExplorerViewModel viewModel, uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback) 
                :base(dwCookie, pSearchQuery, pSearchCallback) {

            _viewModel     = viewModel;
        }

        protected override void OnStartSearch() {
            ThreadHelper.Generic.Invoke(() => {
                 _viewModel.ApplySearch(SearchQuery.SearchString);                
            });

            base.OnStartSearch();
        }

        protected override void OnStopSearch() {
            SearchResults = 0;
        }
    }
}