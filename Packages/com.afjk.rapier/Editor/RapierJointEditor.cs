using AFJK.Rapier;
using UnityEditor;
using UnityEngine;

namespace AFJK.Rapier.Editor
{
    [CustomEditor(typeof(RapierJoint), true)]
    [CanEditMultipleObjects]
    public class RapierJointEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var body1 = serializedObject.FindProperty("body1");
            var body2 = serializedObject.FindProperty("body2");
            var registerOnEnable = serializedObject.FindProperty("registerOnEnable");
            var localAnchor1 = serializedObject.FindProperty("localAnchor1");
            var localAnchor2 = serializedObject.FindProperty("localAnchor2");
            var stableId = serializedObject.FindProperty("stableId");
            var autoGenerateStableId = serializedObject.FindProperty("autoGenerateStableId");
            var registrationOrder = serializedObject.FindProperty("registrationOrder");

            RapierEditorUtility.AutoResolveRigidBody(body1);

            // Common
            EditorGUILayout.PropertyField(body2, new GUIContent("Connected Body"));

            EditorGUILayout.LabelField("Anchors", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(localAnchor1, new GUIContent("Anchor"));
            EditorGUILayout.PropertyField(localAnchor2, new GUIContent("Connected Anchor"));

            DrawTypeSpecific();

            // Advanced
            if (RapierEditorUtility.AdvancedFoldout("RapierJointEditor.Advanced"))
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField("Bodies", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(body1, new GUIContent("Body 1 (Override)"));

                EditorGUILayout.LabelField("Lifecycle", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(registerOnEnable, new GUIContent("Register On Enable"));

                EditorGUILayout.LabelField("Stable Id / Registration", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(stableId, new GUIContent("Stable Id"));
                EditorGUILayout.PropertyField(autoGenerateStableId, new GUIContent("Auto Generate Stable Id"));
                EditorGUILayout.PropertyField(registrationOrder, new GUIContent("Registration Order"));

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTypeSpecific()
        {
            var t0 = target.GetType();
            foreach (var o in targets)
            {
                if (o.GetType() != t0)
                {
                    return;
                }
            }

            if (target is RapierRevoluteJoint)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("axis"), new GUIContent("Axis"));
            }
            else if (target is RapierPrismaticJoint)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("axis"), new GUIContent("Axis"));
            }
            else if (target is RapierRopeJoint)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxDistance"), new GUIContent("Max Distance"));
            }
            else if (target is RapierSpringJoint)
            {
                EditorGUILayout.LabelField("Spring", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("restLength"), new GUIContent("Rest Length"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("stiffness"), new GUIContent("Stiffness"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("damping"), new GUIContent("Damping"));
            }
        }
    }
}
