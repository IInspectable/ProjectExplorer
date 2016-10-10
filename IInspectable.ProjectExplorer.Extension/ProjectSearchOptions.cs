#region Using Directives

using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Internal.VisualStudio.PlatformUI;

#endregion

namespace IInspectable.ProjectExplorer.Extension {

    public class ProjectSearchOptions {

        readonly ImmutableList<IVsWindowSearchOption> _searchOptions;
        readonly WindowSearchBooleanOption _loadedProjectsOption;
        readonly WindowSearchBooleanOption _unloadedProjectsOption;
        readonly WindowSearchBooleanOption _closedProjectsOption;

        IVsEnumWindowSearchOptions _optionsEnum;

        public ProjectSearchOptions() {

            _searchOptions = new List<IVsWindowSearchOption> {

                { _loadedProjectsOption   = new WindowSearchBooleanOption("Loaded Projects"  , "Loaded Projects"  , true)},
                { _unloadedProjectsOption = new WindowSearchBooleanOption("Unloaded Projects", "Unloaded Projects", true)},
                { _closedProjectsOption   = new WindowSearchBooleanOption("Closed Projects"  , "Closed Projects"  , true)},

            }.ToImmutableList();
        }

        public bool LoadedProjects {
            get { return _loadedProjectsOption.Value; }
            set { _loadedProjectsOption.Value = value; }
        }

        public bool UnloadedProjects {
            get { return _unloadedProjectsOption.Value; }
            set { _unloadedProjectsOption.Value = value; }
        }

        public bool ClosedProjects {
            get { return _closedProjectsOption.Value; }
            set { _closedProjectsOption.Value = value; }
        }

        public IVsEnumWindowSearchOptions SearchOptionsEnum {
            get { return _optionsEnum ?? (_optionsEnum = new WindowSearchOptionEnumerator(_searchOptions)); }
        }

        public void ProvideSearchSettings(IVsUIDataSource pSearchSettings) {

            Utilities.SetValue(pSearchSettings, SearchSettingsDataSource.SearchStartTypeProperty.Name, (uint)VSSEARCHSTARTTYPE.SST_DELAYED);
            Utilities.SetValue(pSearchSettings, SearchSettingsDataSource.SearchProgressTypeProperty.Name, (uint)VSSEARCHPROGRESSTYPE.SPT_INDETERMINATE);
        }
    }
}