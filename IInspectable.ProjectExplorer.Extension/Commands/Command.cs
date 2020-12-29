#region Using Directives

using System;
using System.Windows.Input;
using System.ComponentModel;
using System.ComponentModel.Design;

using Microsoft.VisualStudio.Shell;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    abstract class Command: ICommand, INotifyPropertyChanged {

        readonly OleMenuCommand _command;

        protected Command(int commandId, Guid? menuGroupOrDefault = null) {

            var menuGroup = menuGroupOrDefault ?? PackageGuids.ProjectExplorerWindowPackageCmdSetGuid;

            var menuCommandId = new CommandID(menuGroup, commandId);
            _command = new OleMenuCommand(OnExecute, menuCommandId);

            _command.BeforeQueryStatus += (_, _) => UpdateState();
            _command.CommandChanged    += OnCommandChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler                CanExecuteChanged;

        public Guid MenuGroup => _command.CommandID.Guid;

        public int CommandId => _command.CommandID.ID;

        public bool Enabled {
            get => _command.Enabled;
            set => _command.Enabled = value;
        }

        public bool Supported {
            get => _command.Supported;
            set => _command.Supported = value;
        }

        public bool Visible {
            get => _command.Visible;
            set => _command.Visible = value;
        }

        public void Register(OleMenuCommandService commandService) {
            commandService.AddCommand(_command);
        }

        public void Unregister(OleMenuCommandService commandService) {
            commandService.RemoveCommand(_command);
        }

        public bool CanExecute(object parameter = null) {
            return _command.Enabled && _command.Supported && _command.Visible;
        }

        public abstract void Execute(object parameter = null);

        public virtual void UpdateState() {
        }

        void OnCommandChanged(object sender, EventArgs e) {
            CanExecuteChanged?.Invoke(this, e);
            NotifyPropertiesChanged();
        }

        void OnExecute(object sender, EventArgs e) {
            Execute();
        }

        void NotifyPropertiesChanged() {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }

    }

}