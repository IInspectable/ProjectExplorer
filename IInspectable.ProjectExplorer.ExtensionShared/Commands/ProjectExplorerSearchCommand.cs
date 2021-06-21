﻿using Microsoft.VisualStudio.Shell;

namespace IInspectable.ProjectExplorer.Extension {

    class ProjectExplorerSearchCommand: Command {

        readonly ProjectExplorerPackage _package;

        public ProjectExplorerSearchCommand(ProjectExplorerPackage package): base(PackageIds.ProjectExplorerSearchCommandId) {
            _package = package;
        }

        public override void Execute(object parameter = null) {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            using (_package.WaitIndicator.StartWait("Show Project Explorer", "", false)) {
                _package.ShowProjectExplorerWindowAndActivateSearch();
            }

        }

    }

}