using UnityEngine;

namespace AFJK.Rapier
{
    public sealed class RapierSphereCollider : RapierColliderComponent
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
