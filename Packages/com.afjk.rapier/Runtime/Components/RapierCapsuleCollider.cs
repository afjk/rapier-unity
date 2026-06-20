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
            // Rapier capsules are aligned to their local Y axis: scale the height by the Y axis and
            // the radius by the larger perpendicular (X/Z) axis, matching the Scene View handle.
            var scale = ShapeScale;
            var heightScale = scale.y;
            var radiusScale = Mathf.Max(scale.x, scale.z);

            return world.CreateCapsuleCollider(
                body,
                new RapierCapsuleColliderDesc
                {
                    HalfHeight = halfHeight * heightScale,
                    Radius = radius * radiusScale,
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
            halfHeight = Mathf.Max(0f, halfHeight);
            radius = Mathf.Max(0f, radius);
        }
    }
}
