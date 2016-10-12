#region Using Directives

using System;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;

using IInspectable.Utilities.Logging;

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
            int hr = VsSolution4.UnloadProject(ref projectGuid, (uint) _VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser);

            if(ErrorHandler.Failed(hr)) {
                // TODO Error Logging
                Logger.Warn($"IVsolution::UnloadProject returned 0x{hr:X}.");
            }
            return hr;
        }

        public int LoadProject() {

            Guid projectGuid = GetProjectGuid();
            int hr = VsSolution4.ReloadProject(ref projectGuid);

            if(ErrorHandler.Failed(hr)) {
                // TODO Error Logging
                // TODO wirklich per guid laden, und nicht per uniqueName?
                Logger.Warn($"IVsolution::LoadProject with guid {projectGuid} returned 0x{hr:X}.");
            }
            return hr;
        }

        public int CloseProject() {

            int hr = VsSolution1.CloseSolutionElement(grfCloseOpts: (uint) __VSSLNCLOSEOPTIONS.SLNCLOSEOPT_SLNSAVEOPT_MASK | (uint) __VSSLNCLOSEOPTIONS.SLNCLOSEOPT_DeleteProject, 
                                                      pHier: _vsHierarchy, 
                                                      docCookie: 0);

            if (ErrorHandler.Failed(hr)) {
                // TODO Error Logging
                Logger.Warn($"IVsolution::CloseProject returned 0x{hr:X}.");
            }

            return hr;
        }

        public Guid GetProjectGuid() {

            Guid projGuid;
            int hr = VsSolution1.GetGuidOfProject(_vsHierarchy, out projGuid);

            if (ErrorHandler.Failed(hr)) {
                // TODO Error Logging
                Logger.Warn($"IVsolution::GetProjectGuid returned 0x{hr:X}.");
            }

            return projGuid;
        }

        public string GetUniqueNameOfProject() {
            string uniqueName;
            int hr = VsSolution1.GetUniqueNameOfProject(_vsHierarchy, out uniqueName);
            if (ErrorHandler.Failed(hr)) {
                // TODO Error Logging
                Logger.Warn($"IVsolution::GetUniqueNameOfProject returned 0x{hr:X}.");
            }

            return uniqueName.ToLower();
        }

        public bool IsProjectUnloaded() {
            object status;
            var hr = _vsHierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID5.VSHPROPID_ProjectUnloadStatus, out status);

            return ErrorHandler.Succeeded(hr);
        }

        public uint AdviseHierarchyEvents(IVsHierarchyEvents eventSink) {
            uint cookie;
            var hr = _vsHierarchy.AdviseHierarchyEvents(eventSink, out cookie);
            if(ErrorHandler.Failed(hr)) {
                // TODO Error Logging
                Logger.Warn($"IVsolution::AdviseHierarchyEvents returned 0x{hr:X}.");
            }
            return cookie;
        }

        public void UnadviseHierarchyEvents(uint cookie) {
            var hr = _vsHierarchy.UnadviseHierarchyEvents(cookie);
            if(ErrorHandler.Failed(hr)) {
                // TODO Error Logging
                Logger.Warn($"IVsolution::UnadviseHierarchyEvents returned 0x{hr:X}.");
            }
        }
    }
}