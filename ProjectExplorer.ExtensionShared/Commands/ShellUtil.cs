#region Using Directives

using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

#endregion

namespace IInspectable.ProjectExplorer.Extension; 

sealed class ShellUtil : IDisposable {

    static readonly Logger Logger = Logger.Create<ShellUtil>();

    ShellUtil() {
        ThreadHelper.ThrowIfNotOnUIThread();
        Shell?.EnableModeless(0);
    }

    public static IDisposable EnterModalState() {
        return new ShellUtil(); 
    }

    static IVsUIShell Shell => ProjectExplorerPackage.GetGlobalService<IVsUIShell, IVsUIShell>();

    public void Dispose() {
        ThreadHelper.ThrowIfNotOnUIThread();
        Shell?.EnableModeless(1);
    }

    public static bool ReportUserOnFailed(int? hr) {
        ThreadHelper.ThrowIfNotOnUIThread();
        return ReportUserOnFailed(hr ?? VSConstants.S_OK);
    }

    public static bool ReportUserOnFailed(int hr) {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (ErrorHandler.Failed(hr)) {

            Logger.Warn($"{nameof(ReportUserOnFailed)}: Error code: {hr}");
            Shell.ReportErrorInfo(hr);
            return true;
        }
        return false;
    }

    public static void UpdateCommandUI(bool immediateUpdate=true) {
        ThreadHelper.ThrowIfNotOnUIThread();
        Shell.UpdateCommandUI(fImmediateUpdate: immediateUpdate?1:0);
    }

    public static bool ConfirmOkCancel(string text) {
        ThreadHelper.ThrowIfNotOnUIThread();

        Guid unused =Guid.Empty;

        Shell.ShowMessageBox(
            dwCompRole     : 0, 
            rclsidComp     : ref unused, 
            pszTitle       : null, 
            pszText        : text, 
            pszHelpFile    : null, 
            dwHelpContextID: 0, 
            msgbtn         : OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL, 
            msgdefbtn      : OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, 
            msgicon        : OLEMSGICON.OLEMSGICON_WARNING, 
            fSysAlert      : 0, 
            pnResult: out var result);

            
        return result == 1;
    }
}