#region Using Directives

using System.Windows;
using System.Windows.Controls;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    partial class ProjectExplorerControl : UserControl {

        public ProjectExplorerControl(ProjectExplorerViewModel viewModel) {
            DataContext = viewModel;

            InitializeComponent();
        }

        void OnSettingsClick(object sender, RoutedEventArgs e) {
            // TODO OnSettingsClick
            MessageBox.Show("Coming soon");
        }

        void OnProjectListMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            var item=((ListBox) sender).SelectedItem as ProjectViewModel;

            item?.DefaultAction();

            //item?.OpenFolderInFileExplorer();           
        }
    }
}