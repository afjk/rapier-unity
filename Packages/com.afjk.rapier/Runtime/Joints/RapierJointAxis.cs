namespace AFJK.Rapier
{
    /// <summary>
    /// A joint degree of freedom, used to configure limits and motors. Mirrors
    /// Rapier's <c>JointAxis</c> discriminants.
    /// </summary>
    public enum RapierJointAxis : uint
    {
        LinearX = 0,
        LinearY = 1,
        LinearZ = 2,
        AngularX = 3,
        AngularY = 4,
        AngularZ = 5
    }
}
