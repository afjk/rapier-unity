using AFJK.Rapier;
using UnityEditor;
using UnityEngine;

namespace AFJK.Rapier.Editor
{
    public static class RapierEditorUtility
    {
        private static readonly string[] CollisionDetectionOptions = { "Discrete", "Continuous" };

        private static readonly string[] GroupNames =
        {
            "Group 0", "Group 1", "Group 2", "Group 3",
            "Group 4", "Group 5", "Group 6", "Group 7",
            "Group 8", "Group 9", "Group 10", "Group 11",
            "Group 12", "Group 13", "Group 14", "Group 15",
        };

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

        public static void AutoResolveRigidBody(SerializedProperty rbProp)
        {
            if (rbProp.objectReferenceValue != null)
            {
                return;
            }

            if (rbProp.serializedObject.targetObjects.Length != 1)
            {
                return;
            }

            var comp = rbProp.serializedObject.targetObject as Component;
            if (comp == null)
            {
                return;
            }

            var rb = comp.GetComponentInParent<RapierRigidbody>();
            if (rb != null)
            {
                rbProp.objectReferenceValue = rb;
            }
        }

        public static void HalfExtentsAsSizeField(SerializedProperty halfExtentsProp, string label = "Size")
        {
            EditorGUI.showMixedValue = halfExtentsProp.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var size = EditorGUILayout.Vector3Field(label, halfExtentsProp.vector3Value * 2f);
            if (EditorGUI.EndChangeCheck())
            {
                halfExtentsProp.vector3Value = size * 0.5f;
            }

            EditorGUI.showMixedValue = false;
        }

        public static void HalfValueAsFullField(SerializedProperty halfProp, string label)
        {
            EditorGUI.showMixedValue = halfProp.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var full = EditorGUILayout.FloatField(label, halfProp.floatValue * 2f);
            if (EditorGUI.EndChangeCheck())
            {
                halfProp.floatValue = full * 0.5f;
            }

            EditorGUI.showMixedValue = false;
        }

        public static void GroupMaskField(SerializedProperty ushortProp, string label)
        {
            EditorGUI.showMixedValue = ushortProp.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int mask = ushortProp.intValue & 0xFFFF;
            int result = EditorGUILayout.MaskField(label, mask, GroupNames);
            if (EditorGUI.EndChangeCheck())
            {
                ushortProp.intValue = result & 0xFFFF;
            }

            EditorGUI.showMixedValue = false;
        }

        public static void PackedGroupMaskField(SerializedProperty uintProp)
        {
            EditorGUI.showMixedValue = uintProp.hasMultipleDifferentValues;
            long packed = uintProp.longValue & 0xFFFFFFFFL;
            int memberships = (int)((packed >> 16) & 0xFFFF);
            int filter = (int)(packed & 0xFFFF);
            EditorGUI.BeginChangeCheck();
            int newMemberships = EditorGUILayout.MaskField("Memberships", memberships, GroupNames);
            int newFilter = EditorGUILayout.MaskField("Filter", filter, GroupNames);
            if (EditorGUI.EndChangeCheck())
            {
                long newPacked = (((long)(newMemberships & 0xFFFF)) << 16) | (long)(newFilter & 0xFFFF);
                uintProp.longValue = newPacked;
            }

            EditorGUI.showMixedValue = false;
        }
    }
}
