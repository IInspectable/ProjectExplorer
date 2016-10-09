#region Using Directives

using System.ComponentModel.Design;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell;

#endregion

namespace IInspectable.ProjectExplorer.Extension {
    
    [Guid("65511566-dab1-4298-b5c9-a82c4532001e")]
    public class ProjectExplorerWindow : ToolWindowPane {

        readonly ProjectExplorerViewModel _viewModel;

        public ProjectExplorerWindow() : base(null) {

            var solutionService = ProjectExplorerPackage.GetGlobalService<SolutionService, SolutionService>();
            var optionService   = ProjectExplorerPackage.GetGlobalService<OptionService, OptionService>();

            _viewModel = new ProjectExplorerViewModel(this, solutionService, optionService);
            
            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            // ReSharper disable once VirtualMemberCallInConstructor
            Content = new ProjectExplorerControl(_viewModel);
            ToolBar = new CommandID(PackageGuids.ProjectExplorerWindowPackageCmdSetGuid, PackageIds.ProjectExplorerToolbar);
            Caption = "Project Explorer";
        }

        public override bool SearchEnabled {
            get { return true; }
        }

        public override void ClearSearch() {
            base.ClearSearch();
            _viewModel.Reload();
        }
    }
}