using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace AFJK.Rapier
{
    public sealed class RapierWorld : IDisposable
    {
        private ulong world;

        private RapierWorld(ulong world)
        {
            this.world = world;
        }

        public ulong NativeHandle => world;

        public bool IsCreated => world != 0;

        public static RapierWorld Create()
        {
            var handle = RapierNative.WorldCreate();
            if (handle == 0)
            {
                throw new InvalidOperationException("Failed to create Rapier world.");
            }

            return new RapierWorld(handle);
        }

        public static ulong StableIdHash(string stableId)
        {
            if (string.IsNullOrEmpty(stableId))
            {
                return 0;
            }

            var bytes = Encoding.UTF8.GetBytes(stableId);
            var handle = default(GCHandle);
            try
            {
                handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                return RapierNative.StableIdHash(handle.AddrOfPinnedObject(), (UIntPtr)bytes.Length);
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }

        public bool SetGravity(Vector3 gravity)
        {
            ThrowIfDisposed();
            return RapierNative.WorldSetGravity(world, gravity.x, gravity.y, gravity.z);
        }

        public bool SetTimestep(float dt)
        {
            ThrowIfDisposed();
            return RapierNative.WorldSetTimestep(world, dt);
        }

        public bool Step()
        {
            ThrowIfDisposed();
            return RapierNative.WorldStep(world);
        }

        public RapierRigidBodyHandle CreateRigidBody(RapierBodyDesc desc)
        {
            ThrowIfDisposed();
            return RapierNative.BodyCreate(world, desc.ToNative());
        }

        public bool DestroyRigidBody(RapierRigidBodyHandle body)
        {
            ThrowIfDisposed();
            return RapierNative.BodyDestroy(world, body);
        }

        public bool SetRigidBodyStableId(RapierRigidBodyHandle body, ulong stableId)
        {
            ThrowIfDisposed();
            return RapierNative.BodySetStableId(world, body, stableId);
        }

        public bool TryGetTransform(RapierRigidBodyHandle body, out RapierTransform transform)
        {
            ThrowIfDisposed();
            return RapierNative.BodyGetTransform(world, body, out transform);
        }

        public bool TryGetRigidBodyState(RapierRigidBodyHandle body, out RapierRigidBodyState state)
        {
            ThrowIfDisposed();
            if (RapierNative.BodyGetState(world, body, out var nativeState))
            {
                state = new RapierRigidBodyState(nativeState);
                return true;
            }

            state = default;
            return false;
        }

        public bool SetTransform(RapierRigidBodyHandle body, RapierTransform transform)
        {
            ThrowIfDisposed();
            return RapierNative.BodySetTransform(world, body, transform);
        }

        public bool TryGetLinearVelocity(RapierRigidBodyHandle body, out Vector3 velocity)
        {
            ThrowIfDisposed();
            return RapierNative.BodyGetLinvel(world, body, out velocity);
        }

        public bool SetLinearVelocity(RapierRigidBodyHandle body, Vector3 velocity, bool wakeUp = true)
        {
            ThrowIfDisposed();
            return RapierNative.BodySetLinvel(world, body, velocity, wakeUp);
        }

        public bool TryGetAngularVelocity(RapierRigidBodyHandle body, out Vector3 velocity)
        {
            ThrowIfDisposed();
            return RapierNative.BodyGetAngvel(world, body, out velocity);
        }

        public bool SetAngularVelocity(RapierRigidBodyHandle body, Vector3 velocity, bool wakeUp = true)
        {
            ThrowIfDisposed();
            return RapierNative.BodySetAngvel(world, body, velocity, wakeUp);
        }

        public bool TryGetLinearDamping(RapierRigidBodyHandle body, out float damping)
        {
            ThrowIfDisposed();
            return RapierNative.BodyGetLinearDamping(world, body, out damping);
        }

        public bool SetLinearDamping(RapierRigidBodyHandle body, float damping)
        {
            ThrowIfDisposed();
            return RapierNative.BodySetLinearDamping(world, body, damping);
        }

        public bool TryGetAngularDamping(RapierRigidBodyHandle body, out float damping)
        {
            ThrowIfDisposed();
            return RapierNative.BodyGetAngularDamping(world, body, out damping);
        }

        public bool SetAngularDamping(RapierRigidBodyHandle body, float damping)
        {
            ThrowIfDisposed();
            return RapierNative.BodySetAngularDamping(world, body, damping);
        }

        public bool TryGetGravityScale(RapierRigidBodyHandle body, out float scale)
        {
            ThrowIfDisposed();
            return RapierNative.BodyGetGravityScale(world, body, out scale);
        }

        public bool SetGravityScale(RapierRigidBodyHandle body, float scale, bool wakeUp = true)
        {
            ThrowIfDisposed();
            return RapierNative.BodySetGravityScale(world, body, scale, wakeUp);
        }

        public bool TryGetCcdEnabled(RapierRigidBodyHandle body, out bool enabled)
        {
            ThrowIfDisposed();
            return RapierNative.BodyGetCcdEnabled(world, body, out enabled);
        }

        public bool SetCcdEnabled(RapierRigidBodyHandle body, bool enabled)
        {
            ThrowIfDisposed();
            return RapierNative.BodySetCcdEnabled(world, body, enabled);
        }

        public bool TryGetBodyEnabled(RapierRigidBodyHandle body, out bool enabled)
        {
            ThrowIfDisposed();
            return RapierNative.BodyGetEnabled(world, body, out enabled);
        }

        public bool SetBodyEnabled(RapierRigidBodyHandle body, bool enabled)
        {
            ThrowIfDisposed();
            return RapierNative.BodySetEnabled(world, body, enabled);
        }

        public bool AddForce(RapierRigidBodyHandle body, Vector3 force, bool wakeUp = true)
        {
            ThrowIfDisposed();
            return RapierNative.BodyAddForce(world, body, force, wakeUp);
        }

        public bool AddTorque(RapierRigidBodyHandle body, Vector3 torque, bool wakeUp = true)
        {
            ThrowIfDisposed();
            return RapierNative.BodyAddTorque(world, body, torque, wakeUp);
        }

        public bool ApplyImpulse(RapierRigidBodyHandle body, Vector3 impulse, bool wakeUp = true)
        {
            ThrowIfDisposed();
            return RapierNative.BodyApplyImpulse(world, body, impulse, wakeUp);
        }

        public bool ApplyTorqueImpulse(RapierRigidBodyHandle body, Vector3 impulse, bool wakeUp = true)
        {
            ThrowIfDisposed();
            return RapierNative.BodyApplyTorqueImpulse(world, body, impulse, wakeUp);
        }

        public bool SetNextKinematicTranslation(RapierRigidBodyHandle body, Vector3 translation)
        {
            ThrowIfDisposed();
            return RapierNative.BodySetNextKinematicTranslation(world, body, translation);
        }

        public bool SetNextKinematicRotation(RapierRigidBodyHandle body, Quaternion rotation)
        {
            ThrowIfDisposed();
            var transform = new RapierTransform(Vector3.zero, rotation);
            return RapierNative.BodySetNextKinematicRotation(world, body, transform);
        }

        public RapierColliderHandle CreateBoxCollider(
            RapierRigidBodyHandle body,
            RapierBoxColliderDesc desc)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderCreateBox(world, body, desc.ToNative());
        }

        public RapierColliderHandle CreateSphereCollider(
            RapierRigidBodyHandle body,
            RapierSphereColliderDesc desc)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderCreateSphere(world, body, desc.ToNative());
        }

        public RapierColliderHandle CreateCapsuleCollider(
            RapierRigidBodyHandle body,
            RapierCapsuleColliderDesc desc)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderCreateCapsule(world, body, desc.ToNative());
        }

        public RapierColliderHandle CreateTrimeshCollider(
            RapierRigidBodyHandle body,
            Vector3[] vertices,
            int[] indices,
            RapierMeshColliderDesc desc)
        {
            ThrowIfDisposed();
            if (vertices == null || vertices.Length == 0 || indices == null || indices.Length == 0)
            {
                return RapierColliderHandle.Invalid;
            }

            var vertexHandle = default(GCHandle);
            var indexHandle = default(GCHandle);
            try
            {
                vertexHandle = GCHandle.Alloc(vertices, GCHandleType.Pinned);
                indexHandle = GCHandle.Alloc(indices, GCHandleType.Pinned);
                return RapierNative.ColliderCreateTrimesh(
                    world,
                    body,
                    vertexHandle.AddrOfPinnedObject(),
                    (UIntPtr)vertices.Length,
                    indexHandle.AddrOfPinnedObject(),
                    (UIntPtr)indices.Length,
                    desc.ToNative());
            }
            finally
            {
                if (vertexHandle.IsAllocated)
                {
                    vertexHandle.Free();
                }

                if (indexHandle.IsAllocated)
                {
                    indexHandle.Free();
                }
            }
        }

        public RapierColliderHandle CreateConvexHullCollider(
            RapierRigidBodyHandle body,
            Vector3[] vertices,
            RapierMeshColliderDesc desc)
        {
            ThrowIfDisposed();
            if (vertices == null || vertices.Length == 0)
            {
                return RapierColliderHandle.Invalid;
            }

            var vertexHandle = default(GCHandle);
            try
            {
                vertexHandle = GCHandle.Alloc(vertices, GCHandleType.Pinned);
                return RapierNative.ColliderCreateConvexHull(
                    world,
                    body,
                    vertexHandle.AddrOfPinnedObject(),
                    (UIntPtr)vertices.Length,
                    desc.ToNative());
            }
            finally
            {
                if (vertexHandle.IsAllocated)
                {
                    vertexHandle.Free();
                }
            }
        }

        public RapierColliderHandle CreateHeightfieldCollider(
            RapierRigidBodyHandle body,
            float[] heights,
            int rows,
            int columns,
            Vector3 scale,
            RapierMeshColliderDesc desc)
        {
            ThrowIfDisposed();
            if (rows <= 0 || columns <= 0)
            {
                return RapierColliderHandle.Invalid;
            }

            if (heights == null || heights.Length != rows * columns)
            {
                throw new ArgumentException(
                    $"Expected {rows * columns} height samples (rows*columns) but received {heights?.Length ?? 0}.",
                    nameof(heights));
            }

            var heightHandle = default(GCHandle);
            try
            {
                heightHandle = GCHandle.Alloc(heights, GCHandleType.Pinned);
                return RapierNative.ColliderCreateHeightfield(
                    world,
                    body,
                    heightHandle.AddrOfPinnedObject(),
                    (UIntPtr)rows,
                    (UIntPtr)columns,
                    scale,
                    desc.ToNative());
            }
            finally
            {
                if (heightHandle.IsAllocated)
                {
                    heightHandle.Free();
                }
            }
        }

        public bool DestroyCollider(RapierColliderHandle collider)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderDestroy(world, collider);
        }

        public bool SetColliderStableId(RapierColliderHandle collider, ulong stableId)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderSetStableId(world, collider, stableId);
        }

        public bool TryGetColliderFriction(RapierColliderHandle collider, out float friction)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderGetFriction(world, collider, out friction);
        }

        public bool SetColliderFriction(RapierColliderHandle collider, float friction)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderSetFriction(world, collider, friction);
        }

        public bool TryGetColliderRestitution(RapierColliderHandle collider, out float restitution)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderGetRestitution(world, collider, out restitution);
        }

        public bool SetColliderRestitution(RapierColliderHandle collider, float restitution)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderSetRestitution(world, collider, restitution);
        }

        public bool TryGetColliderFrictionCombineRule(RapierColliderHandle collider, out RapierCoefficientCombineRule rule)
        {
            ThrowIfDisposed();
            if (RapierNative.ColliderGetFrictionCombineRule(world, collider, out var raw))
            {
                rule = (RapierCoefficientCombineRule)raw;
                return true;
            }

            rule = default;
            return false;
        }

        public bool SetColliderFrictionCombineRule(RapierColliderHandle collider, RapierCoefficientCombineRule rule)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderSetFrictionCombineRule(world, collider, (uint)rule);
        }

        public bool TryGetColliderRestitutionCombineRule(RapierColliderHandle collider, out RapierCoefficientCombineRule rule)
        {
            ThrowIfDisposed();
            if (RapierNative.ColliderGetRestitutionCombineRule(world, collider, out var raw))
            {
                rule = (RapierCoefficientCombineRule)raw;
                return true;
            }

            rule = default;
            return false;
        }

        public bool SetColliderRestitutionCombineRule(RapierColliderHandle collider, RapierCoefficientCombineRule rule)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderSetRestitutionCombineRule(world, collider, (uint)rule);
        }

        public bool TryGetColliderCollisionGroups(RapierColliderHandle collider, out uint groups)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderGetCollisionGroups(world, collider, out groups);
        }

        public bool SetColliderCollisionGroups(RapierColliderHandle collider, uint groups)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderSetCollisionGroups(world, collider, groups);
        }

        public bool TryGetColliderSolverGroups(RapierColliderHandle collider, out uint groups)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderGetSolverGroups(world, collider, out groups);
        }

        public bool SetColliderSolverGroups(RapierColliderHandle collider, uint groups)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderSetSolverGroups(world, collider, groups);
        }

        public bool TryGetColliderSensor(RapierColliderHandle collider, out bool isSensor)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderGetSensor(world, collider, out isSensor);
        }

        public bool SetColliderSensor(RapierColliderHandle collider, bool isSensor)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderSetSensor(world, collider, isSensor);
        }

        public bool TryGetColliderEnabled(RapierColliderHandle collider, out bool enabled)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderGetEnabled(world, collider, out enabled);
        }

        public bool SetColliderEnabled(RapierColliderHandle collider, bool enabled)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderSetEnabled(world, collider, enabled);
        }

        public bool TryGetColliderDensity(RapierColliderHandle collider, out float density)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderGetDensity(world, collider, out density);
        }

        public bool SetColliderDensity(RapierColliderHandle collider, float density)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderSetDensity(world, collider, density);
        }

        public bool SetColliderTranslationWrtParent(RapierColliderHandle collider, Vector3 translation)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderSetTranslationWrtParent(world, collider, translation);
        }

        public bool SetColliderPositionWrtParent(RapierColliderHandle collider, RapierTransform transform)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderSetPositionWrtParent(world, collider, transform);
        }

        /// <summary>
        /// Packs membership and filter group masks into Rapier's collision-groups
        /// encoding (memberships in the high 16 bits, filter in the low 16 bits).
        /// </summary>
        public static uint InteractionGroups(ushort memberships, ushort filter)
        {
            return ((uint)memberships << 16) | filter;
        }

        public bool Raycast(Ray ray, float maxDistance, out RapierRaycastHit hit)
        {
            ThrowIfDisposed();
            hit = default;

            if (maxDistance < 0f)
            {
                return false;
            }

            var direction = ray.direction;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            var nativeRay = new RapierNative.RayNative
            {
                Origin = ray.origin,
                Direction = direction.normalized
            };

            if (!RapierNative.Raycast(world, nativeRay, maxDistance, out var nativeHit))
            {
                return false;
            }

            hit = new RapierRaycastHit(
                nativeHit.Collider,
                nativeHit.Point,
                nativeHit.Normal,
                nativeHit.Toi);
            return true;
        }

        public bool RaycastFiltered(
            Ray ray,
            float maxDistance,
            bool solid,
            RapierQueryFilter filter,
            out RapierRaycastHit hit)
        {
            ThrowIfDisposed();
            hit = default;

            if (maxDistance < 0f)
            {
                return false;
            }

            var direction = ray.direction;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return false;
            }

            var nativeRay = new RapierNative.RayNative
            {
                Origin = ray.origin,
                Direction = direction.normalized
            };

            if (!RapierNative.RaycastFiltered(world, nativeRay, maxDistance, solid, filter.ToNative(), out var nativeHit))
            {
                return false;
            }

            hit = new RapierRaycastHit(
                nativeHit.Collider,
                nativeHit.Point,
                nativeHit.Normal,
                nativeHit.Toi);
            return true;
        }

        public bool TryProjectPoint(
            Vector3 point,
            bool solid,
            RapierQueryFilter filter,
            out RapierPointProjection projection)
        {
            ThrowIfDisposed();

            if (RapierNative.ProjectPoint(world, point.x, point.y, point.z, solid, filter.ToNative(), out var native))
            {
                projection = new RapierPointProjection(native.Collider, native.Point, native.IsInside != 0);
                return true;
            }

            projection = default;
            return false;
        }

        public bool TryIntersectionWithPoint(
            Vector3 point,
            RapierQueryFilter filter,
            out RapierColliderHandle collider)
        {
            ThrowIfDisposed();
            return RapierNative.IntersectionWithPoint(world, point.x, point.y, point.z, filter.ToNative(), out collider);
        }

        /// <summary>
        /// Casts a ray and writes every intersecting collider into <paramref name="results"/>,
        /// returning the number of hits written (capped at the array length).
        /// </summary>
        public int RaycastAll(
            Ray ray,
            float maxDistance,
            bool solid,
            RapierQueryFilter filter,
            RapierRaycastHit[] results)
        {
            ThrowIfDisposed();
            if (results == null || results.Length == 0 || maxDistance < 0f)
            {
                return 0;
            }

            var direction = ray.direction;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return 0;
            }

            var nativeRay = new RapierNative.RayNative
            {
                Origin = ray.origin,
                Direction = direction.normalized
            };

            var buffer = new RapierNative.RaycastHitNative[results.Length];
            var handle = default(GCHandle);
            int count;
            try
            {
                handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                count = (int)RapierNative.RaycastAll(
                    world,
                    nativeRay,
                    maxDistance,
                    solid,
                    filter.ToNative(),
                    handle.AddrOfPinnedObject(),
                    (UIntPtr)buffer.Length).ToUInt64();
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }

            for (var i = 0; i < count; i++)
            {
                var native = buffer[i];
                results[i] = new RapierRaycastHit(native.Collider, native.Point, native.Normal, native.Toi);
            }

            return count;
        }

        public bool CastShape(
            RapierTransform shapePosition,
            Vector3 shapeVelocity,
            RapierQueryShape shape,
            float maxDistance,
            bool stopAtPenetration,
            RapierQueryFilter filter,
            out RapierShapeCastHit hit)
        {
            ThrowIfDisposed();
            hit = default;

            if (maxDistance < 0f)
            {
                return false;
            }

            if (RapierNative.CastShape(
                world,
                shapePosition,
                shapeVelocity,
                shape.ToNative(),
                maxDistance,
                stopAtPenetration,
                filter.ToNative(),
                out var native))
            {
                hit = new RapierShapeCastHit(native);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Writes the handles of every collider intersecting <paramref name="shape"/> at
        /// <paramref name="shapePosition"/> into <paramref name="results"/>, returning the
        /// number written (capped at the array length).
        /// </summary>
        public int IntersectShape(
            RapierTransform shapePosition,
            RapierQueryShape shape,
            RapierQueryFilter filter,
            RapierColliderHandle[] results)
        {
            ThrowIfDisposed();
            if (results == null || results.Length == 0)
            {
                return 0;
            }

            var handle = default(GCHandle);
            try
            {
                handle = GCHandle.Alloc(results, GCHandleType.Pinned);
                return (int)RapierNative.IntersectShape(
                    world,
                    shapePosition,
                    shape.ToNative(),
                    filter.ToNative(),
                    handle.AddrOfPinnedObject(),
                    (UIntPtr)results.Length).ToUInt64();
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }

        public bool TryGetColliderActiveEvents(RapierColliderHandle collider, out RapierActiveEvents events)
        {
            ThrowIfDisposed();
            if (RapierNative.ColliderGetActiveEvents(world, collider, out var raw))
            {
                events = (RapierActiveEvents)raw;
                return true;
            }

            events = default;
            return false;
        }

        public bool SetColliderActiveEvents(RapierColliderHandle collider, RapierActiveEvents events)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderSetActiveEvents(world, collider, (uint)events);
        }

        public bool TryGetColliderActiveCollisionTypes(RapierColliderHandle collider, out RapierActiveCollisionTypes types)
        {
            ThrowIfDisposed();
            if (RapierNative.ColliderGetActiveCollisionTypes(world, collider, out var raw))
            {
                types = (RapierActiveCollisionTypes)raw;
                return true;
            }

            types = default;
            return false;
        }

        public bool SetColliderActiveCollisionTypes(RapierColliderHandle collider, RapierActiveCollisionTypes types)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderSetActiveCollisionTypes(world, collider, (uint)types);
        }

        public bool TryGetColliderContactForceEventThreshold(RapierColliderHandle collider, out float threshold)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderGetContactForceEventThreshold(world, collider, out threshold);
        }

        public bool SetColliderContactForceEventThreshold(RapierColliderHandle collider, float threshold)
        {
            ThrowIfDisposed();
            return RapierNative.ColliderSetContactForceEventThreshold(world, collider, threshold);
        }

        /// <summary>
        /// Copies collision events from the most recent step into <paramref name="results"/>,
        /// returning the number written (capped at the array length).
        /// </summary>
        public int DrainCollisionEvents(RapierCollisionEvent[] results)
        {
            ThrowIfDisposed();
            if (results == null || results.Length == 0)
            {
                return 0;
            }

            var buffer = new RapierNative.CollisionEventNative[results.Length];
            var handle = default(GCHandle);
            int count;
            try
            {
                handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                count = (int)RapierNative.DrainCollisionEvents(
                    world,
                    handle.AddrOfPinnedObject(),
                    (UIntPtr)buffer.Length).ToUInt64();
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }

            for (var i = 0; i < count; i++)
            {
                results[i] = new RapierCollisionEvent(buffer[i]);
            }

            return count;
        }

        /// <summary>
        /// Copies contact-force events from the most recent step into <paramref name="results"/>,
        /// returning the number written (capped at the array length).
        /// </summary>
        public int DrainContactForceEvents(RapierContactForceEvent[] results)
        {
            ThrowIfDisposed();
            if (results == null || results.Length == 0)
            {
                return 0;
            }

            var buffer = new RapierNative.ContactForceEventNative[results.Length];
            var handle = default(GCHandle);
            int count;
            try
            {
                handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                count = (int)RapierNative.DrainContactForceEvents(
                    world,
                    handle.AddrOfPinnedObject(),
                    (UIntPtr)buffer.Length).ToUInt64();
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }

            for (var i = 0; i < count; i++)
            {
                results[i] = new RapierContactForceEvent(buffer[i]);
            }

            return count;
        }

        public RapierJointHandle CreateFixedJoint(
            RapierRigidBodyHandle body1,
            RapierRigidBodyHandle body2,
            Vector3 anchor1,
            Vector3 anchor2)
        {
            ThrowIfDisposed();
            return RapierNative.JointCreateFixed(world, body1, body2, anchor1, anchor2);
        }

        public RapierJointHandle CreateSphericalJoint(
            RapierRigidBodyHandle body1,
            RapierRigidBodyHandle body2,
            Vector3 anchor1,
            Vector3 anchor2)
        {
            ThrowIfDisposed();
            return RapierNative.JointCreateSpherical(world, body1, body2, anchor1, anchor2);
        }

        public RapierJointHandle CreateRevoluteJoint(
            RapierRigidBodyHandle body1,
            RapierRigidBodyHandle body2,
            Vector3 anchor1,
            Vector3 anchor2,
            Vector3 axis)
        {
            ThrowIfDisposed();
            return RapierNative.JointCreateRevolute(world, body1, body2, anchor1, anchor2, axis);
        }

        public RapierJointHandle CreatePrismaticJoint(
            RapierRigidBodyHandle body1,
            RapierRigidBodyHandle body2,
            Vector3 anchor1,
            Vector3 anchor2,
            Vector3 axis)
        {
            ThrowIfDisposed();
            return RapierNative.JointCreatePrismatic(world, body1, body2, anchor1, anchor2, axis);
        }

        public bool RemoveJoint(RapierJointHandle joint)
        {
            ThrowIfDisposed();
            return RapierNative.JointRemove(world, joint);
        }

        public bool SetJointLimits(RapierJointHandle joint, RapierJointAxis axis, float min, float max)
        {
            ThrowIfDisposed();
            return RapierNative.JointSetLimits(world, joint, (uint)axis, min, max);
        }

        public bool SetJointMotorPosition(
            RapierJointHandle joint,
            RapierJointAxis axis,
            float targetPosition,
            float stiffness,
            float damping)
        {
            ThrowIfDisposed();
            return RapierNative.JointSetMotorPosition(world, joint, (uint)axis, targetPosition, stiffness, damping);
        }

        public bool SetJointMotorVelocity(
            RapierJointHandle joint,
            RapierJointAxis axis,
            float targetVelocity,
            float factor)
        {
            ThrowIfDisposed();
            return RapierNative.JointSetMotorVelocity(world, joint, (uint)axis, targetVelocity, factor);
        }

        public bool SetJointMotorMaxForce(RapierJointHandle joint, RapierJointAxis axis, float maxForce)
        {
            ThrowIfDisposed();
            return RapierNative.JointSetMotorMaxForce(world, joint, (uint)axis, maxForce);
        }

        /// <summary>
        /// Computes the collision-constrained movement of a kinematic character shape,
        /// without moving any body. Scene state reflects the most recent step.
        /// </summary>
        public bool MoveCharacter(
            RapierQueryShape shape,
            RapierTransform position,
            Vector3 desiredTranslation,
            float deltaTime,
            RapierCharacterController controller,
            RapierQueryFilter filter,
            out RapierCharacterMovement movement)
        {
            ThrowIfDisposed();

            if (RapierNative.CharacterControllerMove(
                world,
                shape.ToNative(),
                position,
                desiredTranslation,
                deltaTime,
                controller.ToNative(),
                filter.ToNative(),
                out var native))
            {
                movement = new RapierCharacterMovement(native);
                return true;
            }

            movement = default;
            return false;
        }

        public ulong StateHash()
        {
            ThrowIfDisposed();
            return RapierNative.WorldStateHash(world);
        }

        public int SnapshotSize()
        {
            ThrowIfDisposed();
            var size = RapierNative.WorldSnapshotSize(world).ToUInt64();
            if (size > int.MaxValue)
            {
                throw new InvalidOperationException($"Snapshot is too large for a managed byte array: {size} bytes.");
            }

            return (int)size;
        }

        public bool TryWriteSnapshot(byte[] buffer)
        {
            ThrowIfDisposed();
            if (buffer == null)
            {
                buffer = Array.Empty<byte>();
            }

            var handle = default(GCHandle);
            try
            {
                var ptr = IntPtr.Zero;
                if (buffer.Length > 0)
                {
                    handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    ptr = handle.AddrOfPinnedObject();
                }

                return RapierNative.WorldSnapshotWrite(world, ptr, (UIntPtr)buffer.Length);
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }

        public bool TryCreateSnapshot(out RapierSnapshot snapshot)
        {
            var buffer = new byte[SnapshotSize()];
            if (TryWriteSnapshot(buffer))
            {
                snapshot = new RapierSnapshot(buffer);
                return true;
            }

            snapshot = default;
            return false;
        }

        public bool TryReadSnapshot(RapierSnapshot snapshot)
        {
            ThrowIfDisposed();
            var bytes = snapshot.Bytes ?? Array.Empty<byte>();

            var handle = default(GCHandle);
            try
            {
                var ptr = IntPtr.Zero;
                if (bytes.Length > 0)
                {
                    handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                    ptr = handle.AddrOfPinnedObject();
                }

                return RapierNative.WorldSnapshotRead(world, ptr, (UIntPtr)bytes.Length);
            }
            finally
            {
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }

        public void Dispose()
        {
            if (world == 0)
            {
                return;
            }

            RapierNative.WorldDestroy(world);
            world = 0;
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            if (world == 0)
            {
                throw new ObjectDisposedException(nameof(RapierWorld));
            }
        }

        ~RapierWorld()
        {
            Dispose();
        }
    }
}
