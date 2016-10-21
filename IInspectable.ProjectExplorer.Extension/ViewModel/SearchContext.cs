#region Using Directives

using System;
using System.Text.RegularExpressions;

using JetBrains.Annotations;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    sealed class SearchContext {

        public SearchContext() :this(null){
            
        }

        public SearchContext(string searchString) {
            SearchString = searchString??String.Empty;

            if (!String.IsNullOrWhiteSpace(searchString)) {

                var regexString = WildcardToRegex(searchString);
            
                Regex = new Regex(regexString, RegexOptions.IgnoreCase);
            }
        }

        [NotNull]
        public string SearchString { get; }

        [CanBeNull]
        public Regex Regex { get; }

        public bool IsMatch(string input) {
            if(input == null) {
                return false;
            }
            return Regex == null || Regex.IsMatch(input);
        }

        static string WildcardToRegex(string searchString) {

            if (!searchString.StartsWith("*")) {
                searchString = "*" + searchString;
            }
            if (!searchString.EndsWith("*")) {
                searchString += "*";
            }

            searchString = "^" + Regex.Escape(searchString)
                               .Replace("\\*", ".*")
                               .Replace("\\?", ".") +
                           "$";
            return searchString;
        }
    }
}