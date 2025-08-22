using System.Collections.Generic;

namespace Nela.Ramify {
    public interface IViewModel {
    }

    public interface IViewModelRouter {
        IEnumerable<IViewModel> viewModels { get; }
    }

    /// <summary>
    /// In order to let the View bind the member `viewModel` automatically, this should be implemented by the interface derived from `IViewModel` instead of by the derived class
    /// </summary>
    public interface IViewModelProvider<TViewModel> where TViewModel : IViewModel {
        TViewModel viewModel { get; }
    }

    public interface IListViewModel<out TChildViewModel> : IViewModel where TChildViewModel : IViewModel {
        IReadOnlyList<TChildViewModel> childViewModels { get; }
    }

    public interface IChangeableListViewModel<out TChildViewModel> : IListViewModel<TChildViewModel>
        where TChildViewModel : IViewModel {
        event System.Action onChildrenListChanged;
    }

    public interface IDictionaryViewModel<TKey, out TChildViewModel> : IViewModel where TChildViewModel : IViewModel {
        TChildViewModel GetViewModel(TKey key);
    }
}