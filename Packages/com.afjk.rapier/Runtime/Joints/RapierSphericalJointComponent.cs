namespace AFJK.Rapier
{
    public sealed class RapierSphericalJointComponent : RapierJointComponent
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
