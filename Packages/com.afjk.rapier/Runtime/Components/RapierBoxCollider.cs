using UnityEngine;

namespace AFJK.Rapier
{
    [AddComponentMenu("Rapier/Colliders/Rapier Box Collider")]
    public sealed class RapierBoxCollider : RapierCollider
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
                    HalfExtents = Vector3.Scale(halfExtents, ShapeScale),
                    Density = Density,
                    Friction = Friction,
                    HasFriction = true,
                    Restitution = Restitution,
                    IsSensor = IsSensor,
                    LocalPosition = ScaledLocalPosition,
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
