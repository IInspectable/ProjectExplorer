#region Using Directives

using System;
using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    class Hierarchy {

        readonly SolutionService _solutionService;
        readonly IVsHierarchy _vsHierarchy;

        public Hierarchy(SolutionService solutionService, IVsHierarchy vsHierarchy) {

            if(solutionService == null) {
                throw new ArgumentNullException(nameof(solutionService));
            }

            if (vsHierarchy == null) {
                throw new ArgumentNullException(nameof(vsHierarchy));
            }

            _solutionService = solutionService;
            _vsHierarchy     = vsHierarchy;
        }

        IVsSolution  VsSolution1 { get { return _solutionService.VsSolution1; } }
        IVsSolution4 VsSolution4 { get { return _solutionService.VsSolution4; } }

        public void UnloadProject() {
            // TODO Error Logging
            Guid projectGuid = GetProjectGuid();
            ErrorHandler.ThrowOnFailure(VsSolution4.UnloadProject(ref projectGuid, (uint)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser));
        }

        public void LoadProject() {
            // TODO Error Logging
            Guid projectGuid = GetProjectGuid();
            ErrorHandler.ThrowOnFailure(VsSolution4.ReloadProject(ref projectGuid));
        }

        public void CloseProject() {
            // TODO Error Logging
            int res;
            if (ErrorHandler.Failed(res = VsSolution1.CloseSolutionElement((uint) __VSSLNCLOSEOPTIONS.SLNCLOSEOPT_SLNSAVEOPT_MASK| (uint)__VSSLNCLOSEOPTIONS.SLNCLOSEOPT_DeleteProject, _vsHierarchy, 0))) {
                // TODO Error Logging
                Debug.WriteLine($"IVsolution::CloseProject retuend 0x{res:X}.");
                ErrorHandler.ThrowOnFailure(res);
            }
        }

        public Guid GetProjectGuid() {
            int res;
            Guid projGuid;

            if (ErrorHandler.Failed(res = VsSolution1.GetGuidOfProject(_vsHierarchy, out projGuid))) {
                // TODO Error Logging
                Debug.WriteLine($"IVsolution::GetProjectGuid retuend 0x{res:X}.");
            }

            return projGuid;
        }

        public string GetUniqueNameOfProject() {
            int res;
            string uniqueName;
            if (ErrorHandler.Failed(res = VsSolution1.GetUniqueNameOfProject(_vsHierarchy, out uniqueName))) {
                // TODO Error Logging
                Debug.WriteLine($"IVsolution::GetUniqueNameOfProject retuend 0x{res:X}.");
            }

            return uniqueName;
        }

        public bool IsProjectUnloaded() {
            // TODO Error Logging
            object status;
            var hr = _vsHierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID5.VSHPROPID_ProjectUnloadStatus, out status);

            return ErrorHandler.Succeeded(hr);
        }
    }
}