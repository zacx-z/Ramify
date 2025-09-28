using UnityEngine;
using UnityEngine.Serialization;

namespace Nela.Ramify {
    [DisallowMultipleComponent]
    [AddComponentMenu("MVVM/Injection Options")]
    public class InjectionOptions : MonoBehaviour {
        public enum InjectionType {
            Normal,
            Ignore,
            IgnoreWhenInactive,
            /// <summary>
            /// Stop injection from the node unless it is the root node of the current binding target
            /// </summary>
            OnlyAsBindingRoot
        }

        public InjectionType injectionType => _injectionType;

        [FormerlySerializedAs("_traverseType")] [SerializeField]
        private InjectionType _injectionType;
    }
}