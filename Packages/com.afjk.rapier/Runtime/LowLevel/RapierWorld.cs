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
