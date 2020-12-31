using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace IInspectable.ProjectExplorer.Extension {

    class HierarchyData {

        static readonly Logger Logger = Logger.Create<HierarchyData>();

        public HierarchyData(Hierarchy hierarchy) {
            Hierarchy = hierarchy;
        }

        public  Hierarchy Hierarchy { get; }
        private uint      _eventCookie;

        public void UnadviseHierarchyEvents() {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_eventCookie != 0) {
                Hierarchy.UnadviseHierarchyEvents(_eventCookie);
                _eventCookie = 0;
            }
        }

        public void AdviseHierarchyEvents(IVsHierarchyEvents hierarchyEvents) {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_eventCookie != 0) {
                Logger.Error($"{nameof(AdviseHierarchyEvents)}: event cookie not 0 ({_eventCookie})");
            }

            _eventCookie = Hierarchy?.AdviseHierarchyEvents(hierarchyEvents) ?? 0;
        }

    }

}