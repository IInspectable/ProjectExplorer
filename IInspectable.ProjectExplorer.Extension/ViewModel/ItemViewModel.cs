using Microsoft.VisualStudio.Imaging.Interop;

namespace IInspectable.ProjectExplorer.Extension {

    abstract class ItemViewModel : ViewModelBase {

        bool _visible;

        protected ItemViewModel() {
            _visible = true;
        }
        public abstract string DisplayName { get; }
        public abstract ImageMoniker ImageMoniker { get; set;}
        public abstract bool IsSelected { get; set; }

        public bool Visible {
            get { return _visible; }
            set {
                if(value == _visible) {
                    return;
                }
                _visible = value;
                NotifyPropertyChanged();
            }
        }

        public abstract void Filter(SearchContext context);

        internal void NotifyIsSelectedChanged() {
            NotifyThisPropertyChanged(nameof(IsSelected));
        }
    }

}