using System.Collections.Generic;

namespace Nela.Ramify {
    public interface IBinding {
        void Rebind(object value);
    }

    public interface IBinding<T> {
        void Rebind(T value);
    }

    public interface ISequenceBinding<in T> {
        void RebindItem(int index, T value);
        void RebindAll(IEnumerable<T> values);
    }
}