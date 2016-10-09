#region Using Directives

using System.ComponentModel.Design;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell;

#endregion

namespace IInspectable.ProjectExplorer.Extension {
    
    [Guid("65511566-dab1-4298-b5c9-a82c4532001e")]
    public class ProjectExplorerWindow : ToolWindowPane {

        // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
        readonly ProjectService _service;
        readonly ProjectExplorerViewModel _viewModel;
        readonly OptionService _optionService;
        // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable

        public ProjectExplorerWindow() : base(null) {

            _service = new ProjectService();
            _optionService = ProjectExplorerPackage.GetGlobalService<OptionService, OptionService>();
            _viewModel = new ProjectExplorerViewModel(_service, _optionService);

            Caption = "Project Explorer";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            // ReSharper disable once VirtualMemberCallInConstructor
            Content = new ProjectExplorerControl(_viewModel);
            ToolBar = new CommandID(PackageGuids.ProjectExplorerWindowPackageCmdSetGuid, PackageIds.ProjectExplorerToolbar);
            ProjectExplorerRefreshCommand.Initialize(this, _viewModel);
        }

        public override bool SearchEnabled {
            get { return true; }
        }

    }

}