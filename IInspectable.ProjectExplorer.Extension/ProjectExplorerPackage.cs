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
        Style  = VsDockStyle.Tabbed,
        Window = "3ae79031-e1bc-11d0-8f78-00a0c9110057")]
    [Guid(PackageGuids.ProjectExplorerWindowPackageGuidString)]
    sealed class ProjectExplorerPackage: AsyncPackage {

        readonly Logger _logger = Logger.Create<ProjectExplorerPackage>();

        ImmutableList<Command>            _commands;
        ProjectExplorerToolWindowServices _services;

        public ProjectExplorerPackage() {
            AddOptionKey(OptionService.OptionKey);
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {

            _logger.Info($"{nameof(ProjectExplorerPackage)}.{nameof(Initialize)}");

            _services = await GetProjectExplorerToolWindowServicesAsync();

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            RegisterCommands(new List<Command> {
                new ProjectExplorerCommand(this),
                new ProjectExplorerSearchCommand(this)
            });

            async Task<ProjectExplorerToolWindowServices> GetProjectExplorerToolWindowServicesAsync() {

                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                var oleMenuCommandService   = (OleMenuCommandService) await GetServiceAsync(typeof(IMenuCommandService)).ConfigureAwait(true);
                var windowSearchHostFactory = (IVsWindowSearchHostFactory) await GetServiceAsync(typeof(SVsWindowSearchHostFactory)).ConfigureAwait(true);
                var componentModel          = (IComponentModel) await GetServiceAsync(typeof(SComponentModel)).ConfigureAwait(true);
                var mefServices             = componentModel.GetService<MefServices>();

                return new ProjectExplorerToolWindowServices(
                    package                : this,
                    oleMenuCommandService  : oleMenuCommandService,
                    viewModelProvider      : mefServices.ExplorerViewModelProvider,
                    windowSearchHostFactory: windowSearchHostFactory,
                    optionService          : mefServices.OptionService,
                    waitIndicator          : mefServices.WaitIndicator
                );
            }
        }
        
        public IWaitIndicator WaitIndicator => _services.WaitIndicator;

        public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid toolWindowType) {
            if (toolWindowType == ProjectExplorerToolWindow.Guid) {
                return this;
            }

            return null;
        }

        protected override string GetToolWindowTitle(Type toolWindowType, int id) {
            if (toolWindowType == typeof(ProjectExplorerToolWindow)) {
                return ProjectExplorerToolWindow.Title;
            }

            return base.GetToolWindowTitle(toolWindowType, id);
        }

        protected override Task<object> InitializeToolWindowAsync(Type toolWindowType, int id, CancellationToken cancellationToken) {
            if (toolWindowType == typeof(ProjectExplorerToolWindow)) {

                return Task.FromResult((object) _services);
            }

            return base.InitializeToolWindowAsync(toolWindowType, id, cancellationToken);
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                UnegisterCommands();
            }

            base.Dispose(disposing);
        }

        void RegisterCommands(IList<Command> commands) {

            var commandService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            foreach (var command in commands) {
                command.Register(commandService);
            }

            _commands = commands.ToImmutableList();
        }

        void UnegisterCommands() {

            if (_commands == null) {
                return;
            }

            var commandService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            foreach (var command in _commands) {
                command.Unregister(commandService);
            }

            _commands = null;
        }

        public void ShowProjectExplorerWindow() {

            JoinableTaskFactory.RunAsync(async () => { await ShowProjectExplorerWindowAsync(); });

        }

        public void ShowProjectExplorerWindowAndActivateSearch() {

            JoinableTaskFactory.RunAsync(async () => {
                var toolwindow = await ShowProjectExplorerWindowAsync();

                if (toolwindow.CanActivateSearch) {
                    toolwindow.ActivateSearch();
                }
            });

        }

        async Task<ProjectExplorerToolWindow> ShowProjectExplorerWindowAsync() {

            ToolWindowPane window = await ShowToolWindowAsync(
                toolWindowType   : typeof(ProjectExplorerToolWindow),
                id               : 0,
                create           : true,
                cancellationToken: DisposalToken);

            return (ProjectExplorerToolWindow) window;

        }

        public static IServiceProvider ServiceProvider => GetGlobalService<IServiceProvider, IServiceProvider>();

        public static TService GetGlobalService<TService>() where TService : class {
            return GetGlobalService(typeof(TService)) as TService;
        }

        public static TInterface GetGlobalService<TService, TInterface>() where TInterface : class {
            return GetGlobalService(typeof(TService)) as TInterface;
        }

        protected override void OnLoadOptions(string key, Stream stream) {
            // TODO Wenn der Explorer geöffnet wurde, nachdem eine Solutuon geladen wurde, haben wir noch keinen OptionService...
            if (OptionService.OptionKey == key) {
                _services.OptionService?.LoadOptions(stream);
            }

            base.OnLoadOptions(key, stream);
        }

        protected override void OnSaveOptions(string key, Stream stream) {
            if (OptionService.OptionKey == key) {
                _services.OptionService?.SaveOptions(stream);
            }

            base.OnSaveOptions(key, stream);
        }

        [Export]
        class MefServices {

            #pragma warning disable 0649
            [Import]
            OptionService _optionService;

            [Import]
            ProjectExplorerViewModelProvider _projectExplorerViewModelProvider;

            [Import]
            IWaitIndicator _waitIndicator;
            #pragma warning restore 0649

            public ProjectExplorerViewModelProvider ExplorerViewModelProvider => _projectExplorerViewModelProvider;
            public OptionService                    OptionService             => _optionService;
            public IWaitIndicator                   WaitIndicator             => _waitIndicator;

        }
    }

}