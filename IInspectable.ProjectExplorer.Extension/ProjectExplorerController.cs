using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace IInspectable.ProjectExplorer.Extension {

    public class ProjectExplorerController {

        readonly IVsSolution  _solution1;
        readonly IVsSolution2 _solution2;
        readonly IVsSolution4 _solution4;

        public ProjectExplorerController() {

            _solution1 = ProjectExplorerPackage.GetGlobalService<SVsSolution, IVsSolution>();
            _solution2 = ProjectExplorerPackage.GetGlobalService<SVsSolution, IVsSolution2>();
            _solution4 = ProjectExplorerPackage.GetGlobalService<SVsSolution, IVsSolution4>();
        }

        public List<ProjectFile> LoadProjectFiles() {

            // TODO Verzeichnis aus Solution / File auslesen
            var path = @"C:\ws\Roslyn";

            var projectFiles = new List<ProjectFile>();

            foreach(var file in Directory.EnumerateFiles(path, "*.csproj", SearchOption.AllDirectories)) {
                var projectFile = ProjectFile.FromFile(file);

                projectFiles.Add(projectFile);
            }

            return projectFiles;
        }

        public List<ProjectViewModel> BindProjectFiles(List<ProjectFile> projectFiles) {
            var projectFileViewModels = new List<ProjectViewModel>();

            var projectsById = GetProjectsById();

            foreach (var projectFile in projectFiles) {

                var vm = new ProjectViewModel(projectFile);

                IVsHierarchy pHierarchy;
                if(projectsById.TryGetValue(projectFile.ProjectGuid, out pHierarchy)) {
                    vm.Bind(pHierarchy);
                }

                projectFileViewModels.Add(vm);
            }

            return projectFileViewModels;
        }

        public void UnloadProject(IVsHierarchy pHierarchy) {
            Guid projectGuid;
            ErrorHandler.ThrowOnFailure(_solution1.GetGuidOfProject(pHierarchy, out projectGuid));
            ErrorHandler.ThrowOnFailure(_solution4.UnloadProject(ref projectGuid, (uint)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser));
        }

        public void LoadProject(IVsHierarchy pHierarchy) {
            Guid projectGuid;
            ErrorHandler.ThrowOnFailure(_solution1.GetGuidOfProject(pHierarchy, out projectGuid));
            ErrorHandler.ThrowOnFailure(_solution4.ReloadProject(ref projectGuid));
        }

        public bool IsProjectUnloaded(IVsHierarchy pHierarchy) {
            //_VSProjectUnloadStatus status;
            object status;
            var hr=pHierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID5.VSHPROPID_ProjectUnloadStatus, out status);

            return ErrorHandler.Succeeded(hr);
        }

        public void CloseProject(IVsHierarchy pHierarchy) {
            _solution2.CloseSolutionElement((uint)__VSSLNCLOSEOPTIONS.SLNCLOSEOPT_DeleteProject, pHierarchy, 0);
        }
      
        Dictionary<Guid, IVsHierarchy> GetProjectsById() {

            var result = new Dictionary<Guid, IVsHierarchy>();

               Guid ignored = Guid.Empty;
            IEnumHierarchies hierEnum;
            var flags = __VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION | __VSENUMPROJFLAGS.EPF_UNLOADEDINSOLUTION;
            if (ErrorHandler.Failed(_solution1.GetProjectEnum((uint)flags, ref ignored, out hierEnum))) {
                return result;
            }

            IVsHierarchy[] hier = new IVsHierarchy[1];
            uint fetched;
            while ((hierEnum.Next((uint)hier.Length, hier, out fetched) == VSConstants.S_OK) && (fetched == hier.Length)) {
                int res;

                Guid projGuid;
                if (ErrorHandler.Failed(res = _solution1.GetGuidOfProject(hier[0], out projGuid))) {
                    Debug.Fail($"IVsolution::GetGuidOfProject retuend 0x{res:X}.");
                    continue;
                }

                string uniqueName;
                if (ErrorHandler.Failed(res = _solution1.GetUniqueNameOfProject(hier[0], out uniqueName))) {
                    Debug.Fail($"IVsolution::GetUniqueNameOfProject retuend 0x{res:X}.");
                    continue;
                }

                result[projGuid] = hier[0];
            }

            return result;
        }
    }

    public class ViewModelBase: INotifyPropertyChanged {

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

}