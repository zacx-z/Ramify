using System;
using System.Collections.Generic;

namespace Nela.Ramify {
    public interface IReactiveValue<out T> : IObservable<T> {
        public T Value { get; }
    }

    internal class ReactiveValue<T> : IReactiveValue<T> {
        private LinkedList<IObserver<T>> _observers = new LinkedList<IObserver<T>>();

        public ReactiveValue(T value) {
            Value = value;
        }

        public void OnNext(T value) {
            Value = value;
            for (var p = _observers.First; p != null; p = p.Next) {
                p.Value.OnNext(value);
            }
        }

        public void OnCompleted() {
            for (var p = _observers.First; p != null; p = p.Next) {
                p.Value.OnCompleted();
            }
        }

        public IDisposable Subscribe(IObserver<T> observer) {
            var node = _observers.AddLast(observer); // first registered, first notified
            observer.OnNext(Value);
            return new ObserverSubscription<T>(node);
        }

        public T Value { get; private set; }
    }

    internal class Subject<T> : IObservable<T> {
        private LinkedList<IObserver<T>> _observers = new LinkedList<IObserver<T>>();

        public IDisposable Subscribe(IObserver<T> observer) {
            var node = _observers.AddLast(observer); // first registered, first notified
            return new ObserverSubscription<T>(node);
        }

        public void OnNext(T value) {
            for (var p = _observers.First; p != null; p = p.Next) {
                p.Value.OnNext(value);
            }
        }
    }

    internal class ObserverSubscription<T> : IDisposable {
        private readonly LinkedListNode<IObserver<T>> _node;

        public ObserverSubscription(LinkedListNode<IObserver<T>> node) {
            _node = node;
        }

        public void Dispose() {
            _node.List.Remove(_node);
        }
    }

    internal static class ReactiveUtils {
        public static IDisposable Subscribe<T>(this IObservable<T> observable, Action<T> subscriberCallback) {
            return observable.Subscribe(new CallbackObserver<T>(subscriberCallback));
        }

        private class CallbackObserver<T> : IObserver<T> {
            private readonly Action<T> _callback;

            public CallbackObserver(Action<T> callback) {
                _callback = callback;
            }

            public void OnCompleted() {
            }

            public void OnError(Exception error) {
            }

            public void OnNext(T value) {
                _callback(value);
            }
        }
    }
}