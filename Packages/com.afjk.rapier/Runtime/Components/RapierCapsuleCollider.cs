using UnityEngine;

namespace AFJK.Rapier
{
    [AddComponentMenu("Rapier/Colliders/Rapier Capsule Collider")]
    public sealed class RapierCapsuleCollider : RapierCollider
    {
        [SerializeField] private float halfHeight = 0.5f;
        [SerializeField] private float radius = 0.25f;

        public float HalfHeight
        {
            get => halfHeight;
            set => halfHeight = Mathf.Max(0f, value);
        }

        public float Radius
        {
            get => radius;
            set => radius = Mathf.Max(0f, value);
        }

        protected override RapierColliderHandle CreateCollider(
            RapierWorld world,
            RapierRigidBodyHandle body)
        {
            return world.CreateCapsuleCollider(
                body,
                new RapierCapsuleColliderDesc
                {
                    HalfHeight = halfHeight,
                    Radius = radius,
                    Density = Density,
                    Friction = Friction,
                    HasFriction = true,
                    Restitution = Restitution,
                    IsSensor = IsSensor,
                    LocalPosition = LocalPosition,
                    LocalRotation = LocalRotation
                });
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            halfHeight = Mathf.Max(0f, halfHeight);
            radius = Mathf.Max(0f, radius);
        }
    }
}
