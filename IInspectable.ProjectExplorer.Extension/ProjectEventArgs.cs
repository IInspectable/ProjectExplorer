using System;
using JetBrains.Annotations;
using Microsoft.VisualStudio.Shell.Interop;

namespace IInspectable.ProjectExplorer.Extension {

    public class ProjectEventArgs : EventArgs {

        public ProjectEventArgs(IVsHierarchy realHierarchie, IVsHierarchy stubHierarchie=null) {
            RealHierarchie = realHierarchie;
            StubHierarchie = stubHierarchie;
        }

        public IVsHierarchy RealHierarchie { get; }

        [CanBeNull]
        public IVsHierarchy StubHierarchie { get; }
    }
}