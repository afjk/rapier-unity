using UnityEngine;

namespace AFJK.Rapier
{
    [AddComponentMenu("Rapier/Joints/Rapier Spherical Joint")]
    public sealed class RapierSphericalJoint : RapierJoint
    {
        protected override RapierJointHandle CreateJoint(
            RapierWorld world,
            RapierRigidBodyHandle body1Handle,
            RapierRigidBodyHandle body2Handle)
        {
            return world.CreateSphericalJoint(body1Handle, body2Handle, LocalAnchor1, LocalAnchor2);
        }
    }
}
