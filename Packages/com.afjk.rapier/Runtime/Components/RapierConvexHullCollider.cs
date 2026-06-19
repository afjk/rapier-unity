using UnityEngine;

namespace AFJK.Rapier
{
    /// <summary>
    /// A convex-hull collider built from an explicit point cloud. Points can be assigned in
    /// code (for procedurally generated shapes such as cylinders or cones) or derived from an
    /// assigned <see cref="UnityEngine.Mesh"/> for editor authoring.
    /// </summary>
    [AddComponentMenu("Rapier/Colliders/Rapier Convex Hull Collider")]
    public sealed class RapierConvexHullCollider : RapierCollider
    {
        [SerializeField] private Mesh sourceMesh;
        [SerializeField] private Vector3[] points;

        /// <summary>
        /// Explicit hull points, serialized so they persist in a Scene/Prefab. When non-empty
        /// these take priority over <see cref="SourceMesh"/>.
        /// </summary>
        public Vector3[] Points
        {
            get => points;
            set => points = value;
        }

        public Mesh SourceMesh
        {
            get => sourceMesh;
            set => sourceMesh = value;
        }

        /// <summary>Assigns the hull points used the next time the collider is (re)created.</summary>
        public void SetPoints(Vector3[] value)
        {
            points = value;
        }

        protected override RapierColliderHandle CreateCollider(
            RapierWorld world,
            RapierRigidBodyHandle body)
        {
            var hullPoints = ResolvePoints();
            if (hullPoints == null || hullPoints.Length == 0)
            {
                Debug.LogWarning($"{nameof(RapierConvexHullCollider)} requires hull points or a readable source mesh.", this);
                return RapierColliderHandle.Invalid;
            }

            return world.CreateConvexHullCollider(body, hullPoints, BuildMeshDesc());
        }

        private Vector3[] ResolvePoints()
        {
            if (points != null && points.Length > 0)
            {
                return points;
            }

            if (sourceMesh == null)
            {
                return null;
            }

            try
            {
                return sourceMesh.vertices;
            }
            catch (UnityException)
            {
                Debug.LogWarning($"{nameof(RapierConvexHullCollider)} requires a readable source mesh.", this);
                return null;
            }
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
    }
}
