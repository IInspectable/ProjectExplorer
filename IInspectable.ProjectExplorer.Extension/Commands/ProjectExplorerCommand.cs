﻿namespace IInspectable.ProjectExplorer.Extension {

    sealed class ProjectExplorerCommand : Command {

        readonly ProjectExplorerPackage _package;

        public ProjectExplorerCommand(ProjectExplorerPackage package) :
            base(PackageIds.ProjectExplorerCommandId) {
            _package = package;
        }

        public override void Execute(object parameter=null) {
            _package.ShowProjectExplorerWindow();
        }
    }
}