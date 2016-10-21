using System;
using JetBrains.Annotations;

namespace IInspectable.ProjectExplorer.Extension {

    class ProjectEventArgs : EventArgs {

        public ProjectEventArgs(Hierarchy realHierarchie, Hierarchy stubHierarchie=null) {

            if (realHierarchie == null) {
                throw new ArgumentNullException(nameof(realHierarchie));
            }

            RealHierarchie = realHierarchie;
            StubHierarchie = stubHierarchie;
        }

        public Hierarchy RealHierarchie { get; }

        [CanBeNull]
        public Hierarchy StubHierarchie { get; }
    }
}