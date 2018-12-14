#region Using Directives

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Composition;
using System.Threading;

using Task = System.Threading.Tasks.Task;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading =true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", version: 2)]
    [ProvideToolWindow(typeof(ProjectExplorerToolWindow), Style = VsDockStyle.Tabbed, Window = "3ae79031-e1bc-11d0-8f78-00a0c9110057")]

    [Guid(PackageGuids.ProjectExplorerWindowPackageGuidString)]
    sealed class ProjectExplorerPackage : AsyncPackage {

      //  readonly Logger _logger = Logger.Create<ProjectExplorerPackage>();

        [Import]
        OptionService _optionService;
        [Import]
        ProjectExplorerViewModelProvider _projectExplorerViewModelProvider;

        ImmutableList<Command> _commands;

        public ProjectExplorerPackage() {
            AddOptionKey(OptionService.OptionKey);            
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
            await base.InitializeAsync(cancellationToken, progress).ConfigureAwait(false);


            var componentModel = (IComponentModel)await GetServiceAsync(typeof(SComponentModel));

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            componentModel.DefaultCompositionService.SatisfyImportsOnce(this);

            ((IServiceContainer)this).AddService(_projectExplorerViewModelProvider.GetType(), _projectExplorerViewModelProvider, promote: true);

            RegisterCommands(new List<Command> {
                new ProjectExplorerCommand(this),
                new ProjectExplorerSearchCommand(this)
            });
        }

        //protected override void Initialize() {

        //    _logger.Info($"{nameof(ProjectExplorerPackage)}.{nameof(Initialize)}");

        //    var componentModel = GetGlobalService<SComponentModel, IComponentModel>();            
        //    componentModel.DefaultCompositionService.SatisfyImportsOnce(this);

        //    ((IServiceContainer)this).AddService(_projectExplorerViewModelProvider.GetType(), _projectExplorerViewModelProvider, promote: true);

        //    RegisterCommands(new List<Command> {
        //        new ProjectExplorerCommand(this),
        //        new ProjectExplorerSearchCommand(this)
        //    });

        //    base.Initialize();
        //}

        protected override void Dispose(bool disposing) {
            if(disposing) {
                UnegisterCommands();
                ((IServiceContainer)this).RemoveService(_projectExplorerViewModelProvider.GetType(), true);
            }
            base.Dispose(disposing);
        }

        void RegisterCommands(IList<Command> commands) {

            var commandService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            foreach(var command in commands) {
                command.Register(commandService);
            }
            _commands = commands.ToImmutableList();
        }

        void UnegisterCommands() {

            if(_commands == null) {
                return;
            }
            var commandService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            foreach (var command in _commands) {
                command.Unregister(commandService);
            }
            _commands = null;
        }

        public ProjectExplorerToolWindow GetProjectExplorerToolWindow() {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            var window = (ProjectExplorerToolWindow)FindToolWindow(typeof(ProjectExplorerToolWindow), 0, true);
            return window;
        }

        public void ShowProjectExplorerWindow() {

            var window = GetProjectExplorerToolWindow();
            if (window?.Frame == null) {
                throw new NotSupportedException("Cannot create tool window");
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        public static TService GetGlobalService<TService>() where TService : class {
            return GetGlobalService(typeof(TService)) as TService;
        }

        public static TInterface GetGlobalService<TService, TInterface>() where TInterface : class {
            return GetGlobalService(typeof(TService)) as TInterface;
        }

        protected override void OnLoadOptions(string key, Stream stream) {
            if(OptionService.OptionKey == key) {
                _optionService.LoadOptions(stream);
            }
            base.OnLoadOptions(key, stream);
        }

        protected override void OnSaveOptions(string key, Stream stream) {
            if (OptionService.OptionKey == key) {
                _optionService.SaveOptions(stream);
            }
            base.OnSaveOptions(key, stream);
        }
    }
}