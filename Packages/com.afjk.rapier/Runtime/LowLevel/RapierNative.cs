using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AFJK.Rapier
{
    internal static class RapierNative
    {
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_WEBGL)
        private const string DllName = "__Internal";
#else
        private const string DllName = "rapier_unity_ffi";
#endif

        private const CallingConvention Convention = CallingConvention.Cdecl;

        [StructLayout(LayoutKind.Sequential)]
        internal struct RigidBodyDescNative
        {
            public RapierRigidBodyType BodyType;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 LinearVelocity;
            public Vector3 AngularVelocity;
            public float LinearDamping;
            public float AngularDamping;
            public byte CanSleep;
            public byte CcdEnabled;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BoxColliderDescNative
        {
            public Vector3 HalfExtents;
            public float Density;
            public byte IsSensor;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SphereColliderDescNative
        {
            public float Radius;
            public float Density;
            public byte IsSensor;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CapsuleColliderDescNative
        {
            public float HalfHeight;
            public float Radius;
            public float Density;
            public byte IsSensor;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RayNative
        {
            public Vector3 Origin;
            public Vector3 Direction;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RaycastHitNative
        {
            public RapierColliderHandle Collider;
            public Vector3 Point;
            public Vector3 Normal;
            public float Toi;
        }

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_world_create")]
        internal static extern ulong WorldCreate();

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_world_destroy")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool WorldDestroy(ulong world);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_world_set_gravity")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool WorldSetGravity(ulong world, float x, float y, float z);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_world_set_timestep")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool WorldSetTimestep(ulong world, float dt);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_world_step")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool WorldStep(ulong world);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_create")]
        internal static extern RapierRigidBodyHandle BodyCreate(ulong world, RigidBodyDescNative desc);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_destroy")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodyDestroy(ulong world, RapierRigidBodyHandle body);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_get_transform")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodyGetTransform(
            ulong world,
            RapierRigidBodyHandle body,
            out RapierTransform transform);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_set_transform")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodySetTransform(
            ulong world,
            RapierRigidBodyHandle body,
            RapierTransform transform);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_create_box")]
        internal static extern RapierColliderHandle ColliderCreateBox(
            ulong world,
            RapierRigidBodyHandle body,
            BoxColliderDescNative desc);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_create_sphere")]
        internal static extern RapierColliderHandle ColliderCreateSphere(
            ulong world,
            RapierRigidBodyHandle body,
            SphereColliderDescNative desc);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_create_capsule")]
        internal static extern RapierColliderHandle ColliderCreateCapsule(
            ulong world,
            RapierRigidBodyHandle body,
            CapsuleColliderDescNative desc);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_destroy")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderDestroy(ulong world, RapierColliderHandle collider);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_raycast")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool Raycast(
            ulong world,
            RayNative ray,
            float maxToi,
            out RaycastHitNative hit);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_world_state_hash")]
        internal static extern ulong WorldStateHash(ulong world);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_world_snapshot_size")]
        internal static extern UIntPtr WorldSnapshotSize(ulong world);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_world_snapshot_write")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool WorldSnapshotWrite(ulong world, IntPtr outBytes, UIntPtr len);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_world_snapshot_read")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool WorldSnapshotRead(ulong world, IntPtr bytes, UIntPtr len);
    }
}

