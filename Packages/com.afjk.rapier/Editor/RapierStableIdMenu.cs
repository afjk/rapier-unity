using UnityEditor;
using UnityEngine;

namespace AFJK.Rapier.Editor
{
    /// <summary>
    /// Editor utilities for assigning persistent stable ids to Rapier components, so a Scene or
    /// Prefab carries stable, host-independent ids (useful for Scene Sync import and network parity).
    /// </summary>
    public static class RapierStableIdMenu
    {
        private const string AssignSelectionPath = "Tools/Rapier/Assign Stable Ids To Selection";
        private const string AssignSelectionRecursivePath = "Tools/Rapier/Assign Stable Ids To Selection (Recursive)";

        [MenuItem(AssignSelectionPath, false, 100)]
        public static void AssignToSelection()
        {
            AssignToSelection(false);
        }

        [MenuItem(AssignSelectionRecursivePath, false, 101)]
        public static void AssignToSelectionRecursive()
        {
            AssignToSelection(true);
        }

        [MenuItem(AssignSelectionPath, true)]
        [MenuItem(AssignSelectionRecursivePath, true)]
        private static bool ValidateSelection()
        {
            return Selection.gameObjects.Length > 0;
        }

        private static void AssignToSelection(bool includeChildren)
        {
            var assigned = 0;
            foreach (var gameObject in Selection.gameObjects)
            {
                var components = includeChildren
                    ? gameObject.GetComponentsInChildren<IRapierRegistrationOrdered>(true)
                    : gameObject.GetComponents<IRapierRegistrationOrdered>();

                for (var i = 0; i < components.Length; i++)
                {
                    if (AssignIfEmpty(components[i]))
                    {
                        assigned++;
                    }
                }
            }

            Debug.Log($"Rapier: assigned {assigned} stable id(s) to the selection.");
        }

        private static bool AssignIfEmpty(IRapierRegistrationOrdered component)
        {
            if (component == null || !string.IsNullOrEmpty(component.StableId))
            {
                return false;
            }

            if (!(component is Object unityObject))
            {
                return false;
            }

            Undo.RecordObject(unityObject, "Assign Rapier Stable Id");
            component.StableId = RapierStableId.Generate();
            EditorUtility.SetDirty(unityObject);
            return true;
        }
    }
}
