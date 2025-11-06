using System;
using System.Collections.Generic;

namespace Nela.Ramify {
    public static class DIContainerUtils {
        /// <summary>
        /// Helper function to bind a child view model of a reactive value.
        /// </summary>
        /// <seealso cref="View{TViewModel}.viewModelObservable"/>
        public static IBinding<TChild> BindReactive<T, TChild>(this DIContainer diContainer, IObservable<T> observable, Func<T, TChild> selector)
            where TChild : IViewModel {
            var binding = diContainer.CreateBinding<TChild>();
            observable.Subscribe(v => binding.Rebind(selector(v)));
            return binding;
        }

        /// <summary>
        /// Helper function to bind a sequence of child view models of a reactive value.
        /// </summary>
        /// <seealso cref="View{TViewModel}.viewModelObservable"/>
        public static ISequenceBinding<TChild> BindSequenceReactive<T, TChild>(this DIContainer diContainer, IObservable<T> observable,
            Func<T, IEnumerable<TChild>> selector)
            where TChild : IViewModel {
            var binding = diContainer.CreateSequenceBinding<TChild>();
            observable.Subscribe(v => binding.RebindAll(selector(v)));
            return binding;
        }

        /// <summary>
        /// Helper function to bind a child view model of a runtime type.
        /// </summary>
        /// <param name="type">If set to null, it will use the first type of the value.</param>
        /// <remarks>An exception will be thrown if the type of a new value does not match.</remarks>
        public static void BindReactive<T>(this DIContainer diContainer, IObservable<T> observable, Func<T, object> selector, Type type) {
            if (type != null) {
                var binding = diContainer.CreateBinding(type);
                observable.Subscribe(v => binding.Rebind(selector(v)));
            } else {
                IBinding binding = null;
                observable.Subscribe(v => {
                    var cv = selector(v);
                    if (cv != null) {
                        binding ??= diContainer.CreateBinding(cv.GetType());
                        binding.Rebind(cv);
                    }
                });
            }
        }
    }
}