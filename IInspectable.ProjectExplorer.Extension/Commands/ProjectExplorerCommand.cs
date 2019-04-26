namespace IInspectable.ProjectExplorer.Extension {

    sealed class ProjectExplorerCommand: Command {

        readonly ProjectExplorerPackage _package;

        public ProjectExplorerCommand(ProjectExplorerPackage package):
            base(PackageIds.ProjectExplorerCommandId) {
            _package = package;
        }

        public override void Execute(object parameter = null) {
            using (_package.WaitIndicator.StartWait("Show Project Explorer", "", false)) {
                _package.ShowProjectExplorerWindow();
            }
        }

    }

}