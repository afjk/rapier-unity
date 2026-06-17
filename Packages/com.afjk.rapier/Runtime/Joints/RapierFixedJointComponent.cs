namespace AFJK.Rapier
{
    public sealed class RapierFixedJointComponent : RapierJointComponent
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
