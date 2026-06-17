using UnityEngine;

namespace AFJK.Rapier
{
    public sealed class RapierRevoluteJointComponent : RapierJointComponent
    {
        [SerializeField] private Vector3 axis = Vector3.up;

        public Vector3 Axis
        {
            get => axis;
            set => axis = SanitizeAxis(value, Vector3.up);
        }

        protected override RapierJointHandle CreateJoint(
            RapierWorld world,
            RapierRigidBodyHandle body1Handle,
            RapierRigidBodyHandle body2Handle)
        {
            return world.CreateRevoluteJoint(body1Handle, body2Handle, LocalAnchor1, LocalAnchor2, Axis);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            axis = SanitizeAxis(axis, Vector3.up);
        }

        private static Vector3 SanitizeAxis(Vector3 value, Vector3 fallback)
        {
            return value.sqrMagnitude > 1.0e-12f ? value : fallback;
        }
    }
}
