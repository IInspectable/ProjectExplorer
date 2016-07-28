#region Using Directives

using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(ProjectExplorerWindow))]
    [Guid(PackageGuidString)]
    public sealed class ProjectExplorerPackage : Package {

        public const string PackageGuidString = "f2f16ece-71b7-4b31-a2f1-c91aca261509";

        protected override void Initialize() {
            ProjectExplorerWindowCommand.Initialize(this);
            base.Initialize();
        }
    }
}