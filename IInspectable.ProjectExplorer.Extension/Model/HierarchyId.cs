using Microsoft.VisualStudio;

namespace IInspectable.ProjectExplorer.Extension {

    struct HierarchyId {

        readonly uint _id;

        public HierarchyId(uint id) {
            _id = id;
        }

        public HierarchyId(int id) {
            _id = (uint) id;
        }

        public static readonly HierarchyId Root = new HierarchyId(VSConstants.VSITEMID_ROOT);
        public static readonly HierarchyId Nil = new HierarchyId(VSConstants.VSITEMID_NIL);
        public static readonly HierarchyId Selection = new HierarchyId(VSConstants.VSITEMID_SELECTION);

        public uint Id          => _id;
        public bool IsNil       => _id == Nil.Id;
        public bool IsRoot      => _id == Root.Id;
        public bool IsSelection => _id == Selection.Id;

        public static implicit operator int(HierarchyId id) {
            return (int) id.Id;
        }
        
        public static implicit operator uint(HierarchyId id) {
            return id.Id;
        }

        public static implicit operator HierarchyId(int id) {
            return new HierarchyId(id);
        }

        public static implicit operator HierarchyId(uint id) {
            return new HierarchyId(id);
        }

        public override string ToString() {

            if(IsRoot) {
                return "ROOT";
            }
            if(IsNil) {
                return "NIL";
            }
            if(IsSelection) {
                return "SELECTION";
            }

            return Id.ToString();
        }
    }

}