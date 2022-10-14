#region Using Directives

using System;

using JetBrains.Annotations;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.PatternMatching;

#endregion

namespace IInspectable.ProjectExplorer.Extension; 

class ProjectViewModel: ItemViewModel {

    [NotNull]
    readonly ProjectFile _projectFile;

    [CanBeNull]
    ProjectExplorerViewModel _parent;

    ProjectStatus _status;
    ImageMoniker? _imageMoniker;
    string        _displayName;

    public ProjectViewModel(ProjectFile projectFile) {

        _status      = ProjectStatus.Closed;
        _projectFile = projectFile ?? throw new ArgumentNullException(nameof(projectFile));
    }

    [CanBeNull]
    public ProjectExplorerViewModel Parent => _parent;

    public override bool IsSelected {
        get => _parent?.SelectionService.IsSelected(this) ?? false;
        set {

            if (value == IsSelected) {
                return;
            }

            if (value) {
                _parent?.SelectionService.AddSelection(this);
            } else {
                _parent?.SelectionService.RemoveSelection(this);
            }

            NotifyPropertyChanged();
        }
    }

    public          string        Directory    => System.IO.Path.GetDirectoryName(_projectFile.Path);
    public          string        Path         => _projectFile.Path;
    public          ProjectStatus Status       => _status;
    public override ImageMoniker  ImageMoniker => _imageMoniker              ??_projectFile.ImageMoniker;
    public override string        DisplayName  => _displayName.NullIfEmpty() ?? _projectFile.Name;

    public bool SetState(ProjectStatus status, ImageMoniker? imageMoniker, string displayName) {

        var stateChanged = false;

        if (!ImageMoniker.Equals(imageMoniker)) {
            stateChanged  = true;
            _imageMoniker = imageMoniker;
            NotifyThisPropertyChanged(nameof(ImageMoniker));
        }

        if (_status != status) {
            stateChanged = true;
            _status      = status;
            NotifyThisPropertyChanged(nameof(Status));
        }

        if (_displayName != displayName.NullIfEmpty()) {
            stateChanged = true;
            _displayName = displayName.NullIfEmpty();

            NotifyThisPropertyChanged(nameof(DisplayName));
            NotifyThisPropertyChanged(nameof(DisplayContent));
        }

        if (stateChanged) {
            NotifyAllPropertiesChanged();
        }

        return stateChanged;
    }

    private PatternMatch? _patternMatch;
    public  PatternMatch? PatternMatch => _patternMatch;

    public override void Filter(SearchContext context) {

        Visible = context.IsMatch(DisplayName, out _patternMatch);

        if (!Visible && IsSelected) {
            IsSelected = false;
        }

        NotifyThisPropertyChanged(nameof(DisplayContent));
        NotifyThisPropertyChanged(nameof(PatternMatch));
    }

    public object DisplayContent {
        get {
            ThreadHelper.ThrowIfNotOnUIThread();
            return _parent?.TextBlockBuilderService.ToTextBlock(
                part: DisplayName,
                patternMatch: _patternMatch) ?? (object) DisplayName;
        }
    }

    public HResult Open() {
        ThreadHelper.ThrowIfNotOnUIThread();
        return _parent?.OpenProject(this) ?? HResults.Failed;
    }

    public HResult Close() {
        ThreadHelper.ThrowIfNotOnUIThread();
        return _parent?.CloseProject(this) ?? HResults.Failed;
    }

    public HResult Reload() {
        ThreadHelper.ThrowIfNotOnUIThread();
        return _parent?.ReloadProject(this) ?? HResults.Failed;
    }

    public HResult Unload() {
        ThreadHelper.ThrowIfNotOnUIThread();
        return _parent?.UnloadProject(this) ?? HResults.Failed;
    }

    public void SetParent([NotNull] ProjectExplorerViewModel parent) {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }

}