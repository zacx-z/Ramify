using System;

namespace Nela.Ramify {
    public interface ICommand : IViewModel {
        bool canExecute { get; }
        void Execute();
    }

    public interface ICommand<in T> : IViewModel {
        bool CanExecute(T arg);
        void Execute(T arg);
    }

    internal class Command : ICommand {
        private readonly Action _callback;
        private readonly Func<bool> _validator;

        public Command(Action callback, Func<bool> validator = null) {
            _callback = callback;
            _validator = validator;
        }

        public bool canExecute => _validator?.Invoke() ?? true;

        public void Execute() {
            _callback();
        }
    }

    internal class Command<T> : ICommand<T> {
        private readonly Action<T> _callback;
        private readonly Func<T, bool> _validator;

        public Command(Action<T> callback) {
            _callback = callback;
        }

        public Command(Action<T> callback, Func<T, bool> validator) {
            _callback = callback;
            _validator = validator;
        }

        public bool CanExecute(T arg) => _validator?.Invoke(arg) ?? true;

        public void Execute(T arg) {
            _callback(arg);
        }
    }

    public static class CommandUtils {
        public static ICommand Create(Action callback) {
            return new Command(callback);
        }

        public static ICommand Create(Action callback, Func<bool> validator) {
            return new Command(callback, validator);
        }

        public static ICommand<T> Create<T>(Action<T> callback) {
            return new Command<T>(callback);
        }

        public static ICommand<T> Create<T>(Action<T> callback, Func<T, bool> validator) {
            return new Command<T>(callback, validator);
        }

        public static ICommand<(T1, T2)> Create<T1, T2>(Action<T1, T2> callback) {
            return new Command<(T1, T2)>(t => callback(t.Item1, t.Item2));
        }

        public static ICommand CreateNonExecutable() {
            return new NonExecutableCommand();
        }

        public class NonExecutableCommand : ICommand {
            public bool canExecute => false;
            public void Execute() {
                throw new InvalidOperationException();
            }
        }
    }
}