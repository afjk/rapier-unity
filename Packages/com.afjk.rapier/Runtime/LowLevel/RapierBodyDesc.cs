using UnityEngine;

namespace AFJK.Rapier
{
    public enum RapierRigidBodyType
    {
        Dynamic = 0,
        Fixed = 1,
        KinematicPositionBased = 2,
        KinematicVelocityBased = 3
    }

    public struct RapierBodyDesc
    {
        public RapierRigidBodyType BodyType;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 LinearVelocity;
        public Vector3 AngularVelocity;
        public float LinearDamping;
        public float AngularDamping;
        public bool CanSleep;
        public bool CcdEnabled;

        public static RapierBodyDesc Dynamic(Vector3 position)
        {
            return new RapierBodyDesc
            {
                BodyType = RapierRigidBodyType.Dynamic,
                Position = position,
                Rotation = Quaternion.identity,
                CanSleep = true
            };
        }

        public static RapierBodyDesc Fixed(Vector3 position)
        {
            return new RapierBodyDesc
            {
                BodyType = RapierRigidBodyType.Fixed,
                Position = position,
                Rotation = Quaternion.identity
            };
        }

        internal RapierNative.RigidBodyDescNative ToNative()
        {
            var rotation = Rotation == default(Quaternion) ? Quaternion.identity : Rotation;

            return new RapierNative.RigidBodyDescNative
            {
                BodyType = BodyType,
                Position = Position,
                Rotation = rotation,
                LinearVelocity = LinearVelocity,
                AngularVelocity = AngularVelocity,
                LinearDamping = LinearDamping,
                AngularDamping = AngularDamping,
                CanSleep = CanSleep ? (byte)1 : (byte)0,
                CcdEnabled = CcdEnabled ? (byte)1 : (byte)0
            };
        }
    }
}
