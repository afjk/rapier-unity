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
            public uint BodyType;
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
            public float Friction;
            public float Restitution;
            public byte IsSensor;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SphereColliderDescNative
        {
            public float Radius;
            public float Density;
            public float Friction;
            public float Restitution;
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
            public float Friction;
            public float Restitution;
            public byte IsSensor;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MeshColliderDescNative
        {
            public float Density;
            public float Friction;
            public float Restitution;
            public byte IsSensor;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RigidBodyStateNative
        {
            public RapierTransform Transform;
            public Vector3 LinearVelocity;
            public Vector3 AngularVelocity;
            public byte Sleeping;
            public byte Enabled;
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

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_stable_id_hash")]
        internal static extern ulong StableIdHash(IntPtr bytes, UIntPtr len);

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

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_set_stable_id")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodySetStableId(
            ulong world,
            RapierRigidBodyHandle body,
            ulong stableId);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_get_transform")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodyGetTransform(
            ulong world,
            RapierRigidBodyHandle body,
            out RapierTransform transform);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_get_state")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodyGetState(
            ulong world,
            RapierRigidBodyHandle body,
            out RigidBodyStateNative state);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_set_transform")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodySetTransform(
            ulong world,
            RapierRigidBodyHandle body,
            RapierTransform transform);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_get_linvel")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodyGetLinvel(
            ulong world,
            RapierRigidBodyHandle body,
            out Vector3 velocity);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_set_linvel")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodySetLinvel(
            ulong world,
            RapierRigidBodyHandle body,
            Vector3 velocity,
            [MarshalAs(UnmanagedType.I1)] bool wakeUp);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_get_angvel")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodyGetAngvel(
            ulong world,
            RapierRigidBodyHandle body,
            out Vector3 velocity);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_set_angvel")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodySetAngvel(
            ulong world,
            RapierRigidBodyHandle body,
            Vector3 velocity,
            [MarshalAs(UnmanagedType.I1)] bool wakeUp);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_get_linear_damping")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodyGetLinearDamping(
            ulong world,
            RapierRigidBodyHandle body,
            out float damping);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_set_linear_damping")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodySetLinearDamping(
            ulong world,
            RapierRigidBodyHandle body,
            float damping);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_get_angular_damping")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodyGetAngularDamping(
            ulong world,
            RapierRigidBodyHandle body,
            out float damping);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_set_angular_damping")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodySetAngularDamping(
            ulong world,
            RapierRigidBodyHandle body,
            float damping);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_get_gravity_scale")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodyGetGravityScale(
            ulong world,
            RapierRigidBodyHandle body,
            out float scale);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_set_gravity_scale")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodySetGravityScale(
            ulong world,
            RapierRigidBodyHandle body,
            float scale,
            [MarshalAs(UnmanagedType.I1)] bool wakeUp);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_get_ccd_enabled")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodyGetCcdEnabled(
            ulong world,
            RapierRigidBodyHandle body,
            [MarshalAs(UnmanagedType.I1)] out bool enabled);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_set_ccd_enabled")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodySetCcdEnabled(
            ulong world,
            RapierRigidBodyHandle body,
            [MarshalAs(UnmanagedType.I1)] bool enabled);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_get_enabled")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodyGetEnabled(
            ulong world,
            RapierRigidBodyHandle body,
            [MarshalAs(UnmanagedType.I1)] out bool enabled);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_set_enabled")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodySetEnabled(
            ulong world,
            RapierRigidBodyHandle body,
            [MarshalAs(UnmanagedType.I1)] bool enabled);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_add_force")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodyAddForce(
            ulong world,
            RapierRigidBodyHandle body,
            Vector3 force,
            [MarshalAs(UnmanagedType.I1)] bool wakeUp);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_add_torque")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodyAddTorque(
            ulong world,
            RapierRigidBodyHandle body,
            Vector3 torque,
            [MarshalAs(UnmanagedType.I1)] bool wakeUp);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_apply_impulse")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodyApplyImpulse(
            ulong world,
            RapierRigidBodyHandle body,
            Vector3 impulse,
            [MarshalAs(UnmanagedType.I1)] bool wakeUp);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_apply_torque_impulse")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodyApplyTorqueImpulse(
            ulong world,
            RapierRigidBodyHandle body,
            Vector3 impulse,
            [MarshalAs(UnmanagedType.I1)] bool wakeUp);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_set_next_kinematic_translation")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodySetNextKinematicTranslation(
            ulong world,
            RapierRigidBodyHandle body,
            Vector3 translation);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_body_set_next_kinematic_rotation")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool BodySetNextKinematicRotation(
            ulong world,
            RapierRigidBodyHandle body,
            RapierTransform rotation);

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

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_create_trimesh")]
        internal static extern RapierColliderHandle ColliderCreateTrimesh(
            ulong world,
            RapierRigidBodyHandle body,
            IntPtr vertices,
            UIntPtr vertexCount,
            IntPtr indices,
            UIntPtr indexCount,
            MeshColliderDescNative desc);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_create_convex_hull")]
        internal static extern RapierColliderHandle ColliderCreateConvexHull(
            ulong world,
            RapierRigidBodyHandle body,
            IntPtr vertices,
            UIntPtr vertexCount,
            MeshColliderDescNative desc);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_create_heightfield")]
        internal static extern RapierColliderHandle ColliderCreateHeightfield(
            ulong world,
            RapierRigidBodyHandle body,
            IntPtr heights,
            UIntPtr rows,
            UIntPtr columns,
            Vector3 scale,
            MeshColliderDescNative desc);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_destroy")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderDestroy(ulong world, RapierColliderHandle collider);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_set_stable_id")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderSetStableId(
            ulong world,
            RapierColliderHandle collider,
            ulong stableId);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_get_friction")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderGetFriction(ulong world, RapierColliderHandle collider, out float friction);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_set_friction")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderSetFriction(ulong world, RapierColliderHandle collider, float friction);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_get_restitution")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderGetRestitution(ulong world, RapierColliderHandle collider, out float restitution);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_set_restitution")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderSetRestitution(ulong world, RapierColliderHandle collider, float restitution);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_get_friction_combine_rule")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderGetFrictionCombineRule(ulong world, RapierColliderHandle collider, out uint rule);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_set_friction_combine_rule")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderSetFrictionCombineRule(ulong world, RapierColliderHandle collider, uint rule);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_get_restitution_combine_rule")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderGetRestitutionCombineRule(ulong world, RapierColliderHandle collider, out uint rule);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_set_restitution_combine_rule")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderSetRestitutionCombineRule(ulong world, RapierColliderHandle collider, uint rule);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_get_collision_groups")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderGetCollisionGroups(ulong world, RapierColliderHandle collider, out uint groups);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_set_collision_groups")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderSetCollisionGroups(ulong world, RapierColliderHandle collider, uint groups);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_get_solver_groups")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderGetSolverGroups(ulong world, RapierColliderHandle collider, out uint groups);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_set_solver_groups")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderSetSolverGroups(ulong world, RapierColliderHandle collider, uint groups);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_get_sensor")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderGetSensor(ulong world, RapierColliderHandle collider, [MarshalAs(UnmanagedType.I1)] out bool isSensor);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_set_sensor")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderSetSensor(ulong world, RapierColliderHandle collider, [MarshalAs(UnmanagedType.I1)] bool isSensor);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_get_enabled")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderGetEnabled(ulong world, RapierColliderHandle collider, [MarshalAs(UnmanagedType.I1)] out bool enabled);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_set_enabled")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderSetEnabled(ulong world, RapierColliderHandle collider, [MarshalAs(UnmanagedType.I1)] bool enabled);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_get_density")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderGetDensity(ulong world, RapierColliderHandle collider, out float density);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_set_density")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderSetDensity(ulong world, RapierColliderHandle collider, float density);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_set_translation_wrt_parent")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderSetTranslationWrtParent(ulong world, RapierColliderHandle collider, Vector3 translation);

        [DllImport(DllName, CallingConvention = Convention, EntryPoint = "rapier_unity_collider_set_position_wrt_parent")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool ColliderSetPositionWrtParent(ulong world, RapierColliderHandle collider, RapierTransform transform);

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
