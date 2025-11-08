using System.Collections.Generic;

namespace Nela.Ramify {
    public class ListViewModel<T> : IListViewModel<T> where T : IViewModel {
        public IReadOnlyList<T> childViewModels { get; }
        public ListViewModel(IReadOnlyList<T> childViewModels) {
            this.childViewModels = childViewModels;
        }
    }
}