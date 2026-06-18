using UnityEngine;

namespace AFJK.Rapier
{
    /// <summary>
    /// A triangle-mesh collider built from explicit vertex/index buffers. Geometry can be
    /// assigned in code (for procedurally generated terrain) or derived from an assigned
    /// <see cref="UnityEngine.Mesh"/> for editor authoring.
    /// </summary>
    public sealed class RapierTrimeshCollider : RapierColliderComponent
    {
        [SerializeField] private Mesh sourceMesh;

        private Vector3[] vertices;
        private int[] indices;

        public Vector3[] Vertices
        {
            get => vertices;
            set => vertices = value;
        }

        public int[] Indices
        {
            get => indices;
            set => indices = value;
        }

        public Mesh SourceMesh
        {
            get => sourceMesh;
            set => sourceMesh = value;
        }

        /// <summary>Assigns the geometry used the next time the collider is (re)created.</summary>
        public void SetGeometry(Vector3[] meshVertices, int[] meshIndices)
        {
            vertices = meshVertices;
            indices = meshIndices;
        }

        protected override RapierColliderHandle CreateCollider(
            RapierWorld world,
            RapierRigidBodyHandle body)
        {
            ResolveGeometry(out var meshVertices, out var meshIndices);
            if (meshVertices == null || meshVertices.Length == 0 || meshIndices == null || meshIndices.Length < 3)
            {
                Debug.LogWarning($"{nameof(RapierTrimeshCollider)} requires vertices and triangle indices, or a readable source mesh.", this);
                return RapierColliderHandle.Invalid;
            }

            return world.CreateTrimeshCollider(body, meshVertices, meshIndices, BuildMeshDesc());
        }

        private void ResolveGeometry(out Vector3[] meshVertices, out int[] meshIndices)
        {
            if (vertices != null && vertices.Length > 0 && indices != null && indices.Length >= 3)
            {
                meshVertices = vertices;
                meshIndices = indices;
                return;
            }

            meshVertices = null;
            meshIndices = null;
            if (sourceMesh == null)
            {
                return;
            }

            try
            {
                meshVertices = sourceMesh.vertices;
                meshIndices = sourceMesh.triangles;
            }
            catch (UnityException)
            {
                Debug.LogWarning($"{nameof(RapierTrimeshCollider)} requires a readable source mesh.", this);
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
