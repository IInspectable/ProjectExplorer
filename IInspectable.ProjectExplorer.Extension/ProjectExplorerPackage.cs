#region Using Directives

using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(ProjectExplorerWindow), Style = VsDockStyle.Tabbed, Window = "3ae79031-e1bc-11d0-8f78-00a0c9110057")]
    [ProvideToolWindowVisibility(typeof(ProjectExplorerWindow), /*UICONTEXT_SolutionExists*/ "f1536ef8-92ec-443c-9ed7-fdadf150da82")]
    [Guid(PackageGuids.ProjectExplorerWindowPackageGuidString)]
    public sealed class ProjectExplorerPackage : Package {

        protected override void Initialize() {
            ProjectExplorerWindowCommand.Initialize(this);            
            base.Initialize();
        }
    }
}