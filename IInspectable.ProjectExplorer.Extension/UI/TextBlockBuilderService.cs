#region Using Directives

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Documents;

using JetBrains.Annotations;

using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    [Export]
    public sealed class TextBlockBuilderService {

        [ImportingConstructor]
        public TextBlockBuilderService() {

        }

        [CanBeNull]
        public TextBlock ToTextBlock(IReadOnlyCollection<string> parts) {
            ThreadHelper.ThrowIfNotOnUIThread();

            return ToTextBlock(parts, null, out _);
        }

        [CanBeNull]
        public TextBlock ToTextBlock(string part, [CanBeNull] Regex searchPattern, out bool hasMatch) {
            return ToTextBlock(new[] {part}, searchPattern, out hasMatch);
        }

        [CanBeNull]
        public TextBlock ToTextBlock(IReadOnlyCollection<string> parts, [CanBeNull] Regex searchPattern, out bool hasMatch) {

            ThreadHelper.ThrowIfNotOnUIThread();

            hasMatch = false;

            if (parts.Count == 0) {
                return null;
            }

            var runInfos = ToRunInfo(parts, searchPattern, out hasMatch);
            var textBlock = new TextBlock {
                TextWrapping      = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };

            var highlightedSpanBrush = hasMatch ? GetHighlightedSpanBrush() : Brushes.Transparent;
            foreach (var runInfo in runInfos) {
                var inline = ToInline(runInfo, highlightedSpanBrush);
                textBlock.Inlines.Add(inline);
            }

            return textBlock;
        }

        IList<RunInfo> ToRunInfo(IReadOnlyCollection<string> parts, [CanBeNull] Regex searchPattern, out bool hasMatches) {
            hasMatches = false;

            if (searchPattern == null) {
                return parts.Select(part => new RunInfo(part, isMatch: false)).ToList();
            }

            var runInfos = new List<RunInfo>();
            foreach (var part in parts) {

                var matches = searchPattern.Matches(part);
                if (matches.Count > 0) {

                    var currentIndex = 0;
                    foreach (Match match in matches) {

                        // Der Text vor dem Treffertext
                        if (match.Index > currentIndex) {
                            var text = part.Substring(currentIndex, length: match.Index - currentIndex);
                            runInfos.Add(new RunInfo(text, isMatch: false));
                        }

                        // Der Treffertext
                        runInfos.Add(new RunInfo(match.Value, isMatch: true));
                        currentIndex = match.Index + match.Length;

                    }

                    // Der Text nach dem letzten Treffertext
                    if (currentIndex < part.Length) {
                        var text = part.Substring(currentIndex, length: part.Length - currentIndex);
                        runInfos.Add(new RunInfo(text, isMatch: false));
                    }

                    hasMatches = true;
                } else {
                    runInfos.Add(new RunInfo(part, false));
                }
            }

            return runInfos;
        }

        Run ToInline(RunInfo runInfo, Brush highlightedSpanBrush) {

            var inline = new Run(runInfo.Text);

            if (runInfo.IsMatch) {
                inline.Background = highlightedSpanBrush;
            }

            return inline;
        }

        private static SolidColorBrush GetHighlightedSpanBrush() {

            ThreadHelper.ThrowIfNotOnUIThread();

            var uiShell5 = ProjectExplorerPackage.ServiceProvider.GetService(typeof(SVsUIShell)) as IVsUIShell5;
            var color    = uiShell5?.GetThemedWPFColor(TreeViewColors.HighlightedSpanColorKey) ?? Colors.Orange;

            return new SolidColorBrush(color);
        }

        readonly struct RunInfo {

            public RunInfo(string classifiedText, bool isMatch) {
                Text    = classifiedText;
                IsMatch = isMatch;
            }

            public string Text    { get; }
            public bool   IsMatch { get; }

        }

    }

}