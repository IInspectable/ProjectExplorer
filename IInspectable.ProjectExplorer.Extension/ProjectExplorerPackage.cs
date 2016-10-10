#region Using Directives

using System;
using System.IO;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", version: 2)]
    [ProvideToolWindow(typeof(ProjectExplorerWindow), Style = VsDockStyle.Tabbed, Window = "3ae79031-e1bc-11d0-8f78-00a0c9110057")]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
    [Guid(PackageGuids.ProjectExplorerWindowPackageGuidString)]
    sealed class ProjectExplorerPackage : Package {

        readonly OptionService _optionService;
        readonly ProjectExplorerCommand _projectExplorerCommand;
        readonly ProjectExplorerSearchCommand _projectExplorerSearchCommand;

        public ProjectExplorerPackage() {

            _optionService = new OptionService();
            _projectExplorerCommand = new ProjectExplorerCommand(this);
            _projectExplorerSearchCommand = new ProjectExplorerSearchCommand(this);

            AddOptionKey(_optionService.OptionKey);            
        }

        public static ProjectExplorerPackage Instance {
            get { return GetGlobalService<ProjectExplorerPackage, ProjectExplorerPackage>(); }
        }

        internal ProjectExplorerCommand ProjectExplorerCommand {
            get { return _projectExplorerCommand; }
        }

        protected override void Initialize() {

            var solution = new SolutionService();

            ((IServiceContainer)this).AddService(GetType(), this, promote: true);
            ((IServiceContainer)this).AddService(_optionService.GetType(), _optionService, promote: true);
            ((IServiceContainer)this).AddService(solution.GetType(), solution, promote: true);

            var commandService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            
            _projectExplorerCommand.Register(commandService);
            _projectExplorerSearchCommand.Register(commandService);

            base.Initialize();
        }

        internal ProjectExplorerWindow GetProjectExplorerWindow() {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            var window = (ProjectExplorerWindow)FindToolWindow(typeof(ProjectExplorerWindow), 0, true);
            return window;
        }

        public void ShowProjectExplorerWindow() {

            var window = GetProjectExplorerWindow();
            if (window?.Frame == null) {
                throw new NotSupportedException("Cannot create tool window");
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        public static object GetGlobalService<TService>() where TService : class {
            return GetGlobalService(typeof(TService));
        }

        public static TInterface GetGlobalService<TService, TInterface>() where TInterface : class {
            return GetGlobalService(typeof(TService)) as TInterface;
        }

        protected override void OnLoadOptions(string key, Stream stream) {
            if(_optionService.OptionKey == key) {
                _optionService.LoadOptions(stream);
            }
            base.OnLoadOptions(key, stream);
        }

        protected override void OnSaveOptions(string key, Stream stream) {
            if (_optionService.OptionKey == key) {
                _optionService.SaveOptions(stream);
            }
            base.OnSaveOptions(key, stream);
        }
    }
}