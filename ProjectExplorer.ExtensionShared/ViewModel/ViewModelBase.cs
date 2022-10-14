using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IInspectable.ProjectExplorer.Extension; 

abstract class ViewModelBase: INotifyPropertyChanged {

    public event PropertyChangedEventHandler PropertyChanged;

    protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null) {
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
    }

    protected void NotifyThisPropertyChanged(string propertyName = null) {
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
    }

    protected void NotifyAllPropertiesChanged() {
        OnPropertyChanged(new PropertyChangedEventArgs(string.Empty));
    }

    protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) {
        PropertyChanged?.Invoke(this, e);
    }
}