#region Using Directives

using System.IO;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", version: 2)]
    [ProvideToolWindow(typeof(ProjectExplorerWindow), Style = VsDockStyle.Tabbed, Window = "3ae79031-e1bc-11d0-8f78-00a0c9110057")]
  //  [ProvideToolWindowVisibility(typeof(ProjectExplorerWindow), VSConstants.UICONTEXT.SolutionExists_string)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string)]
    [Guid(PackageGuids.ProjectExplorerWindowPackageGuidString)]
    public sealed class ProjectExplorerPackage : Package {

        readonly OptionService _optionService;

        public ProjectExplorerPackage() {

            _optionService = new OptionService();

            AddOptionKey(_optionService.OptionKey);

            ((IServiceContainer)this).AddService(GetType(), this, promote: true);
            ((IServiceContainer)this).AddService(_optionService.GetType(), _optionService, promote: true);
        }

        protected override void Initialize() {

            var solution = new SolutionService();
            ((IServiceContainer)this).AddService(solution.GetType(), solution, promote: true);

            ShowProjectExplorerCommand.Initialize(this);            
            base.Initialize();
        }
        
        public static object GetGlobalService<TService>() where TService : class {
            return GetGlobalService(typeof(TService));
        }

        public static TInterface GetGlobalService<TService, TInterface>() where TInterface : class {
            return GetGlobalService(typeof(TService)) as TInterface;
        }

        protected override void OnLoadOptions(string key, Stream stream) {
            if(_optionService.OptionKey == key) {
                _optionService.LoadOptions(stream);
            }
            base.OnLoadOptions(key, stream);
        }

        protected override void OnSaveOptions(string key, Stream stream) {
            if (_optionService.OptionKey == key) {
                _optionService.SaveOptions(stream);
            }
            base.OnSaveOptions(key, stream);
        }
    }
}