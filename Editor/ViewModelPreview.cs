using System.Collections.Generic;
using System.Linq;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace Nela.Ramify {
    [CustomPreview(typeof(GameObject))]
    public class GameObjectViewModelPreview : ObjectPreview {
        private int _displayStartIndex;

        private bool HasViewModelComponents() {
            GameObject gameObject = target as GameObject;
            return gameObject != null && gameObject.GetComponents<Component>().OfType<IViewModel>().Any();
        }

        public override bool HasPreviewGUI() {
            return HasViewModelComponents();
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background) {
            GameObject gameObject = target as GameObject;
            if (gameObject == null) return;

            List<IViewModel> viewModels = gameObject.GetComponents<Component>()
                .OfType<IViewModel>()
                .ToList();

            int viewModelCount = viewModels.Count;

            // Draw background
            EditorGUI.DrawRect(r, new Color(0.2f, 0.2f, 0.2f));

            // Prepare styles
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 14,
                normal = { textColor = Color.white }
            };
            GUIStyle contentStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 12,
                normal = { textColor = Color.white }
            };

            // Draw title
            EditorGUI.LabelField(new Rect(r.x, r.y, r.width, 30), 
                $"ViewModel Components: {viewModelCount}", titleStyle);

            // Draw ViewModel list
            int index = 0;
            float yOffset = 35;
            bool hasMoreToShow = false;
            foreach (var viewModel in viewModels)
            {
                if (yOffset > r.height - 20) {
                    hasMoreToShow = true;
                    break;
                }

                string componentName = viewModel.GetType().Name;
                foreach (var @interface in viewModel.GetType().GetInterfaces().Where(i => typeof(IViewModel).IsAssignableFrom(i))) {
                    if (yOffset > r.height - 20) {
                        hasMoreToShow = true;
                        break;
                    }
                    if (@interface == typeof(IViewModel)) continue;
                    index++;
                    if (index <= _displayStartIndex) continue;

                    EditorGUI.LabelField(new Rect(r.x + 5, r.y + yOffset, r.width - 10, 20), 
                        $"{componentName} : {@interface.GetNiceName()}", contentStyle);
                    yOffset += 20;
                }
            }

            if (Event.current.type == EventType.MouseDown) {
                if (Event.current.button == 0 && hasMoreToShow) {
                    _displayStartIndex = index;
                    EditorUtility.SetDirty(target);
                }

                if (_displayStartIndex != 0 && Event.current.button == 1) {
                    _displayStartIndex = 0;
                    EditorUtility.SetDirty(target);
                }
            }
        }

        public override GUIContent GetPreviewTitle() {
            return new GUIContent("ViewModel Components");
        }
    }
}