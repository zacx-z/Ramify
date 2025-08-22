using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UniRx;
using Object = UnityEngine.Object;

namespace Nela.Ramify {
    [InitializeOnLoad]
    public class ViewTreeDebugger {
        public struct ReferenceInfo {
            public object viewModel;
            public Object source;
        }

        private static readonly Dictionary<View, List<ReferenceInfo>> _referenceMap = new Dictionary<View, List<ReferenceInfo>>();
        private static readonly Dictionary<Object, List<object>> _providedViewModels = new Dictionary<Object, List<object>>();
        private static readonly Dictionary<View, IReadOnlyList<IViewModel>> _boundViewModels = new Dictionary<View, IReadOnlyList<IViewModel>>();

        static ViewTreeDebugger() {
            DIContainer.onResolveFor += AddReference;
            DIContainer.onResolveObservableFor += AddReferenceForObservable;
            View.onBind += OnBind;

            currentError = null;
            Logger.loggers.Add(new DebugLogger());
        }

        private static void AddReference(object viewModel, View view, Object source) {
            if (!_referenceMap.TryGetValue(view, out var list)) {
                list = new List<ReferenceInfo>();
                _referenceMap.Add(view, list);
            }
            list.Add(new ReferenceInfo()
            {
                viewModel = viewModel,
                source = source,
            });

            if (source != null) {
                if (!_providedViewModels.TryGetValue(source, out var mList)) {
                    mList = new List<object>();
                    _providedViewModels.Add(source, mList);
                }

                mList.Add(viewModel);
            }
        }

        private static void OnBind(View view, IReadOnlyList<IViewModel> viewModels) {
            _boundViewModels[view] = viewModels.ToList();
        }

        private static void AddReferenceForObservable(IObservable<object> viewModelObservable, View view, Object source) {
            viewModelObservable.Subscribe(viewModel => {
                AddReference(viewModel, view, source);
            });
        }

        public static IReadOnlyList<ReferenceInfo> CollectReferences(View view) {
            if (_referenceMap.TryGetValue(view, out var list)) {
                return list;
            }

            return Array.Empty<ReferenceInfo>();
        }

        public static bool IsViewBound(View view) {
            return view.HasBindings();
        }

        public static IReadOnlyList<object> GetProvidedViewModels(Object source) {
            if (_providedViewModels.TryGetValue(source, out var list)) {
                return list;
            }

            return Array.Empty<object>();
        }

        /// <summary>
        /// Get the view models directly bound to the view
        /// </summary>
        public static IReadOnlyList<IViewModel> GetBoundViewModels(View view) {
            if (_boundViewModels.TryGetValue(view, out var list)) {
                return list;
            }

            return Array.Empty<IViewModel>();
        }

        public static ErrorInfo currentError { get; set; }

        private class DebugLogger : ILogger {
            public void Log(Exception exception, View view, Object context) {
                currentError = new ErrorInfo()
                {
                    exception = exception,
                    view = view,
                    context = context,
                };
            }
        }

        public class ErrorInfo {
            public Exception exception;
            public View view;
            public Object context;
        }
    }
}