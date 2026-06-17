using UnityEngine;

namespace AFJK.Rapier
{
    /// <summary>Primitive shape kind for shape-cast and shape-intersection queries.</summary>
    public enum RapierShapeType : uint
    {
        Ball = 0,
        Cuboid = 1,
        Capsule = 2
    }

    /// <summary>Outcome classification of a shape cast, mirroring parry's <c>ShapeCastStatus</c>.</summary>
    public enum RapierShapeCastStatus : uint
    {
        OutOfIterations = 0,
        Converged = 1,
        Failed = 2,
        PenetratingOrWithinTargetDistance = 3
    }

    /// <summary>
    /// A primitive shape used by <see cref="RapierWorld.CastShape"/> and
    /// <see cref="RapierWorld.IntersectShape"/>. Build with the static helpers.
    /// </summary>
    public struct RapierQueryShape
    {
        public RapierShapeType ShapeType;
        public Vector3 HalfExtents;
        public float Radius;
        public float HalfHeight;

        public static RapierQueryShape Ball(float radius) => new RapierQueryShape
        {
            ShapeType = RapierShapeType.Ball,
            Radius = radius
        };

        public static RapierQueryShape Cuboid(Vector3 halfExtents) => new RapierQueryShape
        {
            ShapeType = RapierShapeType.Cuboid,
            HalfExtents = halfExtents
        };

        public static RapierQueryShape Capsule(float halfHeight, float radius) => new RapierQueryShape
        {
            ShapeType = RapierShapeType.Capsule,
            HalfHeight = halfHeight,
            Radius = radius
        };

        internal RapierNative.QueryShapeNative ToNative()
        {
            return new RapierNative.QueryShapeNative
            {
                ShapeType = (uint)ShapeType,
                HalfExtents = HalfExtents,
                Radius = Radius,
                HalfHeight = HalfHeight
            };
        }
    }

    /// <summary>Result of casting a shape against the closest collider.</summary>
    public readonly struct RapierShapeCastHit
    {
        public readonly RapierColliderHandle Collider;
        public readonly float TimeOfImpact;
        public readonly Vector3 Witness1;
        public readonly Vector3 Witness2;
        public readonly Vector3 Normal1;
        public readonly Vector3 Normal2;
        public readonly RapierShapeCastStatus Status;

        internal RapierShapeCastHit(RapierNative.ShapeCastHitNative native)
        {
            Collider = native.Collider;
            TimeOfImpact = native.TimeOfImpact;
            Witness1 = native.Witness1;
            Witness2 = native.Witness2;
            Normal1 = native.Normal1;
            Normal2 = native.Normal2;
            Status = (RapierShapeCastStatus)native.Status;
        }
    }
}
