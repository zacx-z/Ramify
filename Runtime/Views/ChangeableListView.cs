namespace Nela.Ramify {
    public class ChangeableListView<TViewModel, TChildViewModel, TChildView> : ListView<TViewModel, TChildViewModel, TChildView>
        where TViewModel : IChangeableListViewModel<TChildViewModel>
        where TChildViewModel : IViewModel
        where TChildView : View {
        protected override void OnViewModelInit() {
            base.OnViewModelInit();
            viewModel.onChildrenListChanged += OnListChanged;
        }

        protected override void OnViewModelDisposed() {
            base.OnViewModelDisposed();
            viewModel.onChildrenListChanged -= OnListChanged;
        }

        private void OnListChanged() {
            childBindings.RebindAll(viewModel.childViewModels);
            InitItems();
            OnChildrenChanged();
        }

        protected override void InitItems() {
            int currentIndex = 0;
            foreach (var elem in viewModel.childViewModels) {
                if (currentIndex < _items.Count) {
                    if (_items[currentIndex] == null) {
                        _items[currentIndex] = CreateNewItem(currentIndex);
                    }
                } else {
                    _items.Add(CreateNewItem(currentIndex));
                }

                _items[currentIndex].gameObject.SetActive(true);
                currentIndex++;
            }

            for (; currentIndex < _items.Count; currentIndex++) {
                if (_items[currentIndex] != null) {
                    _items[currentIndex].gameObject.SetActive(false);
                }
            }
        }

        protected virtual void OnChildrenChanged() {}
    }
}