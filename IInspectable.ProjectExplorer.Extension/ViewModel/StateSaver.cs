using System;

namespace IInspectable.ProjectExplorer.Extension {

    sealed class StateSaver<T>: IDisposable {

        readonly T         _previousState;
        readonly Action<T> _setter;

        public StateSaver(T value, Func<T> getter, Action<T> setter) {
            _previousState = getter();
            _setter        = setter;
            _setter(value);
        }

        public void Dispose() {
            _setter(_previousState);
        }

    }

}