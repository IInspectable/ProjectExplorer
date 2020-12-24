#region Using Directives

using System.Windows;
using System.Windows.Controls;

#endregion

namespace IInspectable.ProjectExplorer.Extension.UI {

    public class VsListBox : ListBox {

        static VsListBox() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(VsListBox), new FrameworkPropertyMetadata(typeof(VsListBox)));
        }

        protected override DependencyObject GetContainerForItemOverride() {
            return new VsListBoxItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item) {
            return item is VsListBoxItem;
        }

        public bool Navigate(bool up) {

            if (Items.Count == 0) {
                return false;
            }

            var nextSelectedIndex = SelectedIndex;

            if (up) {
                // keine Selektion
                if (nextSelectedIndex < 0) {
                    nextSelectedIndex = Items.Count;
                }

                nextSelectedIndex -= 1;

                if (nextSelectedIndex < 0) {
                    nextSelectedIndex = Items.Count - 1;
                }
            } else {
                // keine Selektion
                if (nextSelectedIndex < 0) {
                    nextSelectedIndex = -1;
                }

                nextSelectedIndex += 1;

                if (nextSelectedIndex >= Items.Count) {
                    nextSelectedIndex = 0;
                }
            }

            SelectedIndex = nextSelectedIndex;
            return true;

        }
    }
}