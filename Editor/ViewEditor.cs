using System;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Nela.Ramify {
    [CustomEditor(typeof(View), true)]
    public class ViewEditor : OdinEditor {
        public static IViewTreeWindow currentViewTreeWindow { get; set; }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            if (Application.isPlaying) {
                var view = target as View;
                if (view is IViewModelHolder v) {
                    GUILayout.Label(v.hasViewModel ? "Initialized" : "Uninitialized");
                }

                if (currentViewTreeWindow == null) {
                    using (new EditorGUI.DisabledScope(!ViewTreeDebugger.IsViewBound(view))) {
                        if (GUILayout.Button("Inspect Bindings")) {
                            ViewTreeWindow.OpenViewTreeNode(view);
                        }
                    }
                }

                var references = ViewTreeDebugger.CollectReferences(view);
                if (references.Count > 0) {
                    GUILayout.Label("View Model References", UnityEditor.EditorStyles.boldLabel);
                    foreach (var refInfo in references) {
                        using (new GUILayout.HorizontalScope()) {
                            GUILayout.Label($"{refInfo.viewModel}");
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button(refInfo.source.name, EditorStyles.activeBinding)) {
                                if (currentViewTreeWindow != null) {
                                    currentViewTreeWindow.Select(refInfo.source);
                                } else {
                                    Selection.activeObject = refInfo.source;
                                }
                            }
                        }
                    }
                }

                var viewModels = ViewTreeDebugger.GetProvidedViewModels(view);
                if (viewModels.Count > 0) {
                    GUILayout.Label("View Models Provided", UnityEditor.EditorStyles.boldLabel);
                    foreach (var vm in viewModels) {
                        GUILayout.Label($"{(vm == null ? "<none>" : vm.ToString())}");
                    }
                }

                var boundViewModels = ViewTreeDebugger.GetBoundViewModels(view);
                if (boundViewModels.Count > 0) {
                    GUILayout.Label("Bound ViewModels", UnityEditor.EditorStyles.boldLabel);
                    foreach (var vm in boundViewModels) {
                        GUILayout.Label($"{(vm == null ? "<none>" : vm.ToString())}");
                    }
                }
            }

            if (ViewTreeDebugger.currentError != null && ViewTreeDebugger.currentError.view == target) {
                using (new GUILayout.VerticalScope(EditorStyles.errorInfo)) {
                    var originalColor = GUI.color;
                    GUI.color = Color.red;
                    GUILayout.Label(ExceptionToString(ViewTreeDebugger.currentError.exception));
                    GUI.color = originalColor;

                    if (GUILayout.Button(ViewTreeDebugger.currentError.context.name, UnityEditor.EditorStyles.miniButton)) {
                        Selection.activeObject = ViewTreeDebugger.currentError.context;
                    }
                }
            }

            string ExceptionToString(Exception exception) {
                if (exception is ViewModelExceptionBase viewModelException) {
                    return $"Missing {viewModelException.viewModelType}";
                }
                
                return exception.Message;
            }
        }
    }
}