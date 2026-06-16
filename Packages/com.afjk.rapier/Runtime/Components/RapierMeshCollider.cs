using UnityEngine;

namespace AFJK.Rapier
{
    public sealed class RapierMeshCollider : RapierColliderComponent
    {
        [SerializeField] private Mesh mesh;
        [SerializeField] private bool convex;

        public Mesh Mesh
        {
            get => mesh;
            set => mesh = value;
        }

        public bool Convex
        {
            get => convex;
            set => convex = value;
        }

        protected override RapierColliderHandle CreateCollider(
            RapierWorld world,
            RapierRigidBodyHandle body)
        {
            Debug.LogWarning("Rapier mesh collider native FFI is not implemented yet.", this);
            return RapierColliderHandle.Invalid;
        }
    }
}

