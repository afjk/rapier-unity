using UnityEngine;

namespace AFJK.Rapier
{
    /// <summary>
    /// A heightfield collider. Heights are stored in row-major order and scaled by
    /// <see cref="Scale"/>. Sample data can be assigned in code or authored inline.
    /// </summary>
    [AddComponentMenu("Rapier/Colliders/Rapier Heightfield Collider")]
    public sealed class RapierHeightfieldCollider : RapierCollider
    {
        [SerializeField] private int rows = 2;
        [SerializeField] private int columns = 2;
        [SerializeField] private Vector3 scale = new Vector3(10f, 1f, 10f);
        [SerializeField] private float[] heights;

        public int Rows
        {
            get => rows;
            set => rows = Mathf.Max(1, value);
        }

        public int Columns
        {
            get => columns;
            set => columns = Mathf.Max(1, value);
        }

        public Vector3 Scale
        {
            get => scale;
            set => scale = value;
        }

        public float[] Heights
        {
            get => heights;
            set => heights = value;
        }

        /// <summary>Assigns the heightfield samples and grid dimensions in one call.</summary>
        public void SetHeights(float[] samples, int sampleRows, int sampleColumns, Vector3 worldScale)
        {
            heights = samples;
            rows = Mathf.Max(1, sampleRows);
            columns = Mathf.Max(1, sampleColumns);
            scale = worldScale;
        }

        protected override RapierColliderHandle CreateCollider(
            RapierWorld world,
            RapierRigidBodyHandle body)
        {
            if (heights == null || heights.Length != rows * columns)
            {
                Debug.LogWarning(
                    $"{nameof(RapierHeightfieldCollider)} expects rows*columns ({rows * columns}) height samples but has {heights?.Length ?? 0}.",
                    this);
                return RapierColliderHandle.Invalid;
            }

            return world.CreateHeightfieldCollider(body, heights, rows, columns, scale, BuildMeshDesc());
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
            rows = Mathf.Max(1, rows);
            columns = Mathf.Max(1, columns);
        }
    }
}
