#region Using Directives

using System;
using System.ComponentModel.Design;

using Microsoft.VisualStudio.Shell;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed class ProjectExplorerRefreshCommand {

        public const int CommandId = PackageIds.ProjectExplorerRefreshCommandId;
        public static readonly Guid CommandSet = PackageGuids.ProjectExplorerWindowPackageCmdSetGuid;

        readonly ProjectExplorerViewModel _viewModel;
        readonly MenuCommand _command;

        ProjectExplorerRefreshCommand(IServiceProvider serviceProvider, ProjectExplorerViewModel viewModel) {

            if (serviceProvider == null) {
                throw new ArgumentNullException(nameof(serviceProvider));
            }
            if (viewModel == null) {
                throw new ArgumentNullException(nameof(viewModel));
            }

            _viewModel = viewModel;

            OleMenuCommandService commandService = serviceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null) {
                var menuCommandId = new CommandID(CommandSet, CommandId);
                _command = new MenuCommand(Execute, menuCommandId);
                commandService.AddCommand(_command);
            }
        }

        public static ProjectExplorerRefreshCommand Instance { get; private set; }

        public static void Initialize(IServiceProvider serviceProvider, ProjectExplorerViewModel viewModel) {
            Instance = new ProjectExplorerRefreshCommand(serviceProvider, viewModel);
        }

        void Execute(object sender, EventArgs e) {
            _viewModel.Reload();
        }
    }
}