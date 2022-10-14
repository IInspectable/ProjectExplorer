#region Using Directives

using System;
using System.Globalization;

using JetBrains.Annotations;

using Microsoft.VisualStudio.Text.PatternMatching;

#endregion

namespace IInspectable.ProjectExplorer.Extension; 

sealed class SearchContext {

    private readonly IPatternMatcher _patternMatcher;

    public SearchContext(string searchString, IPatternMatcherFactory patternMatcherFactory) {
        SearchString = searchString ?? String.Empty;

        if (SearchString.Length > 0) {

            _patternMatcher = patternMatcherFactory.CreatePatternMatcher(
                searchString,
                new PatternMatcherCreationOptions(
                    cultureInfo: CultureInfo.CurrentCulture,
                    flags: PatternMatcherCreationFlags.IncludeMatchedSpans));

        }
    }

    [NotNull]
    public string SearchString { get; }

    public bool IsMatch(string input, out PatternMatch? patternMatch) {

        patternMatch = _patternMatcher?.TryMatch(input);
        return _patternMatcher == null || patternMatch.HasValue;
    }

}