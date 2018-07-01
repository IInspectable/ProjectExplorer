#region Using Directives

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Composition;
using System.Threading;

using Microsoft.VisualStudio.ComponentModelHost;

using Task = System.Threading.Tasks.Task;
using System.Threading.Tasks;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", version: 1)]
    [ProvideToolWindow(typeof(ProjectExplorerToolWindow), 
        Style = VsDockStyle.Tabbed, 
        Window = "3ae79031-e1bc-11d0-8f78-00a0c9110057")]
    [Guid(PackageGuids.ProjectExplorerWindowPackageGuidString)]
    sealed class ProjectExplorerPackage : AsyncPackage {

        readonly Logger _logger = Logger.Create<ProjectExplorerPackage>();

        [Import]
        OptionService _optionService;
        [Import]
        ProjectExplorerViewModelProvider _projectExplorerViewModelProvider;

        ImmutableList<Command> _commands;

        public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid toolWindowType)
        {
            if (toolWindowType == ProjectExplorerToolWindow.Guid) {
                return this;
            }

            return null;
        }

        protected override string GetToolWindowTitle(Type toolWindowType, int id)
        {
            if (toolWindowType == typeof(ProjectExplorerToolWindow))
            {
                return ProjectExplorerToolWindow.Title;
            }

            return base.GetToolWindowTitle(toolWindowType, id);
        }

        protected override async Task<object> InitializeToolWindowAsync(Type toolWindowType, int id, CancellationToken cancellationToken)
        {
            if (toolWindowType == typeof(ProjectExplorerToolWindow)) {

                return new ProjectExplorerToolWindowServices(
                    await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService,
                    _projectExplorerViewModelProvider,
                    #pragma warning disable VSTHRD010
                    await GetServiceAsync(typeof(SVsWindowSearchHostFactory)) as IVsWindowSearchHostFactory
                    #pragma warning restore VSTHRD010
                );
            }
            return base.InitializeToolWindowAsync(toolWindowType, id, cancellationToken);
        }


        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            AddOptionKey(OptionService.OptionKey);           

            _logger.Info($"{nameof(ProjectExplorerPackage)}.{nameof(Initialize)}");

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var cmp = await GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            cmp?.DefaultCompositionService.SatisfyImportsOnce(this);

            RegisterCommands(new List<Command> {
                new ProjectExplorerCommand(this),
                new ProjectExplorerSearchCommand(this)
            });

        }
        
        protected override void Dispose(bool disposing) {
            if(disposing) {
                UnegisterCommands();
                if (_projectExplorerViewModelProvider != null) {

                ((IServiceContainer)this).RemoveService(_projectExplorerViewModelProvider.GetType(), true);
                }
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

       

        public void ShowProjectExplorerWindow() {

            JoinableTaskFactory.RunAsync(async () =>
            {
                await ShowProjectExplorerWindowAsync();
            });

        }

        public void ShowProjectExplorerWindowAndActivateSearch() {

            JoinableTaskFactory.RunAsync(async () =>
            {
                var toolwindow =await ShowProjectExplorerWindowAsync();

                if(toolwindow.CanActivateSearch) {                
                    toolwindow.ActivateSearch();
                }
            });

        }

        async Task<ProjectExplorerToolWindow> ShowProjectExplorerWindowAsync() {

                ToolWindowPane window = await ShowToolWindowAsync(
                    typeof(ProjectExplorerToolWindow),
                    0,
                    create: true,
                    cancellationToken: DisposalToken);

            return (ProjectExplorerToolWindow)window;

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