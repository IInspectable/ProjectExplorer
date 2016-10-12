#region Using Directives

using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed class ShellUtil : IDisposable {

        ShellUtil() {
            Shell?.EnableModeless(0);
        }

        public static IDisposable EnterModalState() {
            return new ShellUtil(); 
        }

        static IVsUIShell Shell {
            get { return ProjectExplorerPackage.GetGlobalService<IVsUIShell, IVsUIShell>(); }
        }

        public void Dispose() {
            Shell?.EnableModeless(1);
        }

        public static void ReportUserOnFailed(int? hr) {
            ReportUserOnFailed(hr ?? VSConstants.S_OK);
        }

        public static void ReportUserOnFailed(int hr) {
            if (ErrorHandler.Failed(hr)) {
                Shell.ReportErrorInfo(hr);
            }
        }

        public static bool ConfirmOkCancel(string text) {

            Guid unused=Guid.Empty;
            int result;

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
                pnResult       : out result);

            
            return result == 1;
        }
    }
}