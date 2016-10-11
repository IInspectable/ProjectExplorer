#region Using Directives

using System;
using System.Collections;
using System.Collections.Generic;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed class ProjectItemComparer: IComparer<ProjectViewModel>, IComparer {

        public int Compare(ProjectViewModel x, ProjectViewModel y) {

            if (x == null && y != null) {
                return -1;
            }

            if (x != null && y == null) {
                return 1;
            }
            if (x == null) {
                return 0;
            }

            var statusCmp= y.Status - x.Status;
            if (statusCmp != 0) {
                return statusCmp;
            }

            return String.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
        }

        public int Compare(object x, object y) {
            return Compare(x as ProjectViewModel, y as ProjectViewModel);
        }
    }
}