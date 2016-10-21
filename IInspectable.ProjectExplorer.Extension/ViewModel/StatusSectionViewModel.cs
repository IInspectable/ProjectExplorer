using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace IInspectable.ProjectExplorer.Extension {

    class StatusSectionViewModel: ItemViewModel {

        int _visibleCount;
        bool _isExpanded;

        readonly ProjectStatus _status;
        readonly List<ProjectItemViewModel> _projectItems;

        public StatusSectionViewModel(ProjectStatus status) {
            _status = status;
            _projectItems = new List<ProjectItemViewModel>();

            VisibleCount = 0;
            IsExpanded   = true;
            Visible      = false;
        }

        public override string DisplayName {
            get {
                switch (Status) {
                    case ProjectStatus.Closed:
                        return "Closed";
                    case ProjectStatus.Unloaded:
                        return "Unloaded";
                    case ProjectStatus.Loaded:
                        return "Loaded";
                    default:
                        return "Unknown";
                }
            }
        }

        public int VisibleCount {
            get { return _visibleCount; }
            protected set {
                if(_visibleCount == value) {
                    return;
                }
                _visibleCount = value;
                NotifyPropertyChanged();
            }
        }

        public override ImageMoniker ImageMoniker {
            get { return KnownMonikers.None; }
        }

        public override bool IsSelected { get; set; }

        public IReadOnlyList<ProjectItemViewModel> ProjectItems {
            get { return _projectItems; }
        }

        public IReadOnlyList<ItemViewModel> Children {
            get { return ProjectItems.Cast<ItemViewModel>()
                                     .OrderBy(p => p.DisplayName)
                                     .ToList(); }        
        }

        public ProjectStatus Status {
            get { return _status; }
        }

        public bool IsExpanded {
            get { return _isExpanded; }
            set {
                if(_isExpanded == value) {
                    return;
                }
                _isExpanded = value;
                NotifyPropertyChanged();
            }
        }

        public void Populate(IEnumerable<ProjectItemViewModel> projectItemViewModels, SearchContext searchContext) {
            _projectItems.Clear();
            _projectItems.AddRange(projectItemViewModels.Where(p => p.Status == Status));
            NotifyThisPropertyChanged(nameof(ProjectItems));
            Filter(searchContext);
        }

        public override void Filter(SearchContext context) {

            foreach(var item in ProjectItems) {
                item.Filter(context);
            }

            VisibleCount = ProjectItems.Count(p => p.Visible);
            Visible      = VisibleCount > 0;
            NotifyThisPropertyChanged(nameof(Children));
            NotifyThisPropertyChanged(nameof(DisplayName));
        }
    }
}