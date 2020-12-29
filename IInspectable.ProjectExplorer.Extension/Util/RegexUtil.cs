#region Using Directives

using System;
using System.Text.RegularExpressions;

using JetBrains.Annotations;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    public static class RegexUtil {

        [CanBeNull]
        public static Regex BuildSearchPattern([CanBeNull] string searchString, bool matchCase, bool useRegularExpressions) {

            if (String.IsNullOrWhiteSpace(searchString)) {
                return null;
            }

            var regexOptions = RegexOptions.Singleline;
            if (!matchCase) {
                regexOptions |= RegexOptions.IgnoreCase;
            }

            string pattern = searchString;
            if (!useRegularExpressions) {

                pattern = Regex.Escape(pattern)
                               .Replace("\\*", ".*")
                               .Replace("\\?", ".");
            }

            var searchPattern = SafeGetRegex(pattern, regexOptions);

            return searchPattern;
        }

        [CanBeNull]
        static Regex SafeGetRegex(string pattern, RegexOptions regexOptions) {
            pattern ??= String.Empty;
            try {
                return new Regex(pattern, regexOptions);
            } catch (ArgumentException) {
                try {
                    return new Regex(Regex.Escape(pattern), regexOptions);
                } catch (ArgumentException) {
                    return null;
                }
            }
        }

    }

}