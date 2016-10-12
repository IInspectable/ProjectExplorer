#region Using Directives

using System;
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

        public static void ReportErrorInfo(int hr) {
            Shell.ReportErrorInfo(hr);
        }
    }
}