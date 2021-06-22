#region Using Directives

using System.Windows;
using System.Windows.Controls;

#endregion

namespace IInspectable.ProjectExplorer.Extension.UI {

    public class VsListBoxItem : ListBoxItem {

        static VsListBoxItem() {
            #if VS2022
            // Styles funktionieren (noch?) nicht in VS 2022
            #else
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VsListBoxItem), new FrameworkPropertyMetadata(typeof(VsListBoxItem)));
            #endif
        }
    }
}