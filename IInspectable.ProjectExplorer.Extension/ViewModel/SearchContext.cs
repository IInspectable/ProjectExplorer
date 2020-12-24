#region Using Directives

using System;
using System.Text.RegularExpressions;

using JetBrains.Annotations;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed class SearchContext {

        public SearchContext(): this(null) {

        }

        public SearchContext(string searchString) {
            SearchString = searchString ?? String.Empty;

            if (!String.IsNullOrWhiteSpace(searchString)) {

                Regex = RegexUtil.BuildSearchPattern(
                    searchString,
                    matchCase: false,
                    useRegularExpressions: false);
            }
        }

        [NotNull]
        public string SearchString { get; }

        [CanBeNull]
        public Regex Regex { get; }

        public bool IsMatch(string input) {
            if (input == null) {
                return false;
            }

            return Regex == null || Regex.IsMatch(input);
        }

    }

}