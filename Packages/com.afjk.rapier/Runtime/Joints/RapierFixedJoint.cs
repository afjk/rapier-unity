using UnityEngine;

namespace AFJK.Rapier
{
    [AddComponentMenu("Rapier/Joints/Rapier Fixed Joint")]
    public sealed class RapierFixedJoint : RapierJoint
    {
        protected override RapierJointHandle CreateJoint(
            RapierWorld world,
            RapierRigidBodyHandle body1Handle,
            RapierRigidBodyHandle body2Handle)
        {
            return world.CreateFixedJoint(body1Handle, body2Handle, LocalAnchor1, LocalAnchor2);
        }
    }
}
