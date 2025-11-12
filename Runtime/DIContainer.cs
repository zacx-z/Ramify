using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Object = UnityEngine.Object;

namespace Nela.Ramify {
    public class DIContainer {
        public delegate T Factory<T>();

        private ScopeStack _scopes = new ScopeStack(new BindingScopeInfo(0, null));
        private int _nextScopeId;

        public DIContainer() {}

        private DIContainer(ScopeStack scopes) {
            _scopes = scopes;
        }

        public ScopePointer EnterScope() {
            _scopes.Push(_nextScopeId);
            _nextScopeId = 0;
            return _scopes.GetPointer();
        }
        
        public void ExitScope() {
            var scope = _scopes.Pop();
            scope.OnExit();
            _nextScopeId = scope.id + 1;
        }

        public IBinding<T> Bind<T>(T value) where T : IViewModel {
            var binding = new SingleBindingEntry<T>(value);
            AddBindingWithInterfaces(typeof(T), binding);
            return binding;
        }

        public IBinding Bind(object value, Type type) {
            var binding = (BindingEntry)Activator.CreateInstance(typeof(SingleBindingEntry<>).MakeGenericType(type), value);
            AddBindingWithInterfaces(type, binding);
            return (IBinding)binding;
        }

        /// <summary>
        /// Create a binding without binding an actual value. Use `Rebind()` to specify a value to it.
        /// </summary>
        /// <param name="type">Type of the binding.</param>
        /// <returns>A binding interface for rebinding.</returns>
        public IBinding CreateBinding(Type type) {
            var binding = (BindingEntry)Activator.CreateInstance(typeof(SingleBindingEntry<>).MakeGenericType(type));
            AddBindingWithInterfaces(type, binding);
            return (IBinding)binding;
        }

        public IBinding<T> CreateBinding<T>() where T : IViewModel {
            var binding = new SingleBindingEntry<T>();
            AddBindingWithInterfaces(typeof(T), binding);
            return binding;
        }

        public ISequenceBinding<T> BindSequence<T>(IEnumerable<T> values) where T : IViewModel {
            var binding = new SequenceBindingEntry<T>(values);
            AddBindingWithInterfaces(typeof(T), binding);
            return binding;
        }

        public ISequenceBinding<object> BindSequence(Type type, IEnumerable<object> values) {
            var binding = new SequenceBindingEntry<object>(values);
            AddBindingWithInterfaces(type, binding);
            return binding;
        }

        public ISequenceBinding<T> CreateSequenceBinding<T>() where T : IViewModel {
            var binding = new SequenceBindingEntry<T>();
            AddBindingWithInterfaces(typeof(T), binding);
            return binding;
        }

        public ISequenceBinding<object> CreateSequenceBinding(Type type) {
            var binding = new SequenceBindingEntry<object>();
            AddBindingWithInterfaces(type, binding);
            return binding;
        }

        public IBinding<T> Bind<T>(Factory<T> factory) where T : IViewModel {
            var binding = new FactoryBindingEntry<T>(factory);
            AddBindingWithInterfaces(typeof(T), binding);
            return binding;
        }


        private void AddBindingWithInterfaces(Type type, BindingEntry bindingEntry) {
            AddBinding(type, bindingEntry);

#if UNITY_EDITOR
            bindingEntry.source = currentSource;
#endif
            foreach (var @interface in type.GetViewModelInterfaces()) {
                AddBinding(@interface, bindingEntry);
            }
        }

        private void AddBinding(Type type, BindingEntry bindingEntry) {
            var currentScope = _scopes.Peek();
            currentScope.AddBinding(type, bindingEntry);
        }

        public T Resolve<T>() {
            return (T)Resolve(typeof(T));
        }

        public IReactiveValue<T> Observe<T>() {
            return (IReactiveValue<T>)Observe(typeof(T));
        }

        public object Resolve(Type type) {
            try {
                var binding = _scopes.Peek().FindBinding(type);
                var result = binding.GetValue(_scopes);
#if UNITY_EDITOR
                onResolveFor?.Invoke(result, currentView, binding.bindingType, binding.source);
#endif
                return result;
            }
            catch (BindingNotFoundException) {
                throw new MissingViewModelException(type);
            }
            catch (ConflictedValueException) {
                throw new ConflictedViewModelException(type);
            }
            catch (SequenceReachEndException) {
                throw new ViewModelExhaustedException(type);
            }
        }

        public IObservable<object> Observe(Type type) {
            try {
                var binding = _scopes.Peek().FindBinding(type);
                var result = (IObservable<object>)binding.ObserveValue(_scopes);
#if UNITY_EDITOR
                onResolveObservableFor?.Invoke(result, currentView, binding.bindingType, binding.source);
#endif
                return result;
            }
            catch (BindingNotFoundException) {
                throw new MissingViewModelException(type);
            }
            catch (ConflictedValueException) {
                throw new ConflictedViewModelException(type);
            }
            catch (SequenceReachEndException) {
                throw new ViewModelExhaustedException(type);
            }
        }

        public bool TryResolve<T>(out T value) {
            if (TryResolve(typeof(T), out var boxedValue)) {
                value = (T)boxedValue;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryResolve(Type type, out object value) {
            try {
                var bindingRef = _scopes.Peek().FindBindingReference(type);
                if (!bindingRef.isNil()) {
                    value = bindingRef.entry.GetValue(_scopes);
#if UNITY_EDITOR
                    onResolveFor?.Invoke(value, currentView, bindingRef.entry.bindingType, bindingRef.entry.source);
#endif
                    return true;
                }

                value = null;
                return false;
            }
            catch (ConflictedValueException) {
                throw new ConflictedViewModelException(type);
            }
            catch (SequenceReachEndException) {
                throw new ViewModelExhaustedException(type);
            }
        }

        public bool TryObserve<T>(out IReactiveValue<T> value) {
            if (TryObserve(typeof(T), out var boxedValue)) {
                value = (IReactiveValue<T>)boxedValue;
                return true;
            }

            value = default;
            return false;
        }

        public bool TryObserve(Type type, out object value) {
            try {
                var bindingRef = _scopes.Peek().FindBindingReference(type);
                if (!bindingRef.isNil()) {
                    value = bindingRef.entry.ObserveValue(_scopes);
#if UNITY_EDITOR
                    onResolveObservableFor?.Invoke((IObservable<object>)value, currentView, bindingRef.entry.bindingType, bindingRef.entry.source);
#endif
                    return true;
                }

                value = null;
                return false;
            }
            catch (ConflictedValueException) {
                throw new ConflictedViewModelException(type);
            }
            catch (SequenceReachEndException) {
                throw new ViewModelExhaustedException(type);
            }
        }

        public string DumpInfo() {
            var sb = new StringBuilder();
            sb.Append("DI Container: ");
            foreach (var binding in _scopes.Peek().GetAllBindings()) {
                sb.Append(binding.Key);
                sb.Append(',');
            }

            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }

        private class BindingScopeInfo {
            public struct BindingReference {
                public static BindingReference nil = default;
                public BindingEntry entry;
                public BindingScopeInfo enclosingScope;

                public bool isNil() {
                    return entry == null;
                }
            }

            private Dictionary<Type, BindingReference> _bindings = new();

            public int id { get; }
            public readonly BindingScopeInfo parent;

            public BindingScopeInfo(int id, BindingScopeInfo parent) {
                this.id = id;
                this.parent  = parent;
            }

            public void OnExit() {
            }

            public BindingReference FindBindingReference(Type type) {
                if (!_bindings.TryGetValue(type, out var result)) {
                    result = parent?.FindBindingReference(type) ?? BindingReference.nil;
                    _bindings.Add(type, result);
                }

                return result;
            }

            public BindingEntry FindBinding(Type type) {
                var bindingRef = FindBindingReference(type);
                if (bindingRef.isNil()) {
                    throw new BindingNotFoundException();
                }

                return bindingRef.entry;
            }

            public void AddBinding(Type type, BindingEntry bindingEntry) {
                var bindingRef = FindBindingReference(type);
                if (bindingRef.isNil() || bindingRef.enclosingScope != this) {
                    bindingRef.entry = bindingEntry;
                    bindingRef.enclosingScope = this;
                } else {
                    bindingRef.entry = new ConflictedBindingEntry(type);
                }
                _bindings[type] = bindingRef;
            }

            public Dictionary<Type, BindingEntry> GetAllBindings() {
                var bindings = new Dictionary<Type, BindingEntry>();
                GetBindings(bindings);
                return bindings;
            }

            private void GetBindings(Dictionary<Type, BindingEntry> outBindings) {
                foreach (var binding in _bindings) {
                    if (binding.Value.enclosingScope == this) {
                        outBindings.TryAdd(binding.Key, binding.Value.entry);
                    }
                }

                if (parent != null) parent.GetBindings(outBindings);
            }
        }

        public class ScopePointer {
            private readonly BindingScopeInfo _scope;
            private readonly int _stateStamp;

            internal ScopePointer(object scope, int stateStamp) {
                _scope = (BindingScopeInfo)scope;
                _stateStamp = stateStamp;
            }

            public DIContainer ToContainer() {
                return new DIContainer(new ScopeStack(_scope, _stateStamp));
            }
        }

        // a helper class to manage scope chain
        private class ScopeStack {
            private BindingScopeInfo _head;
            private int _count;
            /// <summary>
            /// State stamp is used for protection on state discrepancy.
            /// </summary>
            ///
            /// Prevent state discrepancy where a DIContainer restored from a scope pointer tries to move forward a binding entry that has already be moved after the pointer was created.
            /// This is illegal because the pointer is supposed to point at a previous state of the DIContainer.
            public int stateStamp { get; set; }

            public int Count => _count;

            public ScopeStack(BindingScopeInfo head, int stateStamp = 0) {
                _head = head;
                _count = GetCount(head);
                this.stateStamp = stateStamp;
            }

            public BindingScopeInfo Peek() {
                return _head;
            }

            public BindingScopeInfo Push(int scopeId) {
                _head = new BindingScopeInfo(scopeId, _head);
                _count++;
                return _head;
            }

            public BindingScopeInfo Pop() {
                var ret = _head;
                _head = _head.parent;
                _count--;
                return ret;
            }

            public ScopePointer GetPointer() {
                return new ScopePointer(_head, stateStamp);
            }

            private static int GetCount(BindingScopeInfo head) {
                var ret = 0;
                while (head != null) {
                    ret++;
                    head = head.parent;
                }
                return ret;
            }

            public BindingScopeInfo this[int depth] {
                get {
                    var ret = _head;

                    for (int i = _count - 1; i > depth; i--) {
                        ret = ret.parent;
                    }

                    return ret;
                }
            }
        }

        private abstract class BindingEntry {
            public abstract object GetValue(ScopeStack scopeStack);
            public abstract object ObserveValue(ScopeStack scopeStack);

#if UNITY_EDITOR
            public Object source { get; set; }
            public abstract Type bindingType { get; }
#endif
        }

        private class SingleBindingEntry<T> : BindingEntry, IBinding<T>, IBinding {
            private bool _hasValue;
            private T _value;
            private ReactiveValue<T> _reactiveValue;

            public SingleBindingEntry() {
                _hasValue = false;
            }

            public SingleBindingEntry(T value) {
                _value = value;
                _hasValue = true;
            }

            public override object GetValue(ScopeStack scopeStack) {
                if (!_hasValue) throw new NoValueException(typeof(T));
                return _value;
            }

            public override object ObserveValue(ScopeStack scopeStack) {
                if (!_hasValue) throw new NoValueException(typeof(T));
                return _reactiveValue ??= new ReactiveValue<T>(_value);
            }

#if UNITY_EDITOR
            public override Type bindingType => typeof(T);
#endif

            public void Rebind(T value) {
                _value = value;
                _hasValue = true;
                _reactiveValue?.OnNext(_value);
            }

            public void Rebind(object value) {
                Rebind((T)value);
            }
        }

        private class SequenceBindingEntry<T> : BindingEntry, ISequenceBinding<T> {
            private class Shared {
                public T[] values;
                public ReactiveValue<T>[] observedValues;
            }

            private Shared _shared;
            private T[] values {
                get => _shared.values;
                set => _shared.values = value;
            }

            private ReactiveValue<T>[] observedValues {
                get => _shared.observedValues;
                set => _shared.observedValues = value;
            }

            private int _currentIndex = -1;
            private int _prevScopeId = -1;
            private int _prevScopeDepth;
            private int _stackstamp = 0;

            public SequenceBindingEntry() {
                _shared = new Shared();
            }

            public SequenceBindingEntry(IEnumerable<T> values) {
                _shared = new Shared();
                this.values = values.ToArray();
            }

            private SequenceBindingEntry(SequenceBindingEntry<T> copyFrom) {
                _shared = copyFrom._shared;
                _currentIndex = copyFrom._currentIndex;
                _prevScopeId = copyFrom._prevScopeId;
                _prevScopeDepth = copyFrom._prevScopeDepth;
            }

            public override object GetValue(ScopeStack scopeStack) {
                if (values == null) throw new NoValueException(typeof(T));
                try {
                    MoveCurrent(scopeStack);
                    return values[_currentIndex];
                }
                catch (IndexOutOfRangeException) {
                    throw new SequenceReachEndException();
                }
            }

            public override object ObserveValue(ScopeStack scopeStack) {
                if (values == null) throw new NoValueException(typeof(T));
                try {
                    observedValues ??= new ReactiveValue<T>[values.Length];
                    MoveCurrent(scopeStack);
                    return observedValues[_currentIndex] ??= new ReactiveValue<T>(values[_currentIndex]);
                }
                catch (IndexOutOfRangeException) {
                    throw new SequenceReachEndException();
                }
            }

#if UNITY_EDITOR
            public override Type bindingType => typeof(T);
#endif

            public void RebindItem(int index, T value) {
                values[index] = value;
                observedValues[index]?.OnNext(value);
            }

            public void RebindAll(IEnumerable<T> newValues) {
                values = newValues.ToArray();
                if (observedValues == null) {
                    observedValues = new ReactiveValue<T>[values.Length];
                } else {
                    // try extend
                    if (observedValues.Length < values.Length) {
                        var newObservedValues = new ReactiveValue<T>[values.Length];
                        Array.Copy(observedValues, newObservedValues, observedValues.Length);
                        observedValues = newObservedValues;
                    }
                }

                for (int i = 0; i < observedValues.Length; i++) {
                    observedValues[i]?.OnNext(i < values.Length ? values[i] : default);
                }
            }

            // move the current pointer to the next
            private void MoveCurrent(ScopeStack scopeStack) {
                if (scopeStack.stateStamp < _stackstamp)
                    throw new InvalidOperationException($"State discrepancy: the container is not up to date with this binding entry's state.");

                // do not proceed at the same node and its children
                if (scopeStack.Count - 1 < _prevScopeDepth || scopeStack[_prevScopeDepth].id != _prevScopeId) {
                    _currentIndex++;
                    _prevScopeId = scopeStack.Peek().id;
                    _prevScopeDepth = scopeStack.Count - 1;

                    scopeStack.stateStamp++;
                }
            }
        }

        private class FactoryBindingEntry<T> : BindingEntry, IBinding<T> {
            private readonly Factory<T> _factory;
            private ReactiveValue<T> _observedValue;

            public FactoryBindingEntry(Factory<T> factory) {
                this._factory = factory;
            }

            public override object GetValue(ScopeStack scopeStack) {
                return _factory(); // TODO: let one instance bind to a node
            }

            public override object ObserveValue(ScopeStack scopeStack) {
                return new ReactiveValue<T>(_factory());
            }

#if UNITY_EDITOR
            public override Type bindingType => typeof(T);
#endif

            public void Rebind(T value) {
                throw new NotSupportedException();
            }
        }

        private class ConflictedBindingEntry : BindingEntry {
            private readonly Type _type;

            public ConflictedBindingEntry(Type type) {
                _type = type;
            }

            public override object GetValue(ScopeStack scopeStack) {
                throw new ConflictedValueException();
            }

            public override object ObserveValue(ScopeStack scopeStack) {
                throw new ConflictedValueException();
            }

#if UNITY_EDITOR
            public override Type bindingType => _type;
#endif
        }

        #region Debug Context
#if UNITY_EDITOR
        public static event Action<object, View, Type, Object> onResolveFor;
        public static event Action<IObservable<object>, View, Type, Object> onResolveObservableFor;

        private Object _defaultSource = null;

        private Object currentSource => currentView ?? _defaultSource;
        public View currentView { get; set; }

        /// <summary>
        /// Setting the default source of registered view models to provide context information for debugging.
        /// </summary>
        public void SetDefaultSource(Object gameObject) {
            _defaultSource = gameObject;
        }
#endif
        #endregion
    }
}