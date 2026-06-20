using AFJK.Rapier;
using UnityEditor;
using UnityEngine;

namespace AFJK.Rapier.Editor
{
    [CustomEditor(typeof(RapierPidController))]
    [CanEditMultipleObjects]
    public class RapierPidControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var rigidBody = serializedObject.FindProperty("rigidBody");
            var kp = serializedObject.FindProperty("kp");
            var ki = serializedObject.FindProperty("ki");
            var kd = serializedObject.FindProperty("kd");
            var axes = serializedObject.FindProperty("axes");

            RapierEditorUtility.AutoResolveRigidBody(rigidBody);

            EditorGUILayout.PropertyField(rigidBody, new GUIContent("Rigid Body"));

            EditorGUILayout.LabelField("Gains", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(kp, new GUIContent("Proportional (Kp)"));
            EditorGUILayout.PropertyField(ki, new GUIContent("Integral (Ki)"));
            EditorGUILayout.PropertyField(kd, new GUIContent("Derivative (Kd)"));
            EditorGUILayout.PropertyField(axes, new GUIContent("Axes"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
