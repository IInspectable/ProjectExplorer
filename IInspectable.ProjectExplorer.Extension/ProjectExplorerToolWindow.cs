#region Using Directives

using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Imaging;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    class ProjectExplorerToolWindowServices {

        public ProjectExplorerToolWindowServices(ProjectExplorerPackage package, OleMenuCommandService oleMenuCommandService, ProjectExplorerViewModelProvider viewModelProvider, IVsWindowSearchHostFactory windowSearchHostFactory, OptionService optionService, IWaitIndicator waitIndicator) {
            Package                 = package;
            OleMenuCommandService   = oleMenuCommandService;
            ViewModelProvider       = viewModelProvider;
            WindowSearchHostFactory = windowSearchHostFactory;
            OptionService           = optionService;
            WaitIndicator           = waitIndicator;
        }

        public ProjectExplorerPackage           Package                 { get; }
        public OleMenuCommandService            OleMenuCommandService   { get; }
        public ProjectExplorerViewModelProvider ViewModelProvider       { get; }
        public IVsWindowSearchHostFactory       WindowSearchHostFactory { get; }
        public OptionService                    OptionService           { get; }
        public IWaitIndicator                   WaitIndicator           { get; }

    }

    [Guid(GuidString)]
    class ProjectExplorerToolWindow: ToolWindowPane, IErrorInfoService {

        public const  string GuidString = "f3e3f345-a607-4f4c-9742-bb6415f2b062";
        public static Guid   Guid => new Guid(GuidString);
        public const  string Title = "Project Explorer";

        public ProjectExplorerToolWindow(ProjectExplorerToolWindowServices services): base(null) {
            // ReSharper disable VirtualMemberCallInConstructor

            var menuCommandService      = services.OleMenuCommandService;
            var viewModelProvider       = services.ViewModelProvider;
            var windowSearchHostFactory = services.WindowSearchHostFactory;

            ViewModel = viewModelProvider.CreateViewModel(services.Package, this, menuCommandService);

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            Content            = new ProjectExplorerControl(windowSearchHostFactory, ViewModel);
            ToolBar            = new CommandID(PackageGuids.ProjectExplorerWindowPackageCmdSetGuid, PackageIds.ProjectExplorerToolbar);
            Caption            = Title;
            BitmapImageMoniker = KnownMonikers.SearchFolderOpened;

            // ReSharper restore VirtualMemberCallInConstructor
        }

        ProjectExplorerViewModel ViewModel { get; }

        ProjectExplorerControl ProjectExplorerControl {
            get { return (ProjectExplorerControl) Content; }
        }

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
            ((IErrorInfoService) this).RemoveErrorInfoBar();

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