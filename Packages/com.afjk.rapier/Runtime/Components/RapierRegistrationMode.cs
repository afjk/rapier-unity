namespace AFJK.Rapier
{
    /// <summary>
    /// How a <see cref="RapierWorldBehaviour"/> orders the creation of bodies, colliders, and
    /// joints when it rebuilds its world. Rapier results depend on creation order, so this makes
    /// that order explicit instead of relying on Unity's incidental component discovery order.
    /// </summary>
    public enum RapierRegistrationMode
    {
        /// <summary>Order by Unity hierarchy (depth-first, sibling order). Closest to authoring intuition.</summary>
        HierarchyOrder = 0,

        /// <summary>Order by <c>StableId</c> (ordinal). Components without an id fall back to hierarchy order.</summary>
        StableId = 1,

        /// <summary>Order by an explicit <c>RegistrationOrder</c> integer, tie-broken by <c>StableId</c> then hierarchy.</summary>
        ExplicitOrder = 2
    }

    /// <summary>
    /// Implemented by Rapier components that participate in deterministic registration ordering
    /// (rigid bodies, colliders, joints). Used by <see cref="RapierWorldBehaviour.RebuildWorld"/>.
    /// </summary>
    public interface IRapierRegistrationOrdered
    {
        int RegistrationOrder { get; }
        string StableId { get; set; }

        /// <summary>
        /// Assigns a deterministic <see cref="StableId"/> if the component opts into auto-generation
        /// and does not already have one. Called before sorting so id-based ordering is meaningful.
        /// </summary>
        void EnsureStableId();
    }
}
