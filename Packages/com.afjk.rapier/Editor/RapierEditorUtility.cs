using AFJK.Rapier;
using UnityEditor;
using UnityEngine;

namespace AFJK.Rapier.Editor
{
    public static class RapierEditorUtility
    {
        private static readonly string[] CollisionDetectionOptions = { "Discrete", "Continuous" };

        public static bool AdvancedFoldout(string sessionKey, string label = "Advanced")
        {
            var expanded = SessionState.GetBool(sessionKey, false);
            var newExpanded = EditorGUILayout.Foldout(expanded, label, true);
            if (newExpanded != expanded)
            {
                SessionState.SetBool(sessionKey, newExpanded);
            }

            return newExpanded;
        }

        public static void CollisionDetectionPopup(SerializedProperty ccdBoolProp, string label = "Collision Detection")
        {
            EditorGUI.showMixedValue = ccdBoolProp.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var selected = EditorGUILayout.Popup(label, ccdBoolProp.boolValue ? 1 : 0, CollisionDetectionOptions);
            if (EditorGUI.EndChangeCheck())
            {
                ccdBoolProp.boolValue = (selected == 1);
            }

            EditorGUI.showMixedValue = false;
        }

        public static void ConstraintsGrid(
            SerializedProperty tx, SerializedProperty ty, SerializedProperty tz,
            SerializedProperty rx, SerializedProperty ry, SerializedProperty rz)
        {
            EditorGUILayout.LabelField("Constraints", EditorStyles.boldLabel);
            DrawAxisRow("Freeze Position", tx, ty, tz);
            DrawAxisRow("Freeze Rotation", rx, ry, rz);
        }

        private static void DrawAxisRow(string label, SerializedProperty x, SerializedProperty y, SerializedProperty z)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth));

            DrawAxisToggle("X", x);
            DrawAxisToggle("Y", y);
            DrawAxisToggle("Z", z);

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawAxisToggle(string axisLabel, SerializedProperty prop)
        {
            EditorGUI.showMixedValue = prop.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var value = EditorGUILayout.ToggleLeft(axisLabel, prop.boolValue, GUILayout.Width(30f));
            if (EditorGUI.EndChangeCheck())
            {
                prop.boolValue = value;
            }

            EditorGUI.showMixedValue = false;
        }

        public static void AutoResolveWorld(SerializedProperty worldProp)
        {
            if (worldProp.objectReferenceValue != null)
            {
                return;
            }

            if (worldProp.serializedObject.targetObjects.Length != 1)
            {
                return;
            }

            var worlds = Object.FindObjectsByType<RapierWorldBehaviour>(FindObjectsSortMode.None);
            if (worlds.Length == 1)
            {
                worldProp.objectReferenceValue = worlds[0];
            }
        }
    }
}
