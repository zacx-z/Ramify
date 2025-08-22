using UnityEngine;

namespace Nela.Ramify {
    [DisallowMultipleComponent]
    [AddComponentMenu("MVVM/View Tree Node")]
    public class ViewTreeNode : MonoBehaviour {
        public enum TraverseType {
            Normal,
            Ignore,
            IgnoreWhenInactive
        }

        public TraverseType traverseType => _traverseType;

        [SerializeField]
        private TraverseType _traverseType;
    }
}