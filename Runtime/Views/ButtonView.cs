using UnityEngine;
using UnityEngine.UI;

namespace Nela.Ramify {
    [AddComponentMenu("Ramify/Button View")]
    public class ButtonView : View<ICommand> {
        [SerializeField]
        private Button _button;

        protected override void OnViewModelInit() {
            _button.onClick.AddListener(viewModel.Execute);
            UpdateView();
        }

        void Update() {
            UpdateView();
        }

        protected override void OnViewModelDisposed() {
            _button.onClick.RemoveListener(viewModel.Execute);
        }

        private void UpdateView() {
            _button.interactable = viewModel.canExecute;
        }
    }
}