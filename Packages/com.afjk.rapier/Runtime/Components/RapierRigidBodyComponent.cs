using System.Collections.Generic;
using UnityEngine;

namespace AFJK.Rapier
{
    [DisallowMultipleComponent]
    public sealed class RapierRigidBodyComponent : MonoBehaviour
    {
        [SerializeField] private RapierWorldComponent worldComponent;
        [SerializeField] private RapierRigidBodyType bodyType = RapierRigidBodyType.Dynamic;
        [SerializeField] private bool registerOnEnable = true;
        [SerializeField] private bool syncTransformFromRapier = true;
        [SerializeField] private bool syncTransformToRapierOnRegister = true;
        [SerializeField] private bool syncTransformToRapierBeforeStep;
        [SerializeField] private bool canSleep = true;
        [SerializeField] private bool ccdEnabled;
        [SerializeField] private Vector3 initialLinearVelocity;
        [SerializeField] private Vector3 initialAngularVelocity;
        [SerializeField] private float linearDamping;
        [SerializeField] private float angularDamping;

        private readonly List<RapierColliderComponent> colliders = new List<RapierColliderComponent>();

        public RapierRigidBodyHandle BodyHandle { get; private set; } = RapierRigidBodyHandle.Invalid;

        public bool IsRegistered => BodyHandle.IsValid;

        public RapierWorldComponent WorldComponent => worldComponent;

        public RapierWorld World => worldComponent != null ? worldComponent.World : null;

        public RapierRigidBodyType BodyType
        {
            get => bodyType;
            set => bodyType = value;
        }

        public bool SyncTransformFromRapier
        {
            get => syncTransformFromRapier;
            set => syncTransformFromRapier = value;
        }

        public bool SyncTransformToRapierBeforeStep
        {
            get => syncTransformToRapierBeforeStep;
            set => syncTransformToRapierBeforeStep = value;
        }

        public bool Register()
        {
            if (IsRegistered)
            {
                return true;
            }

            if (worldComponent == null)
            {
                worldComponent = GetComponentInParent<RapierWorldComponent>();
            }

            if (worldComponent == null)
            {
                Debug.LogWarning($"{nameof(RapierRigidBodyComponent)} requires a {nameof(RapierWorldComponent)}.", this);
                return false;
            }

            var world = worldComponent.EnsureWorld();
            BodyHandle = world.CreateRigidBody(CreateDesc());

            if (!BodyHandle.IsValid)
            {
                Debug.LogWarning("Failed to create Rapier rigid body.", this);
                return false;
            }

            worldComponent.RegisterBody(this);

            if (syncTransformToRapierOnRegister)
            {
                PushTransformToRapier();
            }

            for (var i = 0; i < colliders.Count; i++)
            {
                colliders[i].CreateInWorld(this);
            }

            return true;
        }

        public void Unregister()
        {
            for (var i = colliders.Count - 1; i >= 0; i--)
            {
                colliders[i].DestroyInWorld();
            }

            if (BodyHandle.IsValid && worldComponent != null && worldComponent.World != null && worldComponent.World.IsCreated)
            {
                worldComponent.World.DestroyRigidBody(BodyHandle);
            }

            BodyHandle = RapierRigidBodyHandle.Invalid;

            if (worldComponent != null)
            {
                worldComponent.UnregisterBody(this);
            }
        }

        public bool PushTransformToRapier()
        {
            if (!IsRegistered || World == null || !World.IsCreated)
            {
                return false;
            }

            return World.SetTransform(
                BodyHandle,
                new RapierTransform(transform.position, transform.rotation));
        }

        public bool PullTransformFromRapier()
        {
            if (!IsRegistered || World == null || !World.IsCreated)
            {
                return false;
            }

            if (!World.TryGetTransform(BodyHandle, out var rapierTransform))
            {
                return false;
            }

            transform.SetPositionAndRotation(rapierTransform.Position, rapierTransform.Rotation);
            return true;
        }

        private bool TryGetActiveWorld(out RapierWorld activeWorld)
        {
            activeWorld = World;
            return IsRegistered && activeWorld != null && activeWorld.IsCreated;
        }

        public bool TryGetLinearVelocity(out Vector3 velocity)
        {
            if (TryGetActiveWorld(out var w))
            {
                return w.TryGetLinearVelocity(BodyHandle, out velocity);
            }

            velocity = default;
            return false;
        }

        public bool SetLinearVelocity(Vector3 velocity, bool wakeUp = true)
        {
            return TryGetActiveWorld(out var w) && w.SetLinearVelocity(BodyHandle, velocity, wakeUp);
        }

        public bool TryGetAngularVelocity(out Vector3 velocity)
        {
            if (TryGetActiveWorld(out var w))
            {
                return w.TryGetAngularVelocity(BodyHandle, out velocity);
            }

            velocity = default;
            return false;
        }

        public bool SetAngularVelocity(Vector3 velocity, bool wakeUp = true)
        {
            return TryGetActiveWorld(out var w) && w.SetAngularVelocity(BodyHandle, velocity, wakeUp);
        }

        public bool SetLinearDamping(float damping)
        {
            return TryGetActiveWorld(out var w) && w.SetLinearDamping(BodyHandle, damping);
        }

        public bool SetAngularDamping(float damping)
        {
            return TryGetActiveWorld(out var w) && w.SetAngularDamping(BodyHandle, damping);
        }

        public bool SetGravityScale(float scale, bool wakeUp = true)
        {
            return TryGetActiveWorld(out var w) && w.SetGravityScale(BodyHandle, scale, wakeUp);
        }

        public bool SetCcdEnabled(bool enabled)
        {
            return TryGetActiveWorld(out var w) && w.SetCcdEnabled(BodyHandle, enabled);
        }

        public bool SetBodyEnabled(bool enabled)
        {
            return TryGetActiveWorld(out var w) && w.SetBodyEnabled(BodyHandle, enabled);
        }

        public bool AddForce(Vector3 force, bool wakeUp = true)
        {
            return TryGetActiveWorld(out var w) && w.AddForce(BodyHandle, force, wakeUp);
        }

        public bool AddTorque(Vector3 torque, bool wakeUp = true)
        {
            return TryGetActiveWorld(out var w) && w.AddTorque(BodyHandle, torque, wakeUp);
        }

        public bool ApplyImpulse(Vector3 impulse, bool wakeUp = true)
        {
            return TryGetActiveWorld(out var w) && w.ApplyImpulse(BodyHandle, impulse, wakeUp);
        }

        public bool ApplyTorqueImpulse(Vector3 impulse, bool wakeUp = true)
        {
            return TryGetActiveWorld(out var w) && w.ApplyTorqueImpulse(BodyHandle, impulse, wakeUp);
        }

        public bool SetNextKinematicTranslation(Vector3 translation)
        {
            return TryGetActiveWorld(out var w) && w.SetNextKinematicTranslation(BodyHandle, translation);
        }

        public bool SetNextKinematicRotation(Quaternion rotation)
        {
            return TryGetActiveWorld(out var w) && w.SetNextKinematicRotation(BodyHandle, rotation);
        }

        public bool SetEnabledRotations(bool allowX, bool allowY, bool allowZ, bool wakeUp = true)
        {
            return TryGetActiveWorld(out var w) && w.SetEnabledRotations(BodyHandle, allowX, allowY, allowZ, wakeUp);
        }

        public bool SetEnabledTranslations(bool allowX, bool allowY, bool allowZ, bool wakeUp = true)
        {
            return TryGetActiveWorld(out var w) && w.SetEnabledTranslations(BodyHandle, allowX, allowY, allowZ, wakeUp);
        }

        internal void RegisterCollider(RapierColliderComponent collider)
        {
            if (collider == null || colliders.Contains(collider))
            {
                return;
            }

            colliders.Add(collider);

            if (IsRegistered)
            {
                collider.CreateInWorld(this);
            }
        }

        internal void UnregisterCollider(RapierColliderComponent collider)
        {
            colliders.Remove(collider);
        }

        internal void SyncTransformToRapierBeforeStepIfNeeded()
        {
            if (syncTransformToRapierBeforeStep)
            {
                PushTransformToRapier();
            }
        }

        internal void SyncTransformFromRapierIfNeeded()
        {
            if (syncTransformFromRapier)
            {
                PullTransformFromRapier();
            }
        }

        internal void ForgetNativeRegistration(RapierWorldComponent owner)
        {
            if (owner != worldComponent)
            {
                return;
            }

            BodyHandle = RapierRigidBodyHandle.Invalid;
            for (var i = 0; i < colliders.Count; i++)
            {
                colliders[i].ForgetNativeRegistration();
            }
        }

        private void OnEnable()
        {
            if (registerOnEnable)
            {
                Register();
            }
        }

        private void OnDisable()
        {
            Unregister();
        }

        private RapierBodyDesc CreateDesc()
        {
            return new RapierBodyDesc
            {
                BodyType = bodyType,
                Position = transform.position,
                Rotation = transform.rotation,
                LinearVelocity = initialLinearVelocity,
                AngularVelocity = initialAngularVelocity,
                LinearDamping = linearDamping,
                AngularDamping = angularDamping,
                CanSleep = canSleep,
                CcdEnabled = ccdEnabled
            };
        }
    }
}
