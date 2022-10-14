using Microsoft.VisualStudio.Shell.Interop;

namespace IInspectable.ProjectExplorer.Extension; 

partial class VisualStudioWaitContext {

    /// <summary>
    /// Note: this is a COM interface, however it is also free threaded.  This is necessary and
    /// by design so that we can hear about cancellation happening from the wait dialog (which
    /// will happen on the background).
    /// </summary>
    class Callback : IVsThreadedWaitDialogCallback {

        readonly VisualStudioWaitContext _waitContext;

        public Callback(VisualStudioWaitContext waitContext) {
            _waitContext = waitContext;
        }

        public void OnCanceled() {
            _waitContext.OnCanceled();
        }
    }
}