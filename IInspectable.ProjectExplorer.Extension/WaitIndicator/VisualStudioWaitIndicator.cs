#region Using Directives

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    [Export(typeof(IWaitIndicator))]
    sealed class VisualStudioWaitIndicator : IWaitIndicator {

        readonly SVsServiceProvider _serviceProvider;

        [ImportingConstructor]
        public VisualStudioWaitIndicator(SVsServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;
        }

        public WaitIndicatorResult Wait(string title, string message, bool allowCancel, Action<IWaitContext> action) {
            using(var waitContext = StartWait(title, message, allowCancel)) {
                try {
                    action(waitContext);

                    return WaitIndicatorResult.Completed;
                } catch(OperationCanceledException) {
                    return WaitIndicatorResult.Canceled;
                } catch(AggregateException e) {
                    var operationCanceledException = e.InnerExceptions[0] as OperationCanceledException;
                    if(operationCanceledException != null) {
                        return WaitIndicatorResult.Canceled;
                    } else {
                        throw;
                    }
                }
            }
        }

        VisualStudioWaitContext StartWait(string title, string message, bool allowCancel) {

            var dialogFactory = (IVsThreadedWaitDialogFactory) _serviceProvider.GetService(typeof(SVsThreadedWaitDialogFactory));
            //Contract.ThrowIfNull(dialogFactory);

            return new VisualStudioWaitContext(dialogFactory, title, message, allowCancel);
        }

        IWaitContext IWaitIndicator.StartWait(string title, string message, bool allowCancel) {
            return StartWait(title, message, allowCancel);
        }
    }
}