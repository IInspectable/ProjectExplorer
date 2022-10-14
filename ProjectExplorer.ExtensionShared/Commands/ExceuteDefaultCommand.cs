using System;
using System.Linq;

namespace IInspectable.ProjectExplorer.Extension; 

sealed class ExceuteDefaultCommand: Command {

    readonly ProjectExplorerViewModel _viewModel;

    public ExceuteDefaultCommand(ProjectExplorerViewModel viewModel)
        : base(PackageIds.ExecuteDefaultCommandId) {

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }

    public override void UpdateState() {
        Enabled = GetDefaultCommand() != null;
        Visible = true;
    }

    public override void Execute(object parameter = null) {
        var command = GetDefaultCommand();
        command?.Execute();
    }

    Command GetDefaultCommand() {

        var states = _viewModel.SelectionService
                               .SelectedItems
                               .Select(item => item.Status)
                               .Distinct()
                               .ToList();

        if (states.Count != 1) {
            return null;
        }

        switch (states.Single()) {
            case ProjectStatus.Closed:
                return _viewModel.AddProjectCommand;
            case ProjectStatus.Unloaded:
                return _viewModel.LoadProjectCommand;
            case ProjectStatus.Loaded:
                return _viewModel.UnloadProjectCommand;
        }

        return null;
    }

}