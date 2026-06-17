using UnityEngine;

namespace AFJK.Rapier
{
    /// <summary>
    /// Configuration for a kinematic character-controller move. Lengths are in world
    /// units; angles are in radians. Mirrors Rapier's <c>KinematicCharacterController</c>.
    /// </summary>
    public struct RapierCharacterController
    {
        public Vector3 Up;
        public float Offset;
        public bool Slide;
        public bool AutostepEnabled;
        public float AutostepMaxHeight;
        public float AutostepMinWidth;
        public bool AutostepIncludeDynamicBodies;
        public float MaxSlopeClimbAngle;
        public float MinSlopeSlideAngle;
        public bool SnapToGroundEnabled;
        public float SnapToGroundDistance;
        public float NormalNudgeFactor;

        /// <summary>A controller with sensible defaults (up = +Y, sliding enabled).</summary>
        public static RapierCharacterController Default => new RapierCharacterController
        {
            Up = Vector3.up,
            Offset = 0.01f,
            Slide = true,
            AutostepEnabled = false,
            AutostepMaxHeight = 0.25f,
            AutostepMinWidth = 0.1f,
            AutostepIncludeDynamicBodies = false,
            MaxSlopeClimbAngle = 45f * Mathf.Deg2Rad,
            MinSlopeSlideAngle = 30f * Mathf.Deg2Rad,
            SnapToGroundEnabled = false,
            SnapToGroundDistance = 0.1f,
            NormalNudgeFactor = 1.0e-4f
        };

        internal RapierNative.CharacterControllerDescNative ToNative()
        {
            return new RapierNative.CharacterControllerDescNative
            {
                Up = Up == Vector3.zero ? Vector3.up : Up,
                Offset = Offset,
                Slide = Slide ? (byte)1 : (byte)0,
                AutostepEnabled = AutostepEnabled ? (byte)1 : (byte)0,
                AutostepMaxHeight = AutostepMaxHeight,
                AutostepMinWidth = AutostepMinWidth,
                AutostepIncludeDynamic = AutostepIncludeDynamicBodies ? (byte)1 : (byte)0,
                MaxSlopeClimbAngle = MaxSlopeClimbAngle,
                MinSlopeSlideAngle = MinSlopeSlideAngle,
                SnapToGroundEnabled = SnapToGroundEnabled ? (byte)1 : (byte)0,
                SnapToGroundDistance = SnapToGroundDistance,
                NormalNudgeFactor = NormalNudgeFactor
            };
        }
    }

    /// <summary>The computed result of a character-controller move.</summary>
    public readonly struct RapierCharacterMovement
    {
        public readonly Vector3 Translation;
        public readonly bool Grounded;
        public readonly bool IsSlidingDownSlope;

        internal RapierCharacterMovement(RapierNative.CharacterMovementNative native)
        {
            Translation = native.Translation;
            Grounded = native.Grounded != 0;
            IsSlidingDownSlope = native.IsSlidingDownSlope != 0;
        }
    }
}
