using AFJK.Rapier;
using UnityEditor;
using UnityEngine;

namespace AFJK.Rapier.Editor
{
    [CustomEditor(typeof(RapierWorldBehaviour))]
    [CanEditMultipleObjects]
    public class RapierWorldBehaviourEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var stepMode = serializedObject.FindProperty("stepMode");
            var gravity = serializedObject.FindProperty("gravity");
            var timestep = serializedObject.FindProperty("timestep");
            var logStateHash = serializedObject.FindProperty("logStateHash");
            var registrationMode = serializedObject.FindProperty("registrationMode");

            EditorGUILayout.LabelField("Simulation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(stepMode, new GUIContent("Step Mode"));
            EditorGUILayout.PropertyField(gravity, new GUIContent("Gravity"));
            DrawTimestepField(timestep);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(logStateHash, new GUIContent("Log State Hash"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(registrationMode, new GUIContent("Registration Mode"));

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawTimestepField(SerializedProperty timestepProp)
        {
            EditorGUI.showMixedValue = timestepProp.hasMultipleDifferentValues;

            var currentRate = 1f / timestepProp.floatValue;

            EditorGUI.BeginChangeCheck();
            var newRate = EditorGUILayout.FloatField(new GUIContent("Simulation Rate (Hz)"), currentRate);
            if (EditorGUI.EndChangeCheck())
            {
                newRate = Mathf.Max(1f, newRate);
                timestepProp.floatValue = 1f / newRate;
            }

            EditorGUI.showMixedValue = false;

            if (!timestepProp.hasMultipleDifferentValues)
            {
                var rate = 1f / timestepProp.floatValue;
                EditorGUILayout.LabelField(
                    "Timestep",
                    $"1 / {rate.ToString("0.###")} sec (= {timestepProp.floatValue.ToString("0.#######")})");
            }
        }
    }
}
