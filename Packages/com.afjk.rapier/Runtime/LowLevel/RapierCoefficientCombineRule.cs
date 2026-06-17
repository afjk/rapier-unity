namespace AFJK.Rapier
{
    /// <summary>
    /// How the friction or restitution coefficients of two colliders are combined.
    /// Mirrors Rapier's <c>CoefficientCombineRule</c> discriminants.
    /// </summary>
    public enum RapierCoefficientCombineRule : uint
    {
        Average = 0,
        Min = 1,
        Multiply = 2,
        Max = 3
    }
}
