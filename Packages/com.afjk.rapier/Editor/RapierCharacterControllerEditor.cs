using AFJK.Rapier;
using UnityEditor;
using UnityEngine;

namespace AFJK.Rapier.Editor
{
    [CustomEditor(typeof(RapierCharacterControllerBehaviour))]
    [CanEditMultipleObjects]
    public class RapierCharacterControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var rigidBody = serializedObject.FindProperty("rigidBody");
            var registerBodyOnEnable = serializedObject.FindProperty("registerBodyOnEnable");
            var shapeType = serializedObject.FindProperty("shapeType");
            var halfExtents = serializedObject.FindProperty("halfExtents");
            var radius = serializedObject.FindProperty("radius");
            var halfHeight = serializedObject.FindProperty("halfHeight");
            var up = serializedObject.FindProperty("up");
            var offset = serializedObject.FindProperty("offset");
            var slide = serializedObject.FindProperty("slide");
            var autostepEnabled = serializedObject.FindProperty("autostepEnabled");
            var autostepMaxHeight = serializedObject.FindProperty("autostepMaxHeight");
            var autostepMinWidth = serializedObject.FindProperty("autostepMinWidth");
            var autostepIncludeDynamicBodies = serializedObject.FindProperty("autostepIncludeDynamicBodies");
            var maxSlopeClimbAngle = serializedObject.FindProperty("maxSlopeClimbAngle");
            var minSlopeSlideAngle = serializedObject.FindProperty("minSlopeSlideAngle");
            var snapToGroundEnabled = serializedObject.FindProperty("snapToGroundEnabled");
            var snapToGroundDistance = serializedObject.FindProperty("snapToGroundDistance");
            var normalNudgeFactor = serializedObject.FindProperty("normalNudgeFactor");
            var filterFlags = serializedObject.FindProperty("filterFlags");
            var useCollisionGroups = serializedObject.FindProperty("useCollisionGroups");
            var collisionGroups = serializedObject.FindProperty("collisionGroups");
            var excludeOwnBody = serializedObject.FindProperty("excludeOwnBody");

            RapierEditorUtility.AutoResolveRigidBody(rigidBody);

            // Common
            EditorGUILayout.LabelField("Shape", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(shapeType, new GUIContent("Shape"));

            if (shapeType.hasMultipleDifferentValues)
            {
                EditorGUILayout.PropertyField(radius, new GUIContent("Radius"));
                EditorGUILayout.PropertyField(halfHeight, new GUIContent("Half Height"));
                EditorGUILayout.PropertyField(halfExtents, new GUIContent("Half Extents"));
            }
            else
            {
                switch ((RapierShapeType)shapeType.intValue)
                {
                    case RapierShapeType.Ball:
                        EditorGUILayout.PropertyField(radius, new GUIContent("Radius"));
                        break;
                    case RapierShapeType.Cuboid:
                        RapierEditorUtility.HalfExtentsAsSizeField(halfExtents);
                        break;
                    default:
                        EditorGUILayout.PropertyField(radius, new GUIContent("Radius"));
                        RapierEditorUtility.HalfValueAsFullField(halfHeight, "Height");
                        break;
                }
            }

            EditorGUILayout.PropertyField(maxSlopeClimbAngle, new GUIContent("Slope Limit"));
            EditorGUILayout.PropertyField(offset, new GUIContent("Skin Width"));
            EditorGUILayout.PropertyField(slide, new GUIContent("Slide"));

            EditorGUILayout.LabelField("Auto Step", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(autostepEnabled, new GUIContent("Auto Step"));
            if (autostepEnabled.boolValue && !autostepEnabled.hasMultipleDifferentValues)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(autostepMaxHeight, new GUIContent("Max Height"));
                EditorGUILayout.PropertyField(autostepMinWidth, new GUIContent("Min Width"));
                EditorGUILayout.PropertyField(autostepIncludeDynamicBodies, new GUIContent("Include Dynamic Bodies"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.LabelField("Snap To Ground", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(snapToGroundEnabled, new GUIContent("Snap To Ground"));
            if (snapToGroundEnabled.boolValue && !snapToGroundEnabled.hasMultipleDifferentValues)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(snapToGroundDistance, new GUIContent("Distance"));
                EditorGUI.indentLevel--;
            }

            // Advanced
            if (RapierEditorUtility.AdvancedFoldout("RapierCharacterControllerEditor.Advanced"))
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField("Body", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(rigidBody, new GUIContent("Rigid Body"));
                EditorGUILayout.PropertyField(registerBodyOnEnable, new GUIContent("Register Body On Enable"));

                EditorGUILayout.LabelField("Orientation", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(up, new GUIContent("Up Direction"));

                EditorGUILayout.LabelField("Slopes", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(minSlopeSlideAngle, new GUIContent("Min Slope Slide Angle"));

                EditorGUILayout.LabelField("Precision", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(normalNudgeFactor, new GUIContent("Normal Nudge Factor"));

                EditorGUILayout.LabelField("Filtering", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(filterFlags, new GUIContent("Filter Flags"));
                EditorGUILayout.PropertyField(useCollisionGroups, new GUIContent("Use Collision Groups"));
                if (useCollisionGroups.boolValue && !useCollisionGroups.hasMultipleDifferentValues)
                {
                    EditorGUI.indentLevel++;
                    RapierEditorUtility.PackedGroupMaskField(collisionGroups);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(excludeOwnBody, new GUIContent("Exclude Own Body"));

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
