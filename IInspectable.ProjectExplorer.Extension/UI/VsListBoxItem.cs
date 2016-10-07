#region Using Directives

using System.Windows;
using System.Windows.Controls;

#endregion

namespace IInspectable.ProjectExplorer.Extension.UI {

    public class VsListBoxItem : ListBoxItem {

        static VsListBoxItem() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VsListBoxItem), new FrameworkPropertyMetadata(typeof(VsListBoxItem)));
        }
    }
}