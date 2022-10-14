#region Using Directives

using System;

using Microsoft.VisualStudio.Shell;
using Microsoft.WindowsAPICodePack.Dialogs;

#endregion

namespace IInspectable.ProjectExplorer.Extension; 

sealed class SettingsCommand: Command {

    readonly ProjectExplorerViewModel _viewModel;
    bool                              _executing;

    public SettingsCommand(ProjectExplorerViewModel viewModel)
        : base(PackageIds.SettingsCommandId) {

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _executing = false;
    }

    public bool Executing {
        get => _executing;
        set {
            if (value == _executing) {
                return;
            }

            _executing = value;
            UpdateState();
        }
    }

    public override void UpdateState() {
        Enabled = !Executing && !_viewModel.IsLoading;
    }

    public override void Execute(object parameter = null) {

        ThreadHelper.ThrowIfNotOnUIThread();

        if (!CanExecute(parameter)) {
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

            if (result == CommonFileDialogResult.Cancel) {
                return;
            }

            ThreadHelper.JoinableTaskFactory.RunAsync(async () => await _viewModel.SetProjectsRootAsync(dlg.FileName))
                        .FileAndForget("ProjectExplorer/SettingsCommand.Execute");

        } finally {
            Executing = false;
        }
    }

}