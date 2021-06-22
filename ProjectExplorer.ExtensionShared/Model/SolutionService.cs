#region Using Directives

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using JetBrains.Annotations;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
// ReSharper disable ConvertToAutoProperty

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    static class ServiceProviderExtensions {

        public static TInterface GetService<TService, TInterface>(this IServiceProvider sp) where TInterface : class {
            return sp.GetService(typeof(TService)) as TInterface;
        }

    }

    [Export]
    class SolutionService: IVsSolutionEvents, IVsSolutionEvents4, IDisposable {

        readonly        IVsSolution      _vsSolution1;
        readonly        IVsSolution2     _vsSolution2;
        readonly        IVsSolution4     _vsSolution4;
        readonly        IVsImageService2 _vsImageService2;
        static readonly Logger           Logger = Logger.Create<SolutionService>();

        uint _solutionEvents1Cookie;
        uint _solutionEvents4Cookie;

        [ImportingConstructor]
        public SolutionService([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider) {

            ThreadHelper.ThrowIfNotOnUIThread();

            _vsSolution1 = serviceProvider.GetService<SVsSolution, IVsSolution>();
            _vsSolution2 = serviceProvider.GetService<SVsSolution, IVsSolution2>();
            _vsSolution4 = serviceProvider.GetService<SVsSolution, IVsSolution4>();

            _vsImageService2 = serviceProvider.GetService<SVsImageService, IVsImageService2>();

            _vsSolution1.AdviseSolutionEvents(this, out _solutionEvents1Cookie);
            _vsSolution1.AdviseSolutionEvents(this, out _solutionEvents4Cookie);
        }

        public void Dispose() {

            ThreadHelper.ThrowIfNotOnUIThread();

            if (_solutionEvents1Cookie != 0) {
                _vsSolution1.UnadviseSolutionEvents(_solutionEvents1Cookie);
                _solutionEvents1Cookie = 0;
            }

            if (_solutionEvents4Cookie != 0) {
                _vsSolution1.UnadviseSolutionEvents(_solutionEvents4Cookie);
                _solutionEvents4Cookie = 0;
            }
        }

        public IVsSolution  VsSolution1 => _vsSolution1;
        public IVsSolution2 VsSolution2 => _vsSolution2;
        public IVsSolution4 VsSolution4 => _vsSolution4;

        public Hierarchy GetRoot() {
            ThreadHelper.ThrowIfNotOnUIThread();
            // ReSharper disable once SuspiciousTypeConversion.Global
            var hierarchy = _vsSolution1 as IVsHierarchy;
            var root      = new Hierarchy(this, hierarchy, HierarchyId.Root);
            return root;
        }

        public ImageMoniker GetImageMonikerForFile(string filename) {
            ThreadHelper.ThrowIfNotOnUIThread();
            return _vsImageService2.GetImageMonikerForFile(filename);
        }

        public ImageMoniker GetImageMonikerForHierarchyItem(IVsHierarchy hierarchy) {
            ThreadHelper.ThrowIfNotOnUIThread();
            return _vsImageService2.GetImageMonikerForHierarchyItem(hierarchy, (uint) VSConstants.VSITEMID.Root, (int) __VSHIERARCHYIMAGEASPECT.HIA_Icon);
        }

        public HResult EnsureSolution() {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (IsSolutionOpen()) {
                return VSConstants.S_OK;
            }

            return LogFailed(_vsSolution1.CreateSolution(
                                 lpszLocation: null,
                                 lpszName: null,
                                 grfCreateFlags: (uint) __VSCREATESOLUTIONFLAGS.CSF_TEMPORARY));
        }

        public int OpenProject(string path) {

            ThreadHelper.ThrowIfNotOnUIThread();

            Guid empty  = Guid.Empty;
            Guid projId = Guid.Empty;

            return LogFailed(_vsSolution1.CreateProject(
                                 rguidProjectType: ref empty,
                                 lpszMoniker: path,
                                 lpszLocation: null,
                                 lpszName: null,
                                 grfCreateFlags: (uint) __VSCREATEPROJFLAGS.CPF_OPENFILE,
                                 iidProject: ref projId,
                                 ppProject: out IntPtr _));
        }

        public Guid GetProjectGuid(IVsHierarchy pHierarchy) {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (pHierarchy == null) {
                return Guid.Empty;
            }

            LogFailed(_vsSolution1.GetGuidOfProject(pHierarchy, out var projGuid));
            return projGuid;
        }

        public bool IsSolutionOpen() {
            ThreadHelper.ThrowIfNotOnUIThread();

            LogFailed(_vsSolution1.GetProperty((int) __VSPROPID.VSPROPID_IsSolutionOpen, out var value));
            return (bool) value;
        }

        [CanBeNull]
        public string GetSolutionDirectory() {
            ThreadHelper.ThrowIfNotOnUIThread();

            _vsSolution1.GetSolutionInfo(out string solutionDirectory, out string _, out string _);

            return solutionDirectory;
        }

        [CanBeNull]
        public string GetSolutionFile() {
            ThreadHelper.ThrowIfNotOnUIThread();

            _vsSolution1.GetSolutionInfo(out string _, out string solutionFile, out string _);

            return solutionFile;
        }

        public Hierarchy GetHierarchyByUniqueNameOfProject(string uniqueName) {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (Failed(_vsSolution1.GetProjectOfUniqueName(uniqueName, out var result), except: VSConstants.E_FAIL) || result == null) {
                return null;
            }

            return new Hierarchy(this, result, HierarchyId.Root);
        }

        public IEnumerable<Hierarchy> EnumerateHierarchy() {
            ThreadHelper.ThrowIfNotOnUIThread();

            Guid ignored = Guid.Empty;
            var  flags   =  __VSENUMPROJFLAGS.EPF_ALLPROJECTS;
            if (Failed(_vsSolution1.GetProjectEnum((uint) flags, ref ignored, out var hierEnum))) {
                yield break;
            }

            IVsHierarchy[] hier = new IVsHierarchy[1];
            while ((hierEnum.Next((uint) hier.Length, hier, out var fetched) == VSConstants.S_OK) && (fetched == hier.Length)) {

                var hierarchy = new Hierarchy(this, hier[0], HierarchyId.Root);

                yield return hierarchy;
            }
        }

        #region IVsSolutionEvents

        public event EventHandler<ProjectEventArgs> AfterOpenProject;

        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) {

            var realHierarchy = new Hierarchy(this, pHierarchy, HierarchyId.Root);
            AfterOpenProject?.Invoke(this, new ProjectEventArgs(realHierarchy));

            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) {
            return VSConstants.S_OK;
        }

        public event EventHandler<ProjectEventArgs> BeforeRemoveProject;

        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) {

            if (fRemoved == 1) {
                var realHierarchy = new Hierarchy(this, pHierarchy, HierarchyId.Root);
                BeforeRemoveProject?.Invoke(this, new ProjectEventArgs(realHierarchy));
            }

            return VSConstants.S_OK;
        }

        public event EventHandler<ProjectEventArgs> AfterLoadProject;

        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) {

            var realHierarchy = new Hierarchy(this, pStubHierarchy, HierarchyId.Root);
            var stubHierarchy = new Hierarchy(this, pRealHierarchy, HierarchyId.Root);
            AfterLoadProject?.Invoke(this, new ProjectEventArgs(realHierarchy, stubHierarchy));

            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) {
            return VSConstants.S_OK;
        }

        public event EventHandler<ProjectEventArgs> BeforeUnloadProject;

        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) {
            var realHierarchy = new Hierarchy(this, pStubHierarchy, HierarchyId.Root);
            var stubHierarchy = new Hierarchy(this, pRealHierarchy, HierarchyId.Root);
            BeforeUnloadProject?.Invoke(this, new ProjectEventArgs(realHierarchy, stubHierarchy));
            return VSConstants.S_OK;
        }

        public event EventHandler AfterOpenSolution;

        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution) {
            AfterOpenSolution?.Invoke(this, EventArgs.Empty);
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved) {
            return VSConstants.S_OK;
        }

        public event EventHandler AfterCloseSolution;

        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved) {
            AfterCloseSolution?.Invoke(this, EventArgs.Empty);
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

        bool Failed(int hr, int except = VSConstants.S_OK, [CallerMemberName] string callerMemberName = null) {
            // ReSharper disable once ExplicitCallerInfoArgument Ist hier gew�nscht
            return ErrorHandler.Failed(LogFailed(hr, except, callerMemberName));
        }

        int LogFailed(int hr, int except = VSConstants.S_OK, [CallerMemberName] string callerMemberName = null) {
            hr = hr == except ? VSConstants.S_OK : hr;
            if (ErrorHandler.Failed(hr)) {
                var ex = Marshal.GetExceptionForHR(hr);
                Logger.Error($"{callerMemberName} failed with code 0x{hr:X}: '{ex.Message}'");
            }

            return hr;
        }

    }

}