using UnityEngine;
using UnityEngine.UI;

namespace Nela.Ramify {
    [AddComponentMenu("Ramify/Label View")]
    public class LabelView : View<ILabelViewModel> {
        [SerializeField]
        private Text _label;

        protected override void OnViewModelInit() {
            _label.text = viewModel.label;
        }
    }

    public interface ILabelViewModel : IViewModel {
        string label { get; }
    }
}