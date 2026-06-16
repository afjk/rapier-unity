using AFJK.Rapier;
using UnityEditor;
using UnityEngine;

namespace AFJK.Rapier.Editor
{
    public static class RapierMenuItems
    {
        [MenuItem("GameObject/Rapier/Rapier World", false, 10)]
        public static void CreateRapierWorld(MenuCommand command)
        {
            var gameObject = new GameObject("Rapier World");
            GameObjectUtility.SetParentAndAlign(gameObject, command.context as GameObject);
            gameObject.AddComponent<RapierWorldComponent>();
            Undo.RegisterCreatedObjectUndo(gameObject, "Create Rapier World");
            Selection.activeGameObject = gameObject;
        }

        [MenuItem("GameObject/Rapier/Dynamic Box Body", false, 11)]
        public static void CreateDynamicBoxBody(MenuCommand command)
        {
            var gameObject = new GameObject("Rapier Dynamic Box");
            GameObjectUtility.SetParentAndAlign(gameObject, command.context as GameObject);
            gameObject.AddComponent<RapierRigidBodyComponent>();
            gameObject.AddComponent<RapierBoxCollider>();
            Undo.RegisterCreatedObjectUndo(gameObject, "Create Rapier Dynamic Box");
            Selection.activeGameObject = gameObject;
        }
    }
}

