using UnityEngine;

namespace Nela.Ramify {
    public class DictionaryQueryView<T> : View<IDictionaryViewModel<T, IViewModel>> {
        [SerializeField]
        private T _query;

        protected override void OnInject(DIContainer diContainer) {
            base.OnInject(diContainer);
            diContainer.BindReactive(viewModelObservable, vm => vm.GetViewModel(_query), null);
        }
    }
}