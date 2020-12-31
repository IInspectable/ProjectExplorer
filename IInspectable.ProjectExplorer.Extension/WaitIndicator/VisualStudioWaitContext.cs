#region Using Directives

using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed partial class VisualStudioWaitContext: IWaitContext {

        const int DelayToShowDialogSecs = 1;

        readonly IVsThreadedWaitDialog3  _dialog;
        readonly CancellationTokenSource _cancellationTokenSource;
        readonly string                  _title;

        string _message;
        bool   _allowCancel;

        public VisualStudioWaitContext(IVsThreadedWaitDialogFactory dialogFactory,
                                       string title,
                                       string message,
                                       bool allowCancel) {
            ThreadHelper.ThrowIfNotOnUIThread();

            _title                   = title;
            _message                 = message;
            _allowCancel             = allowCancel;
            _cancellationTokenSource = new CancellationTokenSource();
            _dialog                  = CreateDialog(dialogFactory);
        }

        IVsThreadedWaitDialog3 CreateDialog(IVsThreadedWaitDialogFactory dialogFactory) {
            ThreadHelper.ThrowIfNotOnUIThread();

            Marshal.ThrowExceptionForHR(dialogFactory.CreateInstance(out var dialog2));

            var dialog3 = (IVsThreadedWaitDialog3) dialog2;

            var callback = new Callback(this);

            dialog3.StartWaitDialogWithCallback(
                szWaitCaption: _title,
                szWaitMessage: _message,
                szProgressText: null,
                varStatusBmpAnim: null,
                szStatusBarText: null,
                fIsCancelable: _allowCancel,
                iDelayToShowDialog: DelayToShowDialogSecs,
                fShowProgress: false,
                iTotalSteps: 0,
                iCurrentStep: 0,
                pCallback: callback);

            return dialog3;
        }

        public CancellationToken CancellationToken => _allowCancel ? _cancellationTokenSource.Token : CancellationToken.None;

        public string Message {
            get => _message;
            set {
                ThreadHelper.ThrowIfNotOnUIThread();
                _message = value;
                UpdateDialog();
            }
        }

        public bool AllowCancel {
            get => _allowCancel;
            set {
                ThreadHelper.ThrowIfNotOnUIThread();
                _allowCancel = value;
                UpdateDialog();
            }
        }

        void UpdateDialog() {
            ThreadHelper.ThrowIfNotOnUIThread();

            _dialog.UpdateProgress(
                szUpdatedWaitMessage: _message,
                szProgressText      : null,
                szStatusBarText     : null,
                iCurrentStep        : 0,
                iTotalSteps         : 0,
                fDisableCancel      : !_allowCancel,
                pfCanceled          : out _);
        }

        public void UpdateProgress() {
        }

        public void Dispose() {
            ThreadHelper.ThrowIfNotOnUIThread();
            _dialog.EndWaitDialog(out _);
        }

        void OnCanceled() {
            if (_allowCancel) {
                _cancellationTokenSource.Cancel();
            }
        }

    }

}