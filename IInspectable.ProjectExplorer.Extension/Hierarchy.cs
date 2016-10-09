#region Using Directives

using System;
using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    class Hierarchy {

        readonly ProjectService _projectService;
        readonly IVsHierarchy _vsHierarchy;

        public Hierarchy(ProjectService projectService, IVsHierarchy vsHierarchy) {

            if(projectService == null) {
                throw new ArgumentNullException(nameof(projectService));
            }

            if (vsHierarchy == null) {
                throw new ArgumentNullException(nameof(vsHierarchy));
            }

            _projectService = projectService;
            _vsHierarchy    = vsHierarchy;
        }

        public IVsSolution VsSolution { get { return _projectService.Solution1; } }
        public IVsSolution2 VsSolution2 { get { return _projectService.Solution2; } }
        public IVsSolution4 VsSolution4 { get { return _projectService.Solution4; } }

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
            VsSolution.CloseSolutionElement((uint)__VSSLNCLOSEOPTIONS.SLNCLOSEOPT_DeleteProject, _vsHierarchy, 0);
        }

        public Guid GetProjectGuid() {
            int res;
            Guid projGuid;

            if (ErrorHandler.Failed(res = VsSolution.GetGuidOfProject(_vsHierarchy, out projGuid))) {
                // TODO Error Logging
                Debug.WriteLine($"IVsolution::GetGuidOfProject retuend 0x{res:X}.");
            }

            return projGuid;
        }

        public string GetUniqueNameOfProject() {
            int res;
            string uniqueName;
            if (ErrorHandler.Failed(res = VsSolution.GetUniqueNameOfProject(_vsHierarchy, out uniqueName))) {
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