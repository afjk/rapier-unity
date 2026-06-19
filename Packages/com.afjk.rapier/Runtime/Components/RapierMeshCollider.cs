using UnityEngine;

namespace AFJK.Rapier
{
    [AddComponentMenu("Rapier/Colliders/Rapier Mesh Collider")]
    public sealed class RapierMeshCollider : RapierCollider
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
            if (mesh == null)
            {
                Debug.LogWarning($"{nameof(RapierMeshCollider)} requires a readable {nameof(Mesh)}.", this);
                return RapierColliderHandle.Invalid;
            }

            Vector3[] vertices;
            try
            {
                vertices = mesh.vertices;
            }
            catch (UnityException)
            {
                Debug.LogWarning($"{nameof(RapierMeshCollider)} requires a readable {nameof(Mesh)}.", this);
                return RapierColliderHandle.Invalid;
            }

            if (vertices == null || vertices.Length == 0)
            {
                Debug.LogWarning($"{nameof(RapierMeshCollider)} mesh has no vertices.", this);
                return RapierColliderHandle.Invalid;
            }

            var desc = new RapierMeshColliderDesc
            {
                Density = Density,
                Friction = Friction,
                Restitution = Restitution,
                IsSensor = IsSensor,
                LocalPosition = LocalPosition,
                LocalRotation = LocalRotation
            };

            if (convex)
            {
                return world.CreateConvexHullCollider(body, vertices, desc);
            }

            int[] indices;
            try
            {
                indices = mesh.triangles;
            }
            catch (UnityException)
            {
                Debug.LogWarning($"{nameof(RapierMeshCollider)} requires readable triangle indices.", this);
                return RapierColliderHandle.Invalid;
            }

            if (indices == null || indices.Length < 3)
            {
                Debug.LogWarning($"{nameof(RapierMeshCollider)} mesh has no triangle indices.", this);
                return RapierColliderHandle.Invalid;
            }

            return world.CreateTrimeshCollider(body, vertices, indices, desc);
        }
    }
}
