using AFJK.Rapier;
using UnityEditor;
using UnityEngine;

namespace AFJK.Rapier.Editor
{
    [CustomEditor(typeof(RapierCollider), true)]
    [CanEditMultipleObjects]
    public class RapierColliderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var rigidBody = serializedObject.FindProperty("rigidBody");
            var registerOnEnable = serializedObject.FindProperty("registerOnEnable");
            var isSensor = serializedObject.FindProperty("isSensor");
            var density = serializedObject.FindProperty("density");
            var friction = serializedObject.FindProperty("friction");
            var restitution = serializedObject.FindProperty("restitution");
            var localPosition = serializedObject.FindProperty("localPosition");
            var localRotation = serializedObject.FindProperty("localRotation");
            var stableId = serializedObject.FindProperty("stableId");
            var autoGenerateStableId = serializedObject.FindProperty("autoGenerateStableId");
            var registrationOrder = serializedObject.FindProperty("registrationOrder");
            var frictionCombineRule = serializedObject.FindProperty("frictionCombineRule");
            var restitutionCombineRule = serializedObject.FindProperty("restitutionCombineRule");
            var overrideCollisionGroups = serializedObject.FindProperty("overrideCollisionGroups");
            var collisionGroupMemberships = serializedObject.FindProperty("collisionGroupMemberships");
            var collisionGroupFilter = serializedObject.FindProperty("collisionGroupFilter");
            var overrideSolverGroups = serializedObject.FindProperty("overrideSolverGroups");
            var solverGroupMemberships = serializedObject.FindProperty("solverGroupMemberships");
            var solverGroupFilter = serializedObject.FindProperty("solverGroupFilter");
            var activeEvents = serializedObject.FindProperty("activeEvents");
            var overrideActiveCollisionTypes = serializedObject.FindProperty("overrideActiveCollisionTypes");
            var activeCollisionTypes = serializedObject.FindProperty("activeCollisionTypes");
            var contactForceEventThreshold = serializedObject.FindProperty("contactForceEventThreshold");

            RapierEditorUtility.AutoResolveRigidBody(rigidBody);

            DrawShapeSection();

            // Common
            EditorGUILayout.PropertyField(localPosition, new GUIContent("Center"));
            EditorGUILayout.PropertyField(isSensor, new GUIContent("Is Trigger"));

            EditorGUILayout.LabelField("Material", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(density, new GUIContent("Density"));
            EditorGUILayout.PropertyField(friction, new GUIContent("Friction"));
            EditorGUILayout.PropertyField(restitution, new GUIContent("Restitution"));

            // Advanced
            if (RapierEditorUtility.AdvancedFoldout("RapierColliderEditor.Advanced"))
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(localRotation, new GUIContent("Local Rotation"));

                EditorGUILayout.LabelField("Rigid Body", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(rigidBody, new GUIContent("Rigid Body"));

                EditorGUILayout.LabelField("Lifecycle", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(registerOnEnable, new GUIContent("Register On Enable"));

                EditorGUILayout.LabelField("Material Combine", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(frictionCombineRule, new GUIContent("Friction Combine"));
                EditorGUILayout.PropertyField(restitutionCombineRule, new GUIContent("Restitution Combine"));

                EditorGUILayout.LabelField("Collision Filtering", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(overrideCollisionGroups, new GUIContent("Override Collision Groups"));
                if (overrideCollisionGroups.boolValue && !overrideCollisionGroups.hasMultipleDifferentValues)
                {
                    EditorGUI.indentLevel++;
                    RapierEditorUtility.GroupMaskField(collisionGroupMemberships, "Memberships");
                    RapierEditorUtility.GroupMaskField(collisionGroupFilter, "Filter");
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(overrideSolverGroups, new GUIContent("Override Solver Groups"));
                if (overrideSolverGroups.boolValue && !overrideSolverGroups.hasMultipleDifferentValues)
                {
                    EditorGUI.indentLevel++;
                    RapierEditorUtility.GroupMaskField(solverGroupMemberships, "Memberships");
                    RapierEditorUtility.GroupMaskField(solverGroupFilter, "Filter");
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(activeEvents, new GUIContent("Active Events"));
                EditorGUILayout.PropertyField(overrideActiveCollisionTypes, new GUIContent("Override Active Collision Types"));
                if (overrideActiveCollisionTypes.boolValue && !overrideActiveCollisionTypes.hasMultipleDifferentValues)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(activeCollisionTypes, new GUIContent("Active Collision Types"));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(contactForceEventThreshold, new GUIContent("Contact Force Event Threshold"));

                EditorGUILayout.LabelField("Stable Id / Registration", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(stableId, new GUIContent("Stable Id"));
                EditorGUILayout.PropertyField(autoGenerateStableId, new GUIContent("Auto Generate Stable Id"));
                EditorGUILayout.PropertyField(registrationOrder, new GUIContent("Registration Order"));

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawShapeSection()
        {
            var t0 = target.GetType();
            foreach (var o in targets)
            {
                if (o.GetType() != t0)
                {
                    EditorGUILayout.HelpBox("Multiple collider shape types selected.", MessageType.None);
                    return;
                }
            }

            if (target is RapierBoxCollider)
            {
                RapierEditorUtility.HalfExtentsAsSizeField(serializedObject.FindProperty("halfExtents"));
            }
            else if (target is RapierSphereCollider)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("radius"), new GUIContent("Radius"));
            }
            else if (target is RapierCapsuleCollider)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("radius"), new GUIContent("Radius"));
                RapierEditorUtility.HalfValueAsFullField(serializedObject.FindProperty("halfHeight"), "Height");
            }
            else if (target is RapierConvexHullCollider)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sourceMesh"), new GUIContent("Source Mesh"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("points"), new GUIContent("Points"), true);
            }
            else if (target is RapierTrimeshCollider)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sourceMesh"), new GUIContent("Source Mesh"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("vertices"), new GUIContent("Vertices"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("indices"), new GUIContent("Indices"), true);
            }
            else if (target is RapierHeightfieldCollider)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rows"), new GUIContent("Rows"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("columns"), new GUIContent("Columns"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("scale"), new GUIContent("Scale"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("heights"), new GUIContent("Heights"), true);
            }
            else if (target is RapierVoxelsCollider)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("voxelSize"), new GUIContent("Voxel Size"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("points"), new GUIContent("Points"), true);
            }
            else if (target is RapierMeshCollider)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("mesh"), new GUIContent("Mesh"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("convex"), new GUIContent("Convex"));
            }
        }
    }
}
