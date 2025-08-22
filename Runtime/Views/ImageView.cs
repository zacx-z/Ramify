using UnityEngine;
using UnityEngine.UI;

namespace Nela.Ramify {
    [AddComponentMenu("Ramify/Image View")]
    public class ImageView : View<IImageViewModel> {
        [SerializeField]
        private Image _image;

        protected override void OnViewModelInit() {
            _image.sprite = viewModel.sprite;
        }
    }

    public interface IImageViewModel : IViewModel {
        Sprite sprite { get; }
    }
}