using UnityEngine;

namespace AFJK.Rapier
{
    /// <summary>
    /// A voxel collider. Each point selects the voxel cell that contains it; <see cref="VoxelSize"/>
    /// controls the per-axis cell dimensions. Points can be assigned in code or authored inline.
    /// </summary>
    public sealed class RapierVoxelsCollider : RapierColliderComponent
    {
        [SerializeField] private Vector3 voxelSize = Vector3.one;
        [SerializeField] private Vector3[] points;

        public Vector3 VoxelSize
        {
            get => voxelSize;
            set => voxelSize = Vector3.Max(value, new Vector3(1e-4f, 1e-4f, 1e-4f));
        }

        public Vector3[] Points
        {
            get => points;
            set => points = value;
        }

        /// <summary>Assigns the voxel sample points and cell size in one call.</summary>
        public void SetPoints(Vector3[] value, Vector3 size)
        {
            points = value;
            voxelSize = Vector3.Max(size, new Vector3(1e-4f, 1e-4f, 1e-4f));
        }

        protected override RapierColliderHandle CreateCollider(
            RapierWorld world,
            RapierRigidBodyHandle body)
        {
            if (points == null || points.Length == 0)
            {
                Debug.LogWarning($"{nameof(RapierVoxelsCollider)} requires voxel sample points.", this);
                return RapierColliderHandle.Invalid;
            }

            return world.CreateVoxelsCollider(body, points, voxelSize, BuildMeshDesc());
        }

        private RapierMeshColliderDesc BuildMeshDesc()
        {
            return new RapierMeshColliderDesc
            {
                Density = Density,
                Friction = Friction,
                Restitution = Restitution,
                IsSensor = IsSensor,
                LocalPosition = LocalPosition,
                LocalRotation = LocalRotation
            };
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            voxelSize = Vector3.Max(voxelSize, new Vector3(1e-4f, 1e-4f, 1e-4f));
        }
    }
}
