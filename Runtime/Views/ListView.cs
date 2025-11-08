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

        public IReadOnlyList<TChildView> items => _items;

        protected override void OnInject(DIContainer diContainer) {
            ClearItems(); // to allow it to be reused
            base.OnInject(diContainer);
        }

        protected override void OnViewModelInit() {
            base.OnViewModelInit();

            InitItems();
        }

        protected virtual void InitItems() {
            if (viewModel == null) return;
            int i = 0;
            for (; i < viewModel.childViewModels.Count; i++) {
                if (i >= _items.Count)
                    _items.Add(CreateNewItem(i));
                else
                    _items[i].gameObject.SetActive(true);
            }

            for (; i < _items.Count; i++) {
                _items[i].gameObject.SetActive(false);
            }
        }

        protected virtual void ClearItems() {
            foreach (var item in _items) {
                DestroyImmediate(item.gameObject);
            }
            _items.Clear();
        }

        protected virtual TChildView CreateNewItem(int itemIndex) {
            var item = InstantiateView(_itemPrefab, _container);
            OnNewItemInstantiated(item);
            return item;
        }

        protected virtual void OnNewItemInstantiated(TChildView item) {}
    }
}