using System;
using Object = UnityEngine.Object;

namespace Nela.Ramify {
    internal class NoValueException : Exception {
        private readonly Type _type;
        public override string Message => $"No value found for {_type}";

        public NoValueException(Type type) {
            _type = type;
        }
    }

    internal class BindingNotFoundException : Exception {
    }

    internal class ConflictedValueException : Exception {
    }
    
    internal class SequenceReachEndException : Exception {}

    public abstract class ViewModelExceptionBase : Exception {
        public Type viewModelType { get; }

        protected ViewModelExceptionBase(Type viewModelType) {
            this.viewModelType = viewModelType;
        }
    }

    public class ConflictedViewModelException : ViewModelExceptionBase {
        public override string Message => $"Conflicted value on binding of type {viewModelType}";

        public ConflictedViewModelException(Type viewModelType) : base(viewModelType) { }
    }

    public class MissingViewModelException : ViewModelExceptionBase {
        public override string Message => $"Can't bind to {viewModelType} when there is already a value in the current scope.";

        public MissingViewModelException(Type viewModelType) : base(viewModelType) {
        }
    }

    public class ViewModelExhaustedException : ViewModelExceptionBase {
        public override string Message => $"Can't get more values from the sequential binding of type {viewModelType}";

        public ViewModelExhaustedException(Type viewModelType) : base(viewModelType) { }
    }
}