using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace IInspectable.ProjectExplorer.Extension {

    /// <summary>
    /// Command handler
    /// </summary>
    sealed class ProjectExplorerWindowCommand {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = PackageIds.ProjectExplorerWindowCommandId;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = PackageGuids.ProjectExplorerWindowPackageCmdSetGuid;

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        readonly Package _package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectExplorerWindowCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        ProjectExplorerWindowCommand(Package package) {
            if (package == null) {
                throw new ArgumentNullException(nameof(package));
            }

            _package = package;

            OleMenuCommandService commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null) {
                var menuCommandId = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(ShowToolWindow, menuCommandId);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ProjectExplorerWindowCommand Instance {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        IServiceProvider ServiceProvider {
            get {
                return _package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package) {
            Instance = new ProjectExplorerWindowCommand(package);
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        void ShowToolWindow(object sender, EventArgs e) {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = _package.FindToolWindow(typeof(ProjectExplorerWindow), 0, true);
            if ((null == window) || (null == window.Frame)) {
                throw new NotSupportedException("Cannot create tool window");
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}