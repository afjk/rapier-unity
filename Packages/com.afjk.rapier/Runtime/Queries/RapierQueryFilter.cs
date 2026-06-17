using System;
using UnityEngine;

namespace AFJK.Rapier
{
    /// <summary>
    /// Flags excluding whole sets of colliders from a scene query. Mirrors Rapier's
    /// <c>QueryFilterFlags</c> bit values.
    /// </summary>
    [Flags]
    public enum RapierQueryFilterFlags : uint
    {
        None = 0,
        ExcludeFixed = 1 << 0,
        ExcludeKinematic = 1 << 1,
        ExcludeDynamic = 1 << 2,
        ExcludeSensors = 1 << 3,
        ExcludeSolids = 1 << 4,
        OnlyDynamic = ExcludeFixed | ExcludeKinematic,
        OnlyKinematic = ExcludeDynamic | ExcludeFixed,
        OnlyFixed = ExcludeDynamic | ExcludeKinematic
    }

    /// <summary>
    /// Rules describing which colliders a scene query should consider. Construct with
    /// <see cref="Default"/> and the fluent <c>Excluding*</c> / <c>WithGroups</c> helpers.
    /// </summary>
    public struct RapierQueryFilter
    {
        public RapierQueryFilterFlags Flags;
        public bool UseCollisionGroups;
        public uint CollisionGroups;
        public bool HasExcludeCollider;
        public RapierColliderHandle ExcludeCollider;
        public bool HasExcludeBody;
        public RapierRigidBodyHandle ExcludeBody;

        public static RapierQueryFilter Default => default;

        public RapierQueryFilter WithFlags(RapierQueryFilterFlags flags)
        {
            Flags = flags;
            return this;
        }

        public RapierQueryFilter WithGroups(uint collisionGroups)
        {
            UseCollisionGroups = true;
            CollisionGroups = collisionGroups;
            return this;
        }

        public RapierQueryFilter ExcludingCollider(RapierColliderHandle collider)
        {
            HasExcludeCollider = true;
            ExcludeCollider = collider;
            return this;
        }

        public RapierQueryFilter ExcludingBody(RapierRigidBodyHandle body)
        {
            HasExcludeBody = true;
            ExcludeBody = body;
            return this;
        }

        internal RapierNative.QueryFilterNative ToNative()
        {
            return new RapierNative.QueryFilterNative
            {
                Flags = (uint)Flags,
                UseGroups = UseCollisionGroups ? (byte)1 : (byte)0,
                Groups = CollisionGroups,
                UseExcludeCollider = HasExcludeCollider ? (byte)1 : (byte)0,
                ExcludeCollider = ExcludeCollider,
                UseExcludeBody = HasExcludeBody ? (byte)1 : (byte)0,
                ExcludeBody = ExcludeBody
            };
        }
    }

    /// <summary>
    /// Result of projecting a point onto the closest collider.
    /// </summary>
    public readonly struct RapierPointProjection
    {
        public readonly RapierColliderHandle Collider;
        public readonly Vector3 Point;
        public readonly bool IsInside;

        public RapierPointProjection(RapierColliderHandle collider, Vector3 point, bool isInside)
        {
            Collider = collider;
            Point = point;
            IsInside = isInside;
        }
    }
}
