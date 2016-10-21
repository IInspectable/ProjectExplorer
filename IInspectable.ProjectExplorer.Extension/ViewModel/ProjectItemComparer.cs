#region Using Directives

using System;
using System.Collections;
using System.Collections.Generic;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed class ProjectItemComparer: IComparer<ProjectItemViewModel>, IComparer {

        public int Compare(ProjectItemViewModel x, ProjectItemViewModel y) {

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

            return String.Compare(x.DisplayName, y.DisplayName, StringComparison.OrdinalIgnoreCase);
        }

        public int Compare(object x, object y) {
            return Compare(x as ProjectItemViewModel, y as ProjectItemViewModel);
        }
    }
}