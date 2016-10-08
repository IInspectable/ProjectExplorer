#region Using Directives

using System.Windows;
using System.Windows.Controls;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    public partial class ProjectExplorerControl : UserControl {

        readonly ProjectExplorerViewModel _viewModel;

        public ProjectExplorerControl(ProjectExplorerViewModel viewModel) {
            _viewModel  = viewModel;
            DataContext = _viewModel;

            InitializeComponent();
        }

        void OnSettingsClick(object sender, RoutedEventArgs e) {
            // TODO OnSettingsClick
            MessageBox.Show("Coming soon");
        }

        private void VsListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            var item=((ListBox) sender).SelectedItem as ProjectViewModel;
            if(item == null) {
                return;
            }

            item.DefaultAction();
        }
    }
}