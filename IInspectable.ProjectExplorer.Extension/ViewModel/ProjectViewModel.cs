#region Using Directives

using System;

using JetBrains.Annotations;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging.Interop;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    class ProjectViewModel: ItemViewModel {

        [NotNull]
        readonly ProjectFile _projectFile;

        [CanBeNull]
        ProjectExplorerViewModel _parent;

        ProjectStatus        _status;
        private ImageMoniker _imageMoniker;

        public ProjectViewModel(ProjectFile projectFile) {

            _status      = ProjectStatus.Closed;
            _projectFile = projectFile ?? throw new ArgumentNullException(nameof(projectFile));
        }

        [CanBeNull]
        public ProjectExplorerViewModel Parent => _parent;

        public override string DisplayName => _projectFile.Name;
        public          string Directory   => System.IO.Path.GetDirectoryName(_projectFile.Path);
        public          string Path        => _projectFile.Path;

        public override bool IsSelected {
            get { return _parent?.SelectionService.IsSelected(this) ?? false; }
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

        public ProjectStatus Status {
            get => _status;
            set {
                if (_status == value) {
                    return;
                }

                _status = value;
                NotifyPropertyChanged();
            }
        }

        public override ImageMoniker ImageMoniker {
            get => _imageMoniker;
            set {
                if (_imageMoniker.Equals(value)) {
                    return;
                }

                _imageMoniker = value;
                NotifyPropertyChanged();
            }
        }

        public override void Filter(SearchContext context) {
            Visible = context.IsMatch(DisplayName);

            if (IsSelected && !Visible) {
                IsSelected = false;
            }
        }

        public int Open() {
            return _parent?.OpenProject(this) ?? VSConstants.E_FAIL;
        }

        public int Close() {
            return _parent?.CloseProject(this) ?? VSConstants.S_OK;
        }

        public int Reload() {
            return _parent?.ReloadProject(this) ?? VSConstants.E_FAIL;
        }

        public int Unload() {
            return _parent?.UnloadProject(this) ?? VSConstants.E_FAIL;
        }

        public void SetParent([NotNull] ProjectExplorerViewModel parent) {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

    }

}