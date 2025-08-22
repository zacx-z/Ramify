using UnityEngine;

namespace Nela.Ramify {
    public class ViewBinder : MonoBehaviour {
        [SerializeField]
        private View _targetView;
        [SerializeField]
        private GameObject _model;

        [SerializeField]
        [Tooltip("Only bind when the model is active. This is to ensure the model is initialized.")]
        private bool _bindOnModelActivation = true;

        private bool _delayBind = false;

        private void Start() {
            if (_bindOnModelActivation && !_model.activeInHierarchy) {
                _delayBind = true;
            } else {
                _targetView.BindAll(_model);
            }
        }

        private void Update() {
            if (_delayBind) {
                if (_model.activeInHierarchy) {
                    _targetView.BindAll(_model);
                    _delayBind = false;
                }
            }
        }
    }
}