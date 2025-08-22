using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nela.Ramify {
    internal class ViewUpdateManager : MonoBehaviour {
        private static HashSet<IViewUpdateHandler> _updateHandlers = new HashSet<IViewUpdateHandler>();
        private HashSet<IViewUpdateHandler> _cachedHandlers = new HashSet<IViewUpdateHandler>();

        public static void AddHandler(IViewUpdateHandler updateHandler) {
            _updateHandlers.Add(updateHandler);
        }

        private void LateUpdate() {
            int times = 0;
            while (_updateHandlers.Count > 0) {
                times++;
                (_cachedHandlers, _updateHandlers) = (_updateHandlers, _cachedHandlers);
                foreach (var handler in _cachedHandlers) {
                    handler.UpdateHandler();
                }

                _cachedHandlers.Clear();
                if (times > 10000) throw new Exception("Too many updates!");
            }
        }

        [RuntimeInitializeOnLoadMethod]
        private static void OnInit() {
            var go = new GameObject("_viewUpdateManager");
            go.AddComponent<ViewUpdateManager>();
            go.hideFlags = HideFlags.HideAndDontSave;
        }
    }

    internal interface IViewUpdateHandler {
        void UpdateHandler();
    }
}