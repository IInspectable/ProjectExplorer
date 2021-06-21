using System;

using JetBrains.Annotations;

namespace IInspectable.ProjectExplorer.Extension {

    static class StringExtensions {

        public static string NullIfEmpty([CanBeNull] this string value) {
            if (value.IsNullOrEmpty()) {
                return null;
            }

            return value;
        }

        [ContractAnnotation("null=>true")]
        public static bool IsNullOrEmpty([CanBeNull] this string value) {
            return String.IsNullOrEmpty(value);
        }

    }

}