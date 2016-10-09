#region Using Directives

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    partial class ProjectExplorerControl : UserControl {

        public ProjectExplorerControl(ProjectExplorerViewModel viewModel) {

            DataContext = viewModel;

            InitializeComponent();
        }

        ProjectExplorerViewModel ViewModel { get { return DataContext as ProjectExplorerViewModel; } }

        void OnSettingsClick(object sender, RoutedEventArgs e) {
            // TODO OnSettingsClick
            MessageBox.Show("Coming soon");
        }

        void OnProjectListMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {

            // TODO Selection Handling in ViewModel verlagern
            var item=((ListBox) sender).SelectedItem as ProjectViewModel;

            item?.DefaultAction();

            //item?.OpenFolderInFileExplorer();           
        }

        void OnSettingsContextMenuOpening(object sender, ContextMenuEventArgs e) {
            // TODO Tastaturfall berücksichtigen (-1, -1)
            var source = e.OriginalSource as FrameworkElement;
            if(source == null) {
                return;
            }

            var ptScreen=source.PointToScreen(new Point(e.CursorLeft, e.CursorTop));
            ViewModel.ShowSettingsButtonContextMenu((int)ptScreen.X, (int)ptScreen.Y);

            e.Handled = true;
        }

        void OnProjectItemContextMenuOpening(object sender, ContextMenuEventArgs e) {
            // TODO Tastaturfall berücksichtigen (-1, -1)
            var source = e.OriginalSource as FrameworkElement;
            if (source == null) {
                return;
            }

            var ptScreen = source.PointToScreen(new Point(e.CursorLeft, e.CursorTop));
            ViewModel.ShowProjectItemContextMenu((int)ptScreen.X, (int)ptScreen.Y);

            e.Handled = true;
        }

    }
}