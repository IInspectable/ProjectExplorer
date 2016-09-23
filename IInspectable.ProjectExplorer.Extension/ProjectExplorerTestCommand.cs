#region Using Directives

using System;
using System.Windows;
using System.ComponentModel.Design;

using Microsoft.VisualStudio.Shell;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed class ProjectExplorerTestCommand {

        public const int CommandId = PackageIds.ProjectExplorerTestCommandId;
        public static readonly Guid CommandSet = PackageGuids.ProjectExplorerWindowPackageCmdSetGuid;

        readonly ProjectExplorerWindow _projectExplorerWindow;

        ProjectExplorerTestCommand(ProjectExplorerWindow projectExplorerWindow) {
            if (projectExplorerWindow == null) {
                throw new ArgumentNullException(nameof(projectExplorerWindow));
            }

            _projectExplorerWindow = projectExplorerWindow;

            OleMenuCommandService commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null) {
                var menuCommandId = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(Execute, menuCommandId);
                commandService.AddCommand(menuItem);
            }
        }

        public static ProjectExplorerTestCommand Instance { get; private set; }

        IServiceProvider ServiceProvider {
            get { return _projectExplorerWindow; }
        }

        public static void Initialize(ProjectExplorerWindow projectExplorerWindow) {
            Instance = new ProjectExplorerTestCommand(projectExplorerWindow);
        }

        void Execute(object sender, EventArgs e) {
            MessageBox.Show("ProjectExplorerTestCommandId");
        }
    }
}