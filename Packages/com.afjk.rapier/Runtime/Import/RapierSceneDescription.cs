using System;
using System.Collections.Generic;
using UnityEngine;

namespace AFJK.Rapier
{
    /// <summary>
    /// A neutral, serializable description of a physics scene that
    /// <see cref="RapierSceneImporter"/> turns into Rapier component GameObjects. It is
    /// deliberately free of any Scene Sync / network concepts: a downstream importer maps its own
    /// format into this structure, keeping the core Rapier components source-agnostic.
    /// <para>
    /// All fields are plain serializable types so a description can be authored inline, stored as a
    /// ScriptableObject, or round-tripped through <see cref="JsonUtility"/>.
    /// </para>
    /// </summary>
    [Serializable]
    public class RapierSceneDescription
    {
        public Vector3 gravity = new Vector3(0f, -9.81f, 0f);
        public float timestep = 1f / 60f;
        public RapierRegistrationMode registrationMode = RapierRegistrationMode.StableId;
        public string sourceSystem = string.Empty;
        public List<RapierBodyDescription> bodies = new List<RapierBodyDescription>();
    }

    [Serializable]
    public class RapierBodyDescription
    {
        public string id = string.Empty;
        public int order;
        public RapierRigidBodyType bodyType = RapierRigidBodyType.Dynamic;
        public Vector3 position;
        public Vector3 eulerAngles;
        public Vector3 linearVelocity;
        public Vector3 angularVelocity;
        public float linearDamping;
        public float angularDamping;
        public float gravityScale = 1f;
        public bool ccdEnabled;
        public List<RapierColliderDescription> colliders = new List<RapierColliderDescription>();
    }

    public enum RapierImportColliderShape
    {
        Box = 0,
        Sphere = 1,
        Capsule = 2
    }

    [Serializable]
    public class RapierColliderDescription
    {
        public string id = string.Empty;
        public RapierImportColliderShape shape = RapierImportColliderShape.Box;
        public Vector3 halfExtents = Vector3.one * 0.5f;
        public float radius = 0.5f;
        public float halfHeight = 0.5f;
        public Vector3 localPosition;
        public float density = 1f;
        public float friction = 0.5f;
        public float restitution;
        public bool isSensor;
    }
}
