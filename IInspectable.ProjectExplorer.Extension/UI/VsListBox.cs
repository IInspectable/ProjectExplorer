#region Using Directives

using System.Windows;
using System.Windows.Controls;

#endregion

namespace IInspectable.ProjectExplorer.Extension.UI {

    public class VsListBox : ListBox {

        protected override DependencyObject GetContainerForItemOverride() {
            return new VsListBoxItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item) {
            return item is VsListBoxItem;
        }
    }
}