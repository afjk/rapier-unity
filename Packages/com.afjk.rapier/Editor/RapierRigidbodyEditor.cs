using AFJK.Rapier;
using UnityEditor;
using UnityEngine;

namespace AFJK.Rapier.Editor
{
    [CustomEditor(typeof(RapierRigidbody))]
    [CanEditMultipleObjects]
    public class RapierRigidbodyEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var worldComponent = serializedObject.FindProperty("worldComponent");
            var bodyType = serializedObject.FindProperty("bodyType");
            var registerOnEnable = serializedObject.FindProperty("registerOnEnable");
            var syncTransformFromRapier = serializedObject.FindProperty("syncTransformFromRapier");
            var syncTransformToRapierOnRegister = serializedObject.FindProperty("syncTransformToRapierOnRegister");
            var syncTransformToRapierBeforeStep = serializedObject.FindProperty("syncTransformToRapierBeforeStep");
            var canSleep = serializedObject.FindProperty("canSleep");
            var ccdEnabled = serializedObject.FindProperty("ccdEnabled");
            var initialLinearVelocity = serializedObject.FindProperty("initialLinearVelocity");
            var initialAngularVelocity = serializedObject.FindProperty("initialAngularVelocity");
            var linearDamping = serializedObject.FindProperty("linearDamping");
            var angularDamping = serializedObject.FindProperty("angularDamping");
            var stableId = serializedObject.FindProperty("stableId");
            var autoGenerateStableId = serializedObject.FindProperty("autoGenerateStableId");
            var registrationOrder = serializedObject.FindProperty("registrationOrder");
            var gravityScale = serializedObject.FindProperty("gravityScale");
            var softCcdPrediction = serializedObject.FindProperty("softCcdPrediction");
            var additionalSolverIterations = serializedObject.FindProperty("additionalSolverIterations");
            var dominanceGroup = serializedObject.FindProperty("dominanceGroup");
            var lockTranslationX = serializedObject.FindProperty("lockTranslationX");
            var lockTranslationY = serializedObject.FindProperty("lockTranslationY");
            var lockTranslationZ = serializedObject.FindProperty("lockTranslationZ");
            var lockRotationX = serializedObject.FindProperty("lockRotationX");
            var lockRotationY = serializedObject.FindProperty("lockRotationY");
            var lockRotationZ = serializedObject.FindProperty("lockRotationZ");

            RapierEditorUtility.AutoResolveWorld(worldComponent);

            // Common
            EditorGUILayout.PropertyField(bodyType, new GUIContent("Body Type"));

            DrawComputedMass();

            EditorGUILayout.PropertyField(linearDamping, new GUIContent("Linear Damping"));
            EditorGUILayout.PropertyField(angularDamping, new GUIContent("Angular Damping"));
            EditorGUILayout.PropertyField(canSleep, new GUIContent("Can Sleep"));

            RapierEditorUtility.CollisionDetectionPopup(ccdEnabled);

            RapierEditorUtility.ConstraintsGrid(
                lockTranslationX, lockTranslationY, lockTranslationZ,
                lockRotationX, lockRotationY, lockRotationZ);

            EditorGUILayout.LabelField("Initial Velocity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(initialLinearVelocity, new GUIContent("Linear"));
            EditorGUILayout.PropertyField(initialAngularVelocity, new GUIContent("Angular"));

            // Advanced
            if (RapierEditorUtility.AdvancedFoldout("RapierRigidbodyEditor.Advanced"))
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField("World", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(worldComponent, new GUIContent("World"));

                EditorGUILayout.LabelField("Lifecycle", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(registerOnEnable, new GUIContent("Register On Enable"));

                EditorGUILayout.LabelField("Transform Sync", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(syncTransformFromRapier, new GUIContent("Sync From Rapier"));
                EditorGUILayout.PropertyField(syncTransformToRapierOnRegister, new GUIContent("Sync To Rapier On Register"));
                EditorGUILayout.PropertyField(syncTransformToRapierBeforeStep, new GUIContent("Sync To Rapier Before Step"));

                EditorGUILayout.LabelField("Body Tuning", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(gravityScale, new GUIContent("Gravity Scale"));
                EditorGUILayout.PropertyField(softCcdPrediction, new GUIContent("Soft CCD Prediction"));
                EditorGUILayout.PropertyField(additionalSolverIterations, new GUIContent("Additional Solver Iterations"));
                EditorGUILayout.PropertyField(dominanceGroup, new GUIContent("Dominance Group"));

                EditorGUILayout.LabelField("Stable Id / Registration", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(stableId, new GUIContent("Stable Id"));
                EditorGUILayout.PropertyField(autoGenerateStableId, new GUIContent("Auto Generate Stable Id"));
                EditorGUILayout.PropertyField(registrationOrder, new GUIContent("Registration Order"));

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawComputedMass()
        {
            if (targets.Length > 1)
            {
                EditorGUILayout.LabelField("Computed Mass", "Multiple Selection");
                return;
            }

            var rigidbody = (RapierRigidbody)target;
            if (rigidbody.IsRegistered && rigidbody.TryGetMass(out var mass))
            {
                EditorGUILayout.LabelField("Computed Mass", $"{mass:0.###} kg");
                EditorGUILayout.HelpBox(
                    "Mass is computed from attached Rapier colliders and their density.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.LabelField("Computed Mass", "Not available");
                EditorGUILayout.HelpBox(
                    "Mass is computed from attached Rapier colliders. It becomes available after the body is registered, usually in Play Mode.\n" +
                    "Edit Density on attached Rapier Collider components to affect mass.",
                    MessageType.None);
            }
        }
    }
}
