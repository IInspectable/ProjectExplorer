using System;
using JetBrains.Annotations;

namespace IInspectable.ProjectExplorer.Extension {

    class ProjectEventArgs : EventArgs {

        public ProjectEventArgs(Hierarchy realHierarchie, Hierarchy stubHierarchie=null) {

            RealHierarchie = realHierarchie ?? throw new ArgumentNullException(nameof(realHierarchie));
            StubHierarchie = stubHierarchie;
        }

        public Hierarchy RealHierarchie { get; }

        [CanBeNull]
        public Hierarchy StubHierarchie { get; }
    }
}