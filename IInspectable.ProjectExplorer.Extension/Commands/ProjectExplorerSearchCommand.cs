namespace IInspectable.ProjectExplorer.Extension {

    class ProjectExplorerSearchCommand : Command {

        readonly ProjectExplorerPackage _package;

        public ProjectExplorerSearchCommand(ProjectExplorerPackage package) : base(PackageIds.ProjectExplorerSearchCommandId) {
            _package = package;
        }
        
        public override void Execute(object parameter = null) {

            _package.ShowProjectExplorerWindowAndActivateSearch();
            
        }
    }
}