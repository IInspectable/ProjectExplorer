#region Using Directives

using System;
using System.Collections;
using System.Collections.Generic;

#endregion

namespace IInspectable.ProjectExplorer.Extension; 

sealed class ProjectViewModelComparer: IComparer<ProjectViewModel>, IComparer {

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

        // Wenn der Benutzer einen Suchstring eingegeben hat,
        // dann die Einträge in der Reihenfolge ihrer Trefferquote anzeigen.
        if (x.PatternMatch.HasValue && y.PatternMatch.HasValue) {
            var patternCmp = x.PatternMatch.Value.CompareTo(y.PatternMatch.Value);
            if (patternCmp != 0) {
                return patternCmp;
            }

            return String.Compare(x.DisplayName, y.DisplayName, StringComparison.OrdinalIgnoreCase);
        }

        var statusCmp = y.Status - x.Status;
        if (statusCmp != 0) {
            return statusCmp;
        }

        return String.Compare(x.DisplayName, y.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    public int Compare(object x, object y) {
        return Compare(x as ProjectViewModel, y as ProjectViewModel);
    }

}