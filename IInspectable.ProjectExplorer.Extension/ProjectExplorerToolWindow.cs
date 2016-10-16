#region Using Directives

using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Imaging;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    [Guid("65511566-dab1-4298-b5c9-a82c4532001e")]
    class ProjectExplorerToolWindow : ToolWindowPane, IErrorInfoService {

        public ProjectExplorerToolWindow() : base(null) {
            // ReSharper disable VirtualMemberCallInConstructor
  
            var menuCommandService = (OleMenuCommandService)GetService(typeof(IMenuCommandService));
            var viewModelProvider  = ProjectExplorerPackage.GetGlobalService<ProjectExplorerViewModelProvider>();
            var windowSearchHostFactory= ProjectExplorerPackage.GetGlobalService < SVsWindowSearchHostFactory, IVsWindowSearchHostFactory>();

            ViewModel = viewModelProvider.CreateViewModel(this, menuCommandService);

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            Content = new ProjectExplorerControl(windowSearchHostFactory, ViewModel);
            ToolBar = new CommandID(PackageGuids.ProjectExplorerWindowPackageCmdSetGuid, PackageIds.ProjectExplorerToolbar);
            Caption = "Project Explorer";
            BitmapImageMoniker = KnownMonikers.SearchFolderOpened;
            
            // ReSharper restore VirtualMemberCallInConstructor
        }

        ProjectExplorerViewModel ViewModel { get; }
        ProjectExplorerControl ProjectExplorerControl { get { return (ProjectExplorerControl)Content; } }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                ProjectExplorerControl.Dispose();
                ViewModel.Dispose();
            }

            base.Dispose(disposing);
        }
        
        #region IErrorInfoService

        InfoBarModel _errorInfoBar;
        
        protected override void OnInfoBarClosed(IVsInfoBarUIElement infoBarUI, IVsInfoBar infoBar) {
            if (infoBar == _errorInfoBar) {
                _errorInfoBar = null;
            }
            base.OnInfoBarClosed(infoBarUI, infoBar);
        }

        void IErrorInfoService.ShowErrorInfoBar(Exception ex) {
            ((IErrorInfoService)this).RemoveErrorInfoBar();
            
            _errorInfoBar = new InfoBarModel(ex.Message, KnownMonikers.StatusError);
            AddInfoBar(_errorInfoBar);
        }

        void IErrorInfoService.RemoveErrorInfoBar() {
            if (_errorInfoBar != null) {
                RemoveInfoBar(_errorInfoBar);
                _errorInfoBar = null;
            }
        }

        #endregion

        public bool CanActivateSearch {
            get { return ProjectExplorerControl.CanActivateSearch; }
        }

        public void ActivateSearch() {
            ProjectExplorerControl.ActivateSearch();
        }
    }
}