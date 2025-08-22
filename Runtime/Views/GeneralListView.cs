using System;
using UnityEngine;

namespace Nela.Ramify {
    /// <summary>
    /// List view for general purpose.
    /// </summary>
    [AddComponentMenu("Ramify/List View")]
    public class GeneralListView : View {
        [SerializeField]
        private View _childViewPrefab;

        [SerializeField]
        private Transform _container;

        public override Type viewModelType => ViewModelUtils.ToContainerType(_childViewPrefab.viewModelType);

        protected override void OnInject(DIContainer diContainer) {
            var viewModel = (IListViewModel<IViewModel>)diContainer.Resolve(viewModelType);
            diContainer.BindSequence(_childViewPrefab.viewModelType, viewModel.childViewModels);

            foreach (var _ in viewModel.childViewModels) {
                Instantiate(_childViewPrefab, _container);
            }
        }
    }
}