#region Using Directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;

using JetBrains.Annotations;

using Microsoft.VisualStudio.Shell;

#endregion

namespace IInspectable.ProjectExplorer.Extension; 

class Hierarchy {

    static readonly Logger Logger = Logger.Create<Hierarchy>();

    readonly SolutionService _solutionService;
    readonly IVsHierarchy    _vsHierarchy;
    readonly HierarchyId     _itemId;

    public Hierarchy(SolutionService solutionService, IVsHierarchy vsHierarchy, HierarchyId itemId) {

        _solutionService = solutionService ?? throw new ArgumentNullException(nameof(solutionService));
        _vsHierarchy     = vsHierarchy     ?? throw new ArgumentNullException(nameof(vsHierarchy));
        _itemId          = itemId;
    }

    public HierarchyId ItemId => _itemId;
    public string      Name   => GetProperty<string>(__VSHPROPID.VSHPROPID_Name);

    IVsSolution  VsSolution1 => _solutionService.VsSolution1;
    IVsSolution4 VsSolution4 => _solutionService.VsSolution4;

    public string CanonicalName {
        get {
            ThreadHelper.ThrowIfNotOnUIThread();
            LogFailed(_vsHierarchy.GetCanonicalName(ItemId, out var cn));
            return cn;
        }
    }

    public Guid ProjectGuid {
        get {
            ThreadHelper.ThrowIfNotOnUIThread();
            return _solutionService.GetProjectGuid(_vsHierarchy);
        }
    }

    #region Structural Properties

    public HierarchyId ParentItemId => GetProperty<int>(__VSHPROPID.VSHPROPID_Parent, HierarchyId.Nil);

    public Hierarchy Parent => new(_solutionService, _vsHierarchy, ParentItemId);

    public HierarchyId ParentHierarchyItemId {
        get {
            var id = GetProperty<object>(__VSHPROPID.VSHPROPID_ParentHierarchyItemid);

            switch (id) {
                case int i:
                    return (uint) i;
                case uint u:
                    return u;
            }

            return HierarchyId.Nil;
        }
    }

    public Hierarchy ParentHierarchy {
        get {
            var parentHierarchy = GetProperty<IVsHierarchy>(__VSHPROPID.VSHPROPID_ParentHierarchy);

            return parentHierarchy == null
                ? null
                : new Hierarchy(_solutionService, parentHierarchy, VSConstants.VSITEMID_ROOT);
        }
    }

    public bool IsNestedHierachy => ItemId.IsRoot && !ParentHierarchyItemId.IsNil;

    public HierarchyId FirstChildItemId => GetProperty<int>(__VSHPROPID.VSHPROPID_FirstChild, HierarchyId.Nil);

    [CanBeNull]
    public Hierarchy FirstChild => WithId(FirstChildItemId);

    public HierarchyId FirstVisibleChildItemId => GetProperty<int>(__VSHPROPID.VSHPROPID_FirstVisibleChild, HierarchyId.Nil);

    [CanBeNull]
    public Hierarchy FirstVisibleChild => WithId(FirstVisibleChildItemId);

    public HierarchyId NextSiblingItemId => GetProperty<int>(__VSHPROPID.VSHPROPID_NextSibling, HierarchyId.Nil);

    [CanBeNull]
    public Hierarchy NextSibling => WithId(NextSiblingItemId);

    public HierarchyId NextVisibleSiblingItemId => GetProperty<int>(__VSHPROPID.VSHPROPID_NextVisibleSibling, HierarchyId.Nil);

    [CanBeNull]
    public Hierarchy NextVisibleSibling => WithId(NextVisibleSiblingItemId);

    public IEnumerable<Hierarchy> Children() {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (GetProperty<bool>(__VSHPROPID.VSHPROPID_HasEnumerationSideEffects)) {
            yield break;
        }

        var firstChild = FirstChild;
        if (firstChild == null) {
            yield break;
        }

        yield return firstChild.GetNestedHierarchy() ?? firstChild;

        var sibling = firstChild.NextSibling;
        while (sibling != null) {
            yield return sibling.GetNestedHierarchy() ?? sibling;

            sibling = sibling.NextSibling;
        }
    }

    public IEnumerable<Hierarchy> VisibleChildren() {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (GetProperty<bool>(__VSHPROPID.VSHPROPID_HasEnumerationSideEffects)) {
            yield break;
        }

        var firstChild = FirstVisibleChild;
        if (firstChild == null) {
            yield break;
        }

        yield return firstChild.GetNestedHierarchy() ?? firstChild;

        var sibling = firstChild.NextVisibleSibling;
        while (sibling != null) {
            yield return sibling.GetNestedHierarchy() ?? sibling;

            sibling = sibling.NextSibling;
        }
    }

    public IEnumerable<Hierarchy> DescendantsAndSelf() {
        ThreadHelper.ThrowIfNotOnUIThread();

        yield return GetNestedHierarchy() ?? this;

        foreach (var descendant in Descendants()) {
            yield return descendant;
        }
    }

    public IEnumerable<Hierarchy> Descendants() {
        ThreadHelper.ThrowIfNotOnUIThread();

        foreach (var child in Children()) {
            foreach (var descendant in child.DescendantsAndSelf()) {
                yield return descendant;
            }
        }
    }

    public IEnumerable<Hierarchy> VisibleDescendantsAndSelf() {
        ThreadHelper.ThrowIfNotOnUIThread();

        yield return GetNestedHierarchy() ?? this;

        foreach (var descendant in VisibleDescendants()) {
            yield return descendant;
        }
    }

    public IEnumerable<Hierarchy> VisibleDescendants() {
        ThreadHelper.ThrowIfNotOnUIThread();

        foreach (var child in VisibleChildren()) {
            foreach (var descendant in child.DescendantsAndSelf()) {
                yield return descendant;
            }
        }
    }

    [CanBeNull]
    Hierarchy WithId(HierarchyId hierarchyId) {
        if (hierarchyId.IsNil) {
            return null;
        }

        return new Hierarchy(_solutionService, _vsHierarchy, hierarchyId);
    }

    #endregion

    public string Caption  => GetProperty<String>(__VSHPROPID.VSHPROPID_Caption);
    public string SaveName => GetProperty<String>(__VSHPROPID.VSHPROPID_SaveName);

    public string FullPath {
        get {
            ThreadHelper.ThrowIfNotOnUIThread();

            var fullPath = GetMkDocument() ?? GetCanonicalName();

            if (!Path.IsPathRooted(fullPath)) {
                return null;
            }

            return fullPath;
        }
    }

    [CanBeNull]
    public Hierarchy GetNestedHierarchy() {

        ThreadHelper.ThrowIfNotOnUIThread();

        var nestedHierarchyGuid = typeof(IVsHierarchy).GUID;
        LogFailed(_vsHierarchy.GetNestedHierarchy(ItemId, ref nestedHierarchyGuid, out IntPtr nestedHiearchyValue, out uint nestedItemId));

        if (nestedHiearchyValue == IntPtr.Zero) {
            return null;
        }

        var nestedHierarchy = Marshal.GetObjectForIUnknown(nestedHiearchyValue) as IVsHierarchy;
        Marshal.Release(nestedHiearchyValue);
        if (nestedHierarchy == null) {
            return null;
        }

        return new Hierarchy(_solutionService, nestedHierarchy, nestedItemId);
    }

    [CanBeNull]
    public string GetMkDocument() {
        ThreadHelper.ThrowIfNotOnUIThread();
        // ReSharper disable once SuspiciousTypeConversion.Global
        var    ao  = _vsHierarchy as IVsProject;
        string doc = null;
        ao?.GetMkDocument(ItemId, out doc);
        return doc;
    }

    [CanBeNull]
    public string GetCanonicalName() {
        ThreadHelper.ThrowIfNotOnUIThread();
        string cn = null;
        LogFailed(_vsHierarchy?.GetCanonicalName(ItemId, out cn) ?? VSConstants.S_OK);
        return cn;
    }

    public override string ToString() {
        return Name;
    }

    public string DumpAll() {
        ThreadHelper.ThrowIfNotOnUIThread();
        return DumpCore(h => h.Children());
    }

    public string DumpVisible() {
        ThreadHelper.ThrowIfNotOnUIThread();
        return DumpCore(h => h.VisibleChildren());
    }

    string DumpCore(Func<Hierarchy, IEnumerable<Hierarchy>> childSelector, int maxLevel = Int32.MaxValue) {
        ThreadHelper.ThrowIfNotOnUIThread();

        var sb = new StringBuilder();
        Dump(this, 0, sb, childSelector, maxLevel);
        return sb.ToString();
    }

    static void Dump(Hierarchy hier, int level, StringBuilder sb, Func<Hierarchy, IEnumerable<Hierarchy>> childSelector, int maxLevel = Int32.MaxValue) {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (level > maxLevel) {
            return;
        }

        var indent = new String(' ', level * 2);
        sb.AppendLine($"{indent}{hier.Name}: '{hier.FullPath}'");
        foreach (var child in childSelector(hier)) {
            Dump(child, level + 1, sb, childSelector, maxLevel);
        }
    }

    public ImageMoniker GetImageMoniker() {
        ThreadHelper.ThrowIfNotOnUIThread();

        return _solutionService.GetImageMonikerForHierarchyItem(_vsHierarchy);
    }

    public int UnloadProject() {
        ThreadHelper.ThrowIfNotOnUIThread();
        Guid projectGuid = GetProjectGuid();
        return LogFailed(VsSolution4.UnloadProject(ref projectGuid, (uint) _VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser));
    }

    public int ReloadProject() {
        ThreadHelper.ThrowIfNotOnUIThread();
        Guid projectGuid = GetProjectGuid();
        return LogFailed(VsSolution4.ReloadProject(ref projectGuid));
    }

    public int CloseProject() {
        ThreadHelper.ThrowIfNotOnUIThread();
        return LogFailed(VsSolution1.CloseSolutionElement(
                             grfCloseOpts: (uint) __VSSLNCLOSEOPTIONS.SLNCLOSEOPT_SLNSAVEOPT_MASK | (uint) __VSSLNCLOSEOPTIONS.SLNCLOSEOPT_DeleteProject,
                             pHier: _vsHierarchy,
                             docCookie: 0));
    }

    public Guid GetProjectGuid() {
        ThreadHelper.ThrowIfNotOnUIThread();
        LogFailed(VsSolution1.GetGuidOfProject(_vsHierarchy, out var projGuid));
        return projGuid;
    }

    public string GetUniqueNameOfProject() {
        ThreadHelper.ThrowIfNotOnUIThread();
        LogFailed(VsSolution1.GetUniqueNameOfProject(_vsHierarchy, out var uniqueName));
        return uniqueName?.ToLower();
    }

    public bool IsProjectUnloaded() {
        ThreadHelper.ThrowIfNotOnUIThread();
        var hr = _vsHierarchy.GetProperty(_itemId, (int) __VSHPROPID5.VSHPROPID_ProjectUnloadStatus, out var _);

        return ErrorHandler.Succeeded(hr);
    }

    public uint AdviseHierarchyEvents(IVsHierarchyEvents eventSink) {
        ThreadHelper.ThrowIfNotOnUIThread();
        LogFailed(_vsHierarchy.AdviseHierarchyEvents(eventSink, out uint cookie));
        return cookie;
    }

    public int UnadviseHierarchyEvents(uint cookie) {
        ThreadHelper.ThrowIfNotOnUIThread();
        return LogFailed(_vsHierarchy.UnadviseHierarchyEvents(cookie));
    }

    protected T GetProperty<T>(__VSHPROPID propId, T defaultValue = default) {
        ThreadHelper.ThrowIfNotOnUIThread();
        var value = GetPropertyCore((int) propId);
        if (value == null) {
            return defaultValue;
        }

        return (T) value;
    }

    protected object GetPropertyCore(int propId) {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (propId == (int) __VSHPROPID.VSHPROPID_NIL) {
            return null;
        }

        LogFailed(_vsHierarchy.GetProperty(ItemId, propId, out object propValue));

        return propValue;
    }

    int LogFailed(int hr, [CallerMemberName] string callerMemberName = null) {
        if (ErrorHandler.Failed(hr)) {
            var ex = Marshal.GetExceptionForHR(hr);
            Logger.Warn($"{callerMemberName} failed with code 0x{hr:X}: '{ex.Message}'");
        }

        return hr;
    }

}