using UnityEngine;

namespace AFJK.Rapier
{
    [AddComponentMenu("Rapier/Joints/Rapier Rope Joint")]
    public sealed class RapierRopeJoint : RapierJoint
    {
        [SerializeField] private float maxDistance = 1f;

        public float MaxDistance
        {
            get => maxDistance;
            set => maxDistance = Mathf.Max(0f, value);
        }

        protected override RapierJointHandle CreateJoint(
            RapierWorld world,
            RapierRigidBodyHandle body1Handle,
            RapierRigidBodyHandle body2Handle)
        {
            return world.CreateRopeJoint(body1Handle, body2Handle, LocalAnchor1, LocalAnchor2, maxDistance);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            maxDistance = Mathf.Max(0f, maxDistance);
        }
    }
}
