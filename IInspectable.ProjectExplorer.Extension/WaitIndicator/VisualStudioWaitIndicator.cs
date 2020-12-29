#region Using Directives

using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    [Export(typeof(IWaitIndicator))]
    sealed class VisualStudioWaitIndicator: IWaitIndicator {

        readonly SVsServiceProvider _serviceProvider;

        [ImportingConstructor]
        public VisualStudioWaitIndicator(SVsServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;
        }

        public WaitIndicatorResult Wait(string title, string message, bool allowCancel, Action<IWaitContext> action) {
            using var waitContext = StartWait(title, message, allowCancel);
            try {
                action(waitContext);

                return WaitIndicatorResult.Completed;
            } catch (OperationCanceledException) {
                return WaitIndicatorResult.Canceled;
            } catch (AggregateException e) {
                if (e.InnerExceptions[0] is OperationCanceledException) {
                    return WaitIndicatorResult.Canceled;
                }

                throw;
            }

        }

        VisualStudioWaitContext StartWait(string title, string message, bool allowCancel) {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dialogFactory = (IVsThreadedWaitDialogFactory) _serviceProvider.GetService(typeof(SVsThreadedWaitDialogFactory));

            return new VisualStudioWaitContext(dialogFactory, title, message, allowCancel);
        }

        IWaitContext IWaitIndicator.StartWait(string title, string message, bool allowCancel) {
            return StartWait(title, message, allowCancel);
        }

    }

}