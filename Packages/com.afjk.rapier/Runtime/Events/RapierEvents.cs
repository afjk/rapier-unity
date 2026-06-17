using System;
using UnityEngine;

namespace AFJK.Rapier
{
    /// <summary>
    /// Which event kinds a collider generates. Mirrors Rapier's <c>ActiveEvents</c>.
    /// At least one collider in a pair must opt in for events to be reported.
    /// </summary>
    [Flags]
    public enum RapierActiveEvents : uint
    {
        None = 0,
        CollisionEvents = 0b0001,
        ContactForceEvents = 0b0010
    }

    /// <summary>
    /// Which body-type pairs are eligible for collision detection. Mirrors Rapier's
    /// <c>ActiveCollisionTypes</c> bit values.
    /// </summary>
    [Flags]
    public enum RapierActiveCollisionTypes : uint
    {
        DynamicDynamic = 0b0000_0000_0000_0001,
        DynamicKinematic = 0b0000_0000_0000_1100,
        DynamicFixed = 0b0000_0000_0000_0010,
        KinematicKinematic = 0b1100_1100_0000_0000,
        KinematicFixed = 0b0010_0010_0000_0000,
        FixedFixed = 0b0000_0000_0010_0000
    }

    /// <summary>A collision (intersection) event between two colliders during a step.</summary>
    public readonly struct RapierCollisionEvent
    {
        public readonly RapierColliderHandle Collider1;
        public readonly RapierColliderHandle Collider2;
        public readonly bool Started;
        public readonly uint Flags;

        internal RapierCollisionEvent(RapierNative.CollisionEventNative native)
        {
            Collider1 = native.Collider1;
            Collider2 = native.Collider2;
            Started = native.Started != 0;
            Flags = native.Flags;
        }
    }

    /// <summary>A contact-force event between two colliders during a step.</summary>
    public readonly struct RapierContactForceEvent
    {
        public readonly RapierColliderHandle Collider1;
        public readonly RapierColliderHandle Collider2;
        public readonly Vector3 TotalForce;
        public readonly float TotalForceMagnitude;
        public readonly Vector3 MaxForceDirection;
        public readonly float MaxForceMagnitude;

        internal RapierContactForceEvent(RapierNative.ContactForceEventNative native)
        {
            Collider1 = native.Collider1;
            Collider2 = native.Collider2;
            TotalForce = native.TotalForce;
            TotalForceMagnitude = native.TotalForceMagnitude;
            MaxForceDirection = native.MaxForceDirection;
            MaxForceMagnitude = native.MaxForceMagnitude;
        }
    }
}
