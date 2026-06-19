using UnityEngine;

namespace AFJK.Rapier
{
    [AddComponentMenu("Rapier/Colliders/Rapier Sphere Collider")]
    public sealed class RapierSphereCollider : RapierCollider
    {
        [SerializeField] private float radius = 0.5f;

        public float Radius
        {
            get => radius;
            set => radius = Mathf.Max(0f, value);
        }

        protected override RapierColliderHandle CreateCollider(
            RapierWorld world,
            RapierRigidBodyHandle body)
        {
            return world.CreateSphereCollider(
                body,
                new RapierSphereColliderDesc
                {
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
            radius = Mathf.Max(0f, radius);
        }
    }
}
