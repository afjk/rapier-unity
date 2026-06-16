using UnityEngine;

namespace AFJK.Rapier
{
    public struct RapierBoxColliderDesc
    {
        private const float DefaultFriction = 0.5f;

        public Vector3 HalfExtents;
        public float Density;
        public float Friction;
        public bool HasFriction;
        public float Restitution;
        public bool IsSensor;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;

        public static RapierBoxColliderDesc Unit => new RapierBoxColliderDesc
        {
            HalfExtents = Vector3.one * 0.5f,
            Density = 1f,
            Friction = 0.5f,
            HasFriction = true,
            Restitution = 0f,
            LocalRotation = Quaternion.identity
        };

        internal RapierNative.BoxColliderDescNative ToNative()
        {
            return new RapierNative.BoxColliderDescNative
            {
                HalfExtents = HalfExtents,
                Density = Density,
                Friction = HasFriction || Friction > 0f ? Mathf.Max(0f, Friction) : DefaultFriction,
                Restitution = Restitution,
                IsSensor = IsSensor ? (byte)1 : (byte)0,
                LocalPosition = LocalPosition,
                LocalRotation = LocalRotation == default(Quaternion) ? Quaternion.identity : LocalRotation
            };
        }
    }

    public struct RapierSphereColliderDesc
    {
        private const float DefaultFriction = 0.5f;

        public float Radius;
        public float Density;
        public float Friction;
        public bool HasFriction;
        public float Restitution;
        public bool IsSensor;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;

        public static RapierSphereColliderDesc Unit => new RapierSphereColliderDesc
        {
            Radius = 0.5f,
            Density = 1f,
            Friction = 0.5f,
            HasFriction = true,
            Restitution = 0f,
            LocalRotation = Quaternion.identity
        };

        internal RapierNative.SphereColliderDescNative ToNative()
        {
            return new RapierNative.SphereColliderDescNative
            {
                Radius = Radius,
                Density = Density,
                Friction = HasFriction || Friction > 0f ? Mathf.Max(0f, Friction) : DefaultFriction,
                Restitution = Restitution,
                IsSensor = IsSensor ? (byte)1 : (byte)0,
                LocalPosition = LocalPosition,
                LocalRotation = LocalRotation == default(Quaternion) ? Quaternion.identity : LocalRotation
            };
        }
    }

    public struct RapierCapsuleColliderDesc
    {
        private const float DefaultFriction = 0.5f;

        public float HalfHeight;
        public float Radius;
        public float Density;
        public float Friction;
        public bool HasFriction;
        public float Restitution;
        public bool IsSensor;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;

        public static RapierCapsuleColliderDesc Unit => new RapierCapsuleColliderDesc
        {
            HalfHeight = 0.5f,
            Radius = 0.25f,
            Density = 1f,
            Friction = 0.5f,
            HasFriction = true,
            Restitution = 0f,
            LocalRotation = Quaternion.identity
        };

        internal RapierNative.CapsuleColliderDescNative ToNative()
        {
            return new RapierNative.CapsuleColliderDescNative
            {
                HalfHeight = HalfHeight,
                Radius = Radius,
                Density = Density,
                Friction = HasFriction || Friction > 0f ? Mathf.Max(0f, Friction) : DefaultFriction,
                Restitution = Restitution,
                IsSensor = IsSensor ? (byte)1 : (byte)0,
                LocalPosition = LocalPosition,
                LocalRotation = LocalRotation == default(Quaternion) ? Quaternion.identity : LocalRotation
            };
        }
    }
}
