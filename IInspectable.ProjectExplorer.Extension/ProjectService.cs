#region Using Directives

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    class ProjectService: IVsSolutionEvents, IVsSolutionEvents4, IDisposable {

        readonly IVsSolution  _solution1;
        readonly IVsSolution2 _solution2;
        readonly IVsSolution4 _solution4;

        uint _solutionEvents1Cookie;
        uint _solutionEvents4Cookie;

        public ProjectService() {

            _solution1 = ProjectExplorerPackage.GetGlobalService<SVsSolution, IVsSolution>();
            _solution2 = ProjectExplorerPackage.GetGlobalService<SVsSolution, IVsSolution2>();
            _solution4 = ProjectExplorerPackage.GetGlobalService<SVsSolution, IVsSolution4>();
            
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

        public IVsSolution Solution1 {
            get { return _solution1; }
        }

        public IVsSolution2 Solution2 {
            get { return _solution2; }
        }

        public IVsSolution4 Solution4 {
            get { return _solution4; }
        }

        public List<ProjectFile> LoadProjectFiles(string path) {

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

                Hierarchy pHierarchy;
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
        
        public Guid GetProjectGuid(IVsHierarchy pHierarchy) {
            int res;
            Guid projGuid;

            if (ErrorHandler.Failed(res = _solution1.GetGuidOfProject(pHierarchy, out projGuid))) {
                Debug.WriteLine($"IVsolution::GetGuidOfProject retuend 0x{res:X}.");
            }

            return projGuid;
        }

        public Hierarchy GetHierarchyByProjectGuid(Guid projectGuid) {

            int res;
            IVsHierarchy result;
            if(ErrorHandler.Failed(res = _solution1.GetProjectOfGuid(projectGuid, out result))) {
                Debug.WriteLine($"IVsolution::GetGuidOfProject retuend 0x{res:X}.");
                return null;
            }

            return new Hierarchy(this, result);
        }

        Dictionary<Guid, Hierarchy> GetProjectHierarchyById() {

            var result = new Dictionary<Guid, Hierarchy>();

            Guid ignored = Guid.Empty;
            IEnumHierarchies hierEnum;
            var flags = __VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION | __VSENUMPROJFLAGS.EPF_UNLOADEDINSOLUTION;
            if (ErrorHandler.Failed(_solution1.GetProjectEnum((uint)flags, ref ignored, out hierEnum))) {
                return result;
            }

            IVsHierarchy[] hier = new IVsHierarchy[1];
            uint fetched;
            while ((hierEnum.Next((uint)hier.Length, hier, out fetched) == VSConstants.S_OK) && (fetched == hier.Length)) {

                var hierarchy = new Hierarchy(this, hier[0]);

                result[hierarchy.GetProjectGuid()] =hierarchy;
            }

            return result;
        }

        #region IVsSolutionEvents

        public event EventHandler<ProjectEventArgs> AfterOpenProject;
        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) {

            var realHierarchy = new Hierarchy(this, pHierarchy);
            AfterOpenProject?.Invoke(this, new ProjectEventArgs(realHierarchy));

            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) {
            return VSConstants.S_OK;
        }

        public event EventHandler<ProjectEventArgs> BeforeRemoveProject;
        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) {

            if(fRemoved == 1) {
                var realHierarchy = new Hierarchy(this, pHierarchy);
                BeforeRemoveProject?.Invoke(this, new ProjectEventArgs(realHierarchy));
            }

            return VSConstants.S_OK;
        }

        public event EventHandler<ProjectEventArgs> AfterLoadProject;
        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) {

            var realHierarchy = new Hierarchy(this, pStubHierarchy);
            var stubHierarchy = new Hierarchy(this, pRealHierarchy);
            AfterLoadProject?.Invoke(this, new ProjectEventArgs(realHierarchy, stubHierarchy));

            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) {
            return VSConstants.S_OK;
        }

        public event EventHandler<ProjectEventArgs> BeforeUnloadProject;
        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) {
            var realHierarchy = new Hierarchy(this, pStubHierarchy);
            var stubHierarchy = new Hierarchy(this, pRealHierarchy);
            BeforeUnloadProject?.Invoke(this, new ProjectEventArgs(realHierarchy, stubHierarchy));
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