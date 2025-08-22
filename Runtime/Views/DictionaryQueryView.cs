using UnityEngine;

namespace Nela.Ramify {
    public class DictionaryQueryView<T> : View<IDictionaryViewModel<T, IViewModel>> {
        [SerializeField]
        private T _query;

        protected override void OnInject(DIContainer diContainer) {
            base.OnInject(diContainer);
            var vm = viewModel.GetViewModel(_query);
            diContainer.Bind(vm, vm.GetType());
        }
    }
}