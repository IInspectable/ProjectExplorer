using Microsoft.VisualStudio;

namespace IInspectable.ProjectExplorer.Extension {

    readonly struct HResult {

        HResult(int value) {
            Value = value;

        }

        public int Value { get; }

        public bool Succeded       => ErrorHandler.Succeeded(Value);
        public bool Failed         => ErrorHandler.Failed(Value);
        public int  ThrowOnFailure => ErrorHandler.ThrowOnFailure(Value);

        public static implicit operator HResult(int hr) => new(hr);
        public static explicit operator int(HResult hr) => hr.Value;

    }

    static class HResults {

        public static HResult Failed => VSConstants.E_FAIL;
        public static HResult Ok     => VSConstants.S_OK;

    }

}