#region Using Directives

using System;
using Microsoft.WindowsAPICodePack.Dialogs;

#endregion

namespace IInspectable.ProjectExplorer.Extension {
    sealed class SettingsCommand : Command {

        readonly ProjectExplorerViewModel _viewModel;
        bool _executing;

        public SettingsCommand(ProjectExplorerViewModel viewModel)
            : base(PackageIds.SettingsCommandId) {

            if (viewModel == null) {
                throw new ArgumentNullException(nameof(viewModel));
            }

            _viewModel = viewModel;
            _executing = false;
        }

        public bool Executing {
            get { return _executing; }
            set {
                if(value == _executing) {
                    return;
                }
                _executing = value;
                UpdateState();
            }
        }

        public override void UpdateState() {
            Enabled = _viewModel.IsSolutionLoaded && !Executing;
        }

        public override async void Execute(object parameter=null) {

            if(!CanExecute(parameter)) {
                return;
            }

            Executing = true;
            try {
                
                var dlg = new CommonOpenFileDialog {
                    Title                     = "Select Folder",
                    InitialDirectory          = _viewModel.ProjectsRoot,
                    DefaultDirectory          = _viewModel.ProjectsRoot,
                    AddToMostRecentlyUsedList = false,
                    IsFolderPicker            = true,
                    EnsurePathExists          = true,
                    EnsureFileExists          = true,
                    Multiselect               = true,
                    ShowPlacesList            = true,
                    EnsureValidNames          = true
                };

                CommonFileDialogResult result;

                using (ShellUtil.EnterModalState()) {
                    result = dlg.ShowDialog();
                }

                if(result == CommonFileDialogResult.Ok) {
                    await _viewModel.SetProjectsRoot(dlg.FileName);
                }

            } finally {
                Executing = false;
            }
        }
    }
}