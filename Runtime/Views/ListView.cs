using System.Collections.Generic;
using UnityEngine;

namespace Nela.Ramify {
    public class ListView<TViewModel, TChildViewModel, TChildView> : ContainerView<TViewModel, TChildViewModel>
        where TViewModel : IListViewModel<TChildViewModel>
        where TChildViewModel : IViewModel
        where TChildView : View {
        [SerializeField]
        private TChildView _itemPrefab;
        [SerializeField]
        private Transform _container;

        protected List<TChildView> _items = new List<TChildView>();

        public IReadOnlyCollection<TChildView> items => _items;

        protected override void OnViewModelInit() {
            base.OnViewModelInit();

            InitItems();
        }

        protected void AppendNewItem() {
            _items.Add(CreateNewItem(_items.Count));
        }

        protected virtual void InitItems() {
            if (viewModel == null) return;
            for (int i = 0; i < viewModel.childViewModels.Count; i++) {
                if (i >= _items.Count)
                    _items.Add(CreateNewItem(i));
            }
        }

        protected virtual TChildView CreateNewItem(int itemIndex) {
            var item = InstantiateView(_itemPrefab, _container);
            OnNewItemInstantiated(item);
            return item;
        }

        protected virtual void OnNewItemInstantiated(TChildView item) {}
    }
}