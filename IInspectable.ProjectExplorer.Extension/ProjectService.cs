#region Using Directives

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    public class ProjectService: IVsSolutionEvents, IVsSolutionEvents4, IDisposable {

        readonly IVsSolution  _solution1;
        readonly IVsSolution2 _solution2;
        readonly IVsSolution4 _solution4;

        readonly OptionService _optionService;

        uint _solutionEvents1Cookie;
        uint _solutionEvents4Cookie;

        public ProjectService() {

            _solution1 = ProjectExplorerPackage.GetGlobalService<SVsSolution, IVsSolution>();
            _solution2 = ProjectExplorerPackage.GetGlobalService<SVsSolution, IVsSolution2>();
            _solution4 = ProjectExplorerPackage.GetGlobalService<SVsSolution, IVsSolution4>();
            _optionService = ProjectExplorerPackage.GetGlobalService<OptionService, OptionService>();

            _solution1.AdviseSolutionEvents(this, out _solutionEvents1Cookie);
            _solution1.AdviseSolutionEvents(this, out _solutionEvents4Cookie);           
        }

        public void Dispose() {
            if(_solutionEvents1Cookie != 0) {
                _solution1.UnadviseSolutionEvents(_solutionEvents1Cookie);
                _solutionEvents1Cookie = 0;
            }

            if (_solutionEvents4Cookie != 0) {
                _solution1.UnadviseSolutionEvents(_solutionEvents4Cookie);
                _solutionEvents4Cookie = 0;
            }
        }

        public string ProjectsRoot {
            get { return _optionService.ProjectsRoot; }
        }

        public List<ProjectFile> LoadProjectFiles() {

            var path = ProjectsRoot;

            var projectFiles = new List<ProjectFile>();

            foreach(var file in Directory.EnumerateFiles(path, "*.csproj", SearchOption.AllDirectories)) {
                var projectFile = ProjectFile.FromFile(file);

                projectFiles.Add(projectFile);
            }

            return projectFiles;
        }

        public List<ProjectViewModel> BindToHierarchy(List<ProjectFile> projectFiles) {
            var projectFileViewModels = new List<ProjectViewModel>();

            var projectHierarchyById = GetProjectHierarchyById();

            foreach (var projectFile in projectFiles) {

                var vm = new ProjectViewModel(projectFile);

                IVsHierarchy pHierarchy;
                if(projectHierarchyById.TryGetValue(projectFile.ProjectGuid, out pHierarchy)) {
                    vm.Bind(pHierarchy);
                }

                projectFileViewModels.Add(vm);
            }

            return projectFileViewModels;
        }

        public void OpenProject(string path) {

            Guid empty = Guid.Empty;
            Guid projId=Guid.Empty;
            IntPtr ppProj;
            // TODO: Fehlerbehandlung
            _solution1.CreateProject(
                rguidProjectType: ref empty, 
                lpszMoniker     : path, 
                lpszLocation    : null,
                lpszName        : null, 
                grfCreateFlags  : (uint)__VSCREATEPROJFLAGS.CPF_OPENFILE, 
                iidProject      : ref projId, 
                ppProject       : out ppProj);
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

        public Guid GetProjectGuid(IVsHierarchy pHierarchy) {
            int res;
            Guid projGuid;

            if (ErrorHandler.Failed(res = _solution1.GetGuidOfProject(pHierarchy, out projGuid))) {
                Debug.WriteLine($"IVsolution::GetGuidOfProject retuend 0x{res:X}.");
            }

            return projGuid;
        }

        public IVsHierarchy GetHierarchyByProjectGuid(Guid projectGuid) {

            int res;
            IVsHierarchy result;
            if(ErrorHandler.Failed(res = _solution1.GetProjectOfGuid(projectGuid, out result))) {
                Debug.WriteLine($"IVsolution::GetGuidOfProject retuend 0x{res:X}.");
            }

            return result;
        }

        public string GetUniqueNameOfProject(IVsHierarchy pHierarchy) {
            int res;
            string uniqueName;
            if (ErrorHandler.Failed(res = _solution1.GetUniqueNameOfProject(pHierarchy, out uniqueName))) {
                Debug.WriteLine($"IVsolution::GetUniqueNameOfProject retuend 0x{res:X}.");
            }

            return uniqueName;
        }

        Dictionary<Guid, IVsHierarchy> GetProjectHierarchyById() {

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

                Guid projGuid = GetProjectGuid(hier[0]);

                result[projGuid] = hier[0];
            }

            return result;
        }

        #region IVsSolutionEvents

        public event EventHandler<ProjectEventArgs> AfterOpenProject;
        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) {
            AfterOpenProject?.Invoke(this, new ProjectEventArgs(pHierarchy));
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) {
            return VSConstants.S_OK;
        }

        public event EventHandler<ProjectEventArgs> BeforeRemoveProject;
        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) {
            if(fRemoved == 1) {                
                BeforeRemoveProject?.Invoke(this, new ProjectEventArgs(pHierarchy));
            }
            return VSConstants.S_OK;
        }

        public event EventHandler<ProjectEventArgs> AfterLoadProject;
        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) {
            AfterLoadProject?.Invoke(this, new ProjectEventArgs(pRealHierarchy, pStubHierarchy));
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) {
            return VSConstants.S_OK;
        }

        public event EventHandler<ProjectEventArgs> BeforeUnloadProject;
        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) {
            BeforeUnloadProject?.Invoke(this, new ProjectEventArgs(pRealHierarchy, pStubHierarchy));
            return VSConstants.S_OK;
        }
        
        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution) {

           
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved) {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved) {
            return VSConstants.S_OK;
        }

        #endregion

        #region IVsSolutionEvents4

        int IVsSolutionEvents4.OnAfterRenameProject(IVsHierarchy pHierarchy) {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents4.OnQueryChangeProjectParent(IVsHierarchy pHierarchy, IVsHierarchy pNewParentHier, ref int pfCancel) {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents4.OnAfterChangeProjectParent(IVsHierarchy pHierarchy) {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents4.OnAfterAsynchOpenProject(IVsHierarchy pHierarchy, int fAdded) {
            return VSConstants.S_OK;
        }

        #endregion
    }

}