using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Nela.Ramify {
    public static class ViewModelUtils {
        public static Type ToContainerType(Type viewModelType) {
            return typeof(IListViewModel<>).MakeGenericType(viewModelType);
        }

        public static IEnumerable<IViewModel> GetAllViewModels(GameObject gameObject) {
            foreach (var comp in gameObject.GetComponents<IViewModel>()) {
                yield return comp;
            }

            foreach (var router in gameObject.GetComponents<IViewModelRouter>()) {
                foreach (var v in router.viewModels) {
                    yield return v;
                }
            }
        }

        public static IEnumerable<(IViewModel, Type)> GetChildrenViewModels<T>(T viewModel) where T : IViewModel {
            return GetChildrenViewModels(typeof(T), viewModel);
        }

        public static IEnumerable<(IViewModel, Type)> GetChildrenViewModels(Type viewModelType, object viewModel) {
            foreach (var @interface in viewModelType.GetInterfaces()) {
                if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IViewModelProvider<>)) {
                    var childType = @interface.GenericTypeArguments[0];
                    var childViewModel = viewModel == null ? null : (IViewModel)@interface
                        .GetProperty("viewModel", BindingFlags.Instance | BindingFlags.Public)
                        .GetValue(viewModel);
                    yield return (childViewModel, childType);
                    if (childViewModel != null) {
                        // beware! if there is a cyclic reference this will end up with infinite recursion finally resulting in stack overflow
                        foreach (var descendent in GetChildrenViewModels(childType, childViewModel)) {
                            yield return descendent;
                        }
                    }
                }
            }
        }

        public static IListViewModel<T> ToViewModel<T>(this List<T> list) where T : IViewModel {
            return new ListViewModel<T>(list);
        }

        private class ListViewModel<T> : IListViewModel<T> where T : IViewModel {
            private readonly List<T> _children = new List<T>();

            public IReadOnlyList<T> childViewModels => _children;

            public ListViewModel(List<T> list) {
                _children = list;
            }
        }
    }
}