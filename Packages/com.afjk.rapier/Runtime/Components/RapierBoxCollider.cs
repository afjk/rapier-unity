using UnityEngine;

namespace AFJK.Rapier
{
    public sealed class RapierBoxCollider : RapierColliderComponent
    {
        [SerializeField] private Vector3 halfExtents = Vector3.one * 0.5f;

        public Vector3 HalfExtents
        {
            get => halfExtents;
            set => halfExtents = Vector3.Max(value, Vector3.zero);
        }

        protected override RapierColliderHandle CreateCollider(
            RapierWorld world,
            RapierRigidBodyHandle body)
        {
            return world.CreateBoxCollider(
                body,
                new RapierBoxColliderDesc
                {
                    HalfExtents = halfExtents,
                    Density = Density,
                    IsSensor = IsSensor,
                    LocalPosition = LocalPosition,
                    LocalRotation = LocalRotation
                });
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            halfExtents = Vector3.Max(halfExtents, Vector3.zero);
        }
    }
}

