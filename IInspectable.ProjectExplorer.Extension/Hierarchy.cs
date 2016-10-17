#region Using Directives

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using JetBrains.Annotations;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    class Hierarchy {

        static readonly Logger Logger = Logger.Create<Hierarchy>();
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

        public ImageMoniker GetImageMoniker() {
            return _solutionService.GetImageMonikerForHierarchyItem(_vsHierarchy);
        }

        public int UnloadProject() {

            Guid projectGuid = GetProjectGuid();
            return LogFailed(VsSolution4.UnloadProject(ref projectGuid, (uint) _VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser));
        }

        public int ReloadProject() {

            Guid projectGuid = GetProjectGuid();
            return LogFailed(VsSolution4.ReloadProject(ref projectGuid));
        }

        public int CloseProject() {

            return LogFailed(VsSolution1.CloseSolutionElement(
                grfCloseOpts: (uint) __VSSLNCLOSEOPTIONS.SLNCLOSEOPT_SLNSAVEOPT_MASK | (uint) __VSSLNCLOSEOPTIONS.SLNCLOSEOPT_DeleteProject, 
                pHier       : _vsHierarchy, 
                docCookie   : 0));
        }

        [CanBeNull]
        public string GetFullPath() {
            var solutionDir = _solutionService.GetSolutionDirectory();
            if(solutionDir == null) {
                return null;
            }
            var fullPath=Path.Combine(solutionDir, GetUniqueNameOfProject());
            return fullPath;
        }

        public Guid GetProjectGuid() {
            Guid projGuid;
            LogFailed(VsSolution1.GetGuidOfProject(_vsHierarchy, out projGuid));
            return projGuid;
        }

        public string GetUniqueNameOfProject() {
            string uniqueName;
            LogFailed(VsSolution1.GetUniqueNameOfProject(_vsHierarchy, out uniqueName));
            return uniqueName?.ToLower();
        }

        public bool IsProjectUnloaded() {

            object status;
            var hr = _vsHierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID5.VSHPROPID_ProjectUnloadStatus, out status);

            return ErrorHandler.Succeeded(hr);
        }

        public ProjectStatus GetStatus() {
            string uniqueName;
            if (ErrorHandler.Failed(VsSolution1.GetUniqueNameOfProject(_vsHierarchy, out uniqueName))) {
                return ProjectStatus.Closed;
            }

            if (IsProjectUnloaded()) {
                return ProjectStatus.Unloaded;
            }
            return ProjectStatus.Loaded;
        }

        public uint AdviseHierarchyEvents(IVsHierarchyEvents eventSink) {
            uint cookie;
            LogFailed(_vsHierarchy.AdviseHierarchyEvents(eventSink, out cookie));           
            return cookie;
        }

        public int UnadviseHierarchyEvents(uint cookie) {
            return LogFailed(_vsHierarchy.UnadviseHierarchyEvents(cookie));            
        }

        int LogFailed(int hr, [CallerMemberName] string callerMemberName = null) {
            if(ErrorHandler.Failed(hr)) {
                var ex = Marshal.GetExceptionForHR(hr);
                Logger.Error($"{callerMemberName} failed with code 0x{hr:X}: '{ex.Message}'");
            }
            return hr;
        }
    }
}