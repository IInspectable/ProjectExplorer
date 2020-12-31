using System.ComponentModel.Composition;
using System.Globalization;

using Microsoft.VisualStudio.Text.PatternMatching;

namespace IInspectable.ProjectExplorer.Extension {

    [Export]
    sealed class SearchContextFactory {

        readonly IPatternMatcherFactory _patternMatcherFactory;

        [ImportingConstructor]
        public SearchContextFactory(IPatternMatcherFactory patternMatcherFactory) {
            _patternMatcherFactory = patternMatcherFactory;
        }

        public SearchContext Create(string searchString) {
            return new(searchString, _patternMatcherFactory);
        }

    }

}