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
            // Unity's SphereCollider scales the radius by the largest absolute axis scale so the
            // sphere stays round under non-uniform scale; mirror that here and in the Scene handle.
            var scale = ShapeScale;
            var radiusScale = Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));

            return world.CreateSphereCollider(
                body,
                new RapierSphereColliderDesc
                {
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
            radius = Mathf.Max(0f, radius);
        }
    }
}
