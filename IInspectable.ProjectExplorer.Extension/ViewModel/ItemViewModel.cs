using Microsoft.VisualStudio.Imaging.Interop;

namespace IInspectable.ProjectExplorer.Extension {

    abstract class ItemViewModel : ViewModelBase {

        public abstract string DisplayName { get; }
        public abstract ImageMoniker ImageMoniker { get; }
        public abstract bool IsSelected { get; set; }

        internal void NotifyIsSelectedChanged() {
            NotifyThisPropertyChanged(nameof(IsSelected));
        }
    }
}