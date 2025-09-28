using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Nela.Ramify {
    public class ViewTreeView : TreeView {
        private View _rootView;

        public event Action<IReadOnlyList<View>> onSelectionChanged;
        private List<View> _cachedSelectedViews = new List<View>();
        private List<int> _viewModelSources = new List<int>();

        public ViewTreeView(TreeViewState state, View rootView) : base(state) {
            _rootView = rootView;
        }

        protected override TreeViewItem BuildRoot() {
            var rootBase = new TreeViewItem { id = 0, depth = -1, displayName = "<none>" };
            if (_rootView == null) {
                return rootBase;
            }

            var rootViewItem = new TreeViewItem
            {
                id = _rootView.GetInstanceID(),
                depth = 0,
                displayName = _rootView.name,
                icon = EditorGUIUtility.GetIconForObject(_rootView.gameObject) ?? PrefabUtility.GetIconForGameObject(_rootView.gameObject),
            };

            rootBase.AddChild(rootViewItem);

            AddChildren(_rootView.transform, rootViewItem);

            return rootBase;
        }

        private void AddChildren(Transform parent, TreeViewItem viewItem) {
            foreach (Transform child in parent) {
                var currentViewItem = viewItem;
                var childView = child.GetComponent<View>();
                if (childView != null) {
                    var treeItem = currentViewItem = new TreeViewItem
                    {
                        id = childView.GetInstanceID(),
                        depth = viewItem.depth + 1,
                        displayName = childView.name,
                        icon = EditorGUIUtility.GetIconForObject(childView.gameObject) ?? PrefabUtility.GetIconForGameObject(childView.gameObject),
                    };
                    
                    viewItem.AddChild(treeItem);
                }
                AddChildren(child, currentViewItem);
            }
        }

        public override void OnGUI(Rect rect) {
            _viewModelSources.Clear();
            var selection = GetSelection();
            foreach (var id in selection) {
                var view = EditorUtility.InstanceIDToObject(id) as View;
                if (view != null) {
                    foreach (var info in ViewTreeDebugger.CollectReferences(view)) {
                        _viewModelSources.Add(info.source.GetInstanceID());
                    }
                }
            }
            base.OnGUI(rect);
        }

        protected override void RowGUI(RowGUIArgs args) {
            var symbolRect = args.rowRect;
            symbolRect.xMin = symbolRect.xMax - 20;
            
            var rowRect = args.rowRect;

            if (_viewModelSources.Contains(args.item.id)) {
                GUI.Label(symbolRect, "↴");
                rowRect.xMax -= 20;
            } else {
                var selection = GetSelection();
                var thisView = EditorUtility.InstanceIDToObject(args.item.id) as View;
                if (thisView != null && ViewTreeDebugger.CollectReferences(thisView).Any(r => r.source != null && selection.Contains(r.source.GetInstanceID()))) {
                    GUI.Label(symbolRect, "↵");
                    rowRect.xMax -= 20;
                }
            }

            var originalColor = GUI.color;
            if (ViewTreeDebugger.currentError != null && ViewTreeDebugger.currentError.view.GetInstanceID() == args.item.id)
                GUI.color = Color.red;

            base.RowGUI(args);

            GUI.color = originalColor;

            var view = EditorUtility.InstanceIDToObject(args.item.id);
            if (view != null) {
                var viewModels = ViewTreeDebugger.GetProvidedViewModels(view);
                if (viewModels.Count > 0) {
                    GUILayout.BeginArea(rowRect);
                    using (new GUILayout.HorizontalScope()) {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label($"{viewModels.Count}", GUI.skin.box);
                    }
                    GUILayout.EndArea();
                }
            }
        }

        protected override void SelectionChanged(IList<int> selectedIds) {
            if (onSelectionChanged != null) {
                _cachedSelectedViews.Clear();
                for (int i = 0; i < selectedIds.Count; i++)
                {
                    _cachedSelectedViews.Add(EditorUtility.InstanceIDToObject(selectedIds[i]) as View);
                }

                onSelectionChanged.Invoke(_cachedSelectedViews);
            }
        }

        public void SetViewRoot(View root) {
            if (_rootView == root) return;
            _rootView = root;
            Reload();
        }

        public void ExpandToShow(View selected) {
            var ids = new HashSet<int>(GetExpanded());
            var item = FindItem(selected.GetInstanceID(), rootItem);

            var ancestor = item.parent;
            while (ancestor != null) {
                ids.Add(ancestor.id);
                ancestor = ancestor.parent;
            }

            SetExpanded(ids.ToList());
        }
    }
}