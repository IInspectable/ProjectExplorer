#region Using Directives

using System.Windows;
using System.Windows.Controls;

#endregion

namespace IInspectable.ProjectExplorer.Extension {
    

    public partial class ProjectExplorerControl : UserControl {

        public ProjectExplorerControl() {
            InitializeComponent();
        }

        void OnButtonClick(object sender, RoutedEventArgs e) {
            MessageBox.Show(
                string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}'", ToString()),
                "ProjectExplorerWindow");
        }
    }
}