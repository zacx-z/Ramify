using UnityEngine;

namespace Nela.Ramify {
    public static class EditorStyles {
        public static readonly GUIStyle activeBinding;
        public static readonly GUIStyle errorInfo;

        static EditorStyles() {
            activeBinding = new GUIStyle(UnityEditor.EditorStyles.miniButton);
            activeBinding.normal.textColor = Color.cyan;

            errorInfo = new GUIStyle(UnityEditor.EditorStyles.helpBox);
            errorInfo.normal.textColor = Color.red;
            errorInfo.onNormal.textColor = Color.green;
        }
    }
}