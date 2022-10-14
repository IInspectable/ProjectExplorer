using System;
using System.Threading;

namespace IInspectable.ProjectExplorer.Extension; 

enum WaitIndicatorResult {

    Completed,
    Canceled,

}

interface IWaitContext : IDisposable {

    CancellationToken CancellationToken { get; }

    bool   AllowCancel { get; set; }
    string Message     { get; set; }

    void UpdateProgress();

}