using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nela.Ramify {
    public abstract class View : MonoBehaviour {
        public abstract Type viewModelType { get; }

        private void Inject(DIContainer diContainer) {
            OnInject(diContainer);
            OnPostInject();
        }

        /// <summary>
        /// Implement this function to initialize this view and add new bindings to the DI container for child views.
        /// </summary>
        /// <param name="diContainer">DI container of ViewModels</param>
        protected abstract void OnInject(DIContainer diContainer);

        protected virtual void OnPostInject() {}

        protected virtual void OnAfterInjectChildren() {}
        protected virtual void OnViewRecycled() {}

        public void Bind<TViewModel>(TViewModel viewModel) where TViewModel : IViewModel {
#if UNITY_EDITOR
            onBind?.Invoke(this, new IViewModel[] { viewModel });
#endif
            Bind(viewModel, typeof(TViewModel));
        }

        public void Bind(IViewModel viewModel, Type type) {
#if UNITY_EDITOR
            onBind?.Invoke(this, new IViewModel[] { viewModel });
#endif
            var container = new DIContainer();
            container.Bind(viewModel, type);

            var visitor = new ViewTreeVisitor(this);
            visitor.Inject(container);
        }

        public void BindMultiple(params IViewModel[] viewModels) {
            var container = new DIContainer();
            foreach (var vm in viewModels) {
                container.Bind(vm, vm.GetType());
            }

            var visitor = new ViewTreeVisitor(this, gameObject);
            visitor.Inject(container);
            container.ExitScope();
        }

        public void BindAll(GameObject gameObject, params IViewModel[] additionalViewModels) {
            var container = new DIContainer();
#if UNITY_EDITOR
            onBind?.Invoke(this, ViewModelUtils.GetAllViewModels(gameObject).Concat(additionalViewModels).ToList());
            container.SetDefaultSource(gameObject);
#endif
            foreach (var comp in ViewModelUtils.GetAllViewModels(gameObject)) {
                container.Bind(comp, comp.GetType());
            }

            container.EnterScope();
            foreach (var vm in additionalViewModels) {
                container.Bind(vm, vm.GetType());
            }

            var visitor = new ViewTreeVisitor(this, gameObject);
            visitor.Inject(container);
            container.ExitScope();
        }

        public void Recycle() {
            OnViewRecycled();
        }

        protected TView InstantiateView<TView>(TView viewPrefab, Transform container) where TView : View {
            var view = Instantiate(viewPrefab, container);
            ViewTreeVisitor.GetVisitor(this)?.MarkForInjection(this);
            return view;
        }

#if UNITY_EDITOR
        public bool HasBindings() {
            return ViewTreeVisitor.GetVisitor(this) != null;
        }
#endif

        private class ViewTreeVisitor : IViewUpdateHandler {
            private readonly View _rootView;
            private readonly Object _context;
            private HashSet<View> _markedViews;
            private HashSet<View> _cachedMarkedViews;
            private static Dictionary<View, ViewTreeVisitor> _visitorMap = new Dictionary<View, ViewTreeVisitor>();

            public ViewTreeVisitor(View view, Object context = null) {
                _rootView = view;
                _context = context;
            }

            public void Inject(DIContainer container) {
                RecursiveInject(_rootView.transform, container, true, _rootView.GetComponents<View>());
            }

            private void RecursiveInject(Transform node, DIContainer container, bool isRoot, params View[] views) {
                var treeNode = node.GetComponent<InjectionOptions>();
                
                if (treeNode != null) {
                    if (treeNode.injectionType == InjectionOptions.InjectionType.Ignore)
                        return;
                    if (treeNode.injectionType == InjectionOptions.InjectionType.OnlyAsBindingRoot && !isRoot) {
                        return;
                    }
                    if (!treeNode.gameObject.activeInHierarchy && treeNode.injectionType == InjectionOptions.InjectionType.IgnoreWhenInactive)
                        return;
                }

                List<View> afterInjectionViews = null;

                if (views.Length > 0) {
                    var pointer = container.EnterScope();
                    int hasInjectedCount = 0;
                    afterInjectionViews = new List<View>(4);
                    foreach (var view in views) {
                        if (WasViewInjected(view)) { // has injected
                            _markedViews?.Remove(view);
                            hasInjectedCount++;
                            continue;
                        }
                        try {
                            container.currentView = view;
                            afterInjectionViews.Add(view);
                            _visitorMap[view] = this;
                            view.Inject(container);
                            view.diContainerPointer = pointer;
                            container.currentView = null;
                        }
                        catch (Exception e) {
                            Logger.LogError(e, view, _context);
                        }

                        _markedViews?.Remove(view);
                    }

                    if (hasInjectedCount == views.Length) {
                        // skip this sub-tree if this was already injected
                        container.ExitScope();
                        return;
                    }
                }

                foreach (Transform child in node.transform) {
                    RecursiveInject(child, container, false, child.GetComponents<View>());
                }

                if (afterInjectionViews != null) {
                    foreach (var view in afterInjectionViews) {
                        try {
                            view.OnAfterInjectChildren();
                        }
                        catch (Exception e) {
                            Debug.LogException(e, view);
                        }
                    }

                    container.ExitScope();
                }

                bool WasViewInjected(View view) {
                    return _visitorMap.TryGetValue(view, out var result) && result == this;
                }
            }

            public void UpdateHandler() {
                while (_markedViews != null && _markedViews.Count > 0) {
                    (_cachedMarkedViews, _markedViews) = (_markedViews, _cachedMarkedViews);
                    foreach (var view in _cachedMarkedViews) {
                        var container = GetModelContainer(view);
                        foreach (Transform child in view.transform) {
                            RecursiveInject(child, container, false, child.GetComponents<View>());
                        }
                    }

                    _cachedMarkedViews.Clear();
                }
            }

            private DIContainer GetModelContainer(View view) {
                return view.diContainerPointer.ToContainer();
            }

            public void MarkForInjection(View view) {
                _markedViews ??= new HashSet<View>();
                _markedViews.Add(view);
                ViewUpdateManager.AddHandler(this);
            }

            public static ViewTreeVisitor GetVisitor(View view) {
                _visitorMap.TryGetValue(view, out var result);
                return result;
            }
        }

        public DIContainer.ScopePointer diContainerPointer { get; set; }

#if UNITY_EDITOR
        public static event Action<View, IReadOnlyList<IViewModel>> onBind; 
#endif
    }

    public class View<TViewModel> : View, IViewModelHolder where TViewModel : IViewModel {
        public bool hasViewModel => viewModel != null;
        protected TViewModel viewModel;
        private Subject<TViewModel> _disposeSubject = new();
        private bool _pendingInitCall = false;
        private Dictionary<Type,IBinding> _childrenBindings;
        private IDisposable _viewModelSubscription;
        /// <summary>
        /// Observable of the bound view model.
        /// </summary>
        /// Initialized after View.OnInjection() that can be utilized for reactively binding child view models.
        /// <example>
        /// ```csharp
        /// diContainer.BindReactive(_viewModelObservable, vm => vm.someViewModel);
        /// 
        /// diContainer.BindSequenceReactive(_viewModelObservable, vm => vm.someViewModelList);
        /// ```
        /// </example>
        protected IReactiveValue<TViewModel> viewModelObservable { get; private set; }

        public void AssignNewViewModel(TViewModel viewModel) {
            if (hasViewModel) DisposeViewModel();
            this.viewModel = viewModel;

            if (viewModel != null) {
                // delay view model init to allow an external controller to set the view inactive when no valid view model is provided
                if (gameObject.activeInHierarchy) {
                    InitViewModel();
                    _pendingInitCall = false;
                } else _pendingInitCall = true;
            }

            if (_childrenBindings != null) {
                foreach (var (childViewModel, vmType) in ViewModelUtils.GetChildrenViewModels(viewModel)) {
                    _childrenBindings[vmType].Rebind(childViewModel);
                }
            }
        }

        public override Type viewModelType => typeof(TViewModel);

        protected IObservable<TViewModel> disposeObservable => _disposeSubject;

        protected override void OnInject(DIContainer diContainer) {
            IReactiveValue<TViewModel> viewModelObservable;
            if (ViewUtils.ShouldDisableOnMissingViewModel(this)) {
                if (!diContainer.TryObserve(out viewModelObservable)) {
                    enabled = false;
                    return;
                }
            } else
                viewModelObservable = diContainer.Observe<TViewModel>();

            TViewModel vModel = viewModelObservable.Value;

            _childrenBindings?.Clear();

            foreach (var (_, vmType) in ViewModelUtils.GetChildrenViewModels(vModel)) {
                _childrenBindings ??= new Dictionary<Type, IBinding>();
                _childrenBindings.Add(vmType, diContainer.CreateBinding(vmType));
            }

            this.viewModelObservable = viewModelObservable;
        }

        protected override void OnPostInject() {
            _viewModelSubscription = viewModelObservable.Subscribe(AssignNewViewModel);
        }

        private void InitViewModel() {
            OnViewModelInit();
        }

        protected virtual void OnViewModelInit() {}
        /// <summary>
        /// Called whenever this view is about to be disposed or change a view model, where the view model is supposed to be unsubscribed from.
        /// </summary>
        protected virtual void OnViewModelDisposed() {}

        protected virtual void OnEnable() {
            if (_pendingInitCall) {
                if (viewModel != null) {
                    InitViewModel();
                }
                _pendingInitCall = false;
            }
        }

        protected virtual void OnDisable() {
        }

        protected virtual void OnDestroy() {
            if (viewModel != null) DisposeViewModel();
            if (_viewModelSubscription != null) {
                _viewModelSubscription.Dispose();
                _viewModelSubscription = null;
            }
        }

        // receives broadcast message from outside
        protected override void OnViewRecycled() {
            DisposeViewModel();
            if (_viewModelSubscription != null) {
                _viewModelSubscription.Dispose();
                _viewModelSubscription = null;
            }
        }

        protected void DisposeViewModel() {
            _disposeSubject.OnNext(viewModel);
            OnViewModelDisposed();
            viewModel = default;
        }

        /// <summary>
        /// An helper function that just re-initialize the view model again
        /// </summary>
        public void SetViewModel(TViewModel viewModel) {
            if (viewModel != null) DisposeViewModel();
            this.viewModel = viewModel;
            OnViewModelInit();
        }
    }

    public class ContainerView<TViewModel, TChildViewModel> : View<TViewModel>
        where TViewModel : IListViewModel<TChildViewModel>
        where TChildViewModel : IViewModel {
        protected ISequenceBinding<TChildViewModel> childBindings;

        protected override void OnInject(DIContainer diContainer) {
            childBindings = diContainer.CreateSequenceBinding<TChildViewModel>();
            base.OnInject(diContainer);
        }

        protected override void OnViewModelInit() {
            childBindings.RebindAll(viewModel?.childViewModels ?? Enumerable.Empty<TChildViewModel>());
        }
    }

    public interface IViewModelHolder {
        bool hasViewModel { get; }
    }
}