using System.Collections.Generic;
using UnityEngine;

namespace AFJK.Rapier
{
    [DisallowMultipleComponent]
    public sealed class RapierRigidBodyComponent : MonoBehaviour, IRapierRegistrationOrdered
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

        [Header("Stable Id (optional, for external references)")]
        [SerializeField] private string stableId = string.Empty;

        [Tooltip("If set and StableId is empty, a deterministic StableId is generated from the hierarchy path on registration.")]
        [SerializeField] private bool autoGenerateStableId;

        [Tooltip("Used by RapierWorldComponent.RebuildWorld when its registration mode is ExplicitOrder.")]
        [SerializeField] private int registrationOrder;

        [Header("Authored body settings (applied on register)")]
        [SerializeField] private float gravityScale = 1f;
        [SerializeField] private float softCcdPrediction;
        [SerializeField] private uint additionalSolverIterations;
        [SerializeField] private int dominanceGroup;
        [SerializeField] private bool lockTranslationX;
        [SerializeField] private bool lockTranslationY;
        [SerializeField] private bool lockTranslationZ;
        [SerializeField] private bool lockRotationX;
        [SerializeField] private bool lockRotationY;
        [SerializeField] private bool lockRotationZ;

        private readonly List<RapierColliderComponent> colliders = new List<RapierColliderComponent>();
        private readonly List<RapierJointComponent> joints = new List<RapierJointComponent>();

        public RapierRigidBodyHandle BodyHandle { get; private set; } = RapierRigidBodyHandle.Invalid;

        public bool IsRegistered => BodyHandle.IsValid;

        public RapierWorldComponent WorldComponent => worldComponent;

        public RapierWorld World => worldComponent != null ? worldComponent.World : null;

        public RapierRigidBodyType BodyType
        {
            get => bodyType;
            set => bodyType = value;
        }

        public bool RegisterOnEnable
        {
            get => registerOnEnable;
            set => registerOnEnable = value;
        }

        public int RegistrationOrder
        {
            get => registrationOrder;
            set => registrationOrder = value;
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

        public bool CanSleep
        {
            get => canSleep;
            set => canSleep = value;
        }

        public bool CcdEnabled
        {
            get => ccdEnabled;
            set => ccdEnabled = value;
        }

        public Vector3 InitialLinearVelocity
        {
            get => initialLinearVelocity;
            set => initialLinearVelocity = value;
        }

        public Vector3 InitialAngularVelocity
        {
            get => initialAngularVelocity;
            set => initialAngularVelocity = value;
        }

        public float LinearDamping
        {
            get => linearDamping;
            set => linearDamping = Mathf.Max(0f, value);
        }

        public float AngularDamping
        {
            get => angularDamping;
            set => angularDamping = Mathf.Max(0f, value);
        }

        public float GravityScale
        {
            get => gravityScale;
            set => gravityScale = value;
        }

        public float SoftCcdPrediction
        {
            get => softCcdPrediction;
            set => softCcdPrediction = Mathf.Max(0f, value);
        }

        public uint AdditionalSolverIterations
        {
            get => additionalSolverIterations;
            set => additionalSolverIterations = value;
        }

        public int DominanceGroup
        {
            get => dominanceGroup;
            set => dominanceGroup = value;
        }

        /// <summary>Locks the X/Y/Z world translation axes (true = locked) before registration.</summary>
        public void SetLockedTranslations(bool x, bool y, bool z)
        {
            lockTranslationX = x;
            lockTranslationY = y;
            lockTranslationZ = z;
        }

        /// <summary>Locks the X/Y/Z world rotation axes (true = locked) before registration.</summary>
        public void SetLockedRotations(bool x, bool y, bool z)
        {
            lockRotationX = x;
            lockRotationY = y;
            lockRotationZ = z;
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
            ApplyAuthoredSettings(world);

            if (syncTransformToRapierOnRegister)
            {
                PushTransformToRapier();
            }

            for (var i = 0; i < colliders.Count; i++)
            {
                colliders[i].CreateInWorld(this);
            }

            for (var i = 0; i < joints.Count; i++)
            {
                joints[i].CreateInWorld();
            }

            return true;
        }

        // Creates only the native body (no collider/joint pass) against an explicitly chosen world.
        // RapierWorldComponent.RebuildWorld drives collider/joint creation separately so their global
        // order is deterministic rather than dependent on per-body registration timing.
        internal bool CreateManaged(RapierWorldComponent owner)
        {
            if (IsRegistered)
            {
                return true;
            }

            if (owner == null)
            {
                return false;
            }

            worldComponent = owner;
            var world = owner.EnsureWorld();
            BodyHandle = world.CreateRigidBody(CreateDesc());

            if (!BodyHandle.IsValid)
            {
                Debug.LogWarning("Failed to create Rapier rigid body.", this);
                return false;
            }

            owner.RegisterBody(this);
            ApplyAuthoredSettings(world);

            if (syncTransformToRapierOnRegister)
            {
                PushTransformToRapier();
            }

            return true;
        }

        // Adds a collider/joint to this body's tracking lists without creating it, so a managed
        // rebuild can create them in its own deterministic order while teardown still finds them.
        internal void TrackCollider(RapierColliderComponent collider)
        {
            if (collider != null && !colliders.Contains(collider))
            {
                colliders.Add(collider);
            }
        }

        internal void TrackJoint(RapierJointComponent joint)
        {
            if (joint != null && !joints.Contains(joint))
            {
                joints.Add(joint);
            }
        }

        public void Unregister()
        {
            for (var i = joints.Count - 1; i >= 0; i--)
            {
                joints[i].DestroyInWorld();
            }

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

        public bool SetSleeping(bool sleeping)
        {
            return TryGetActiveWorld(out var w) && w.SetBodySleeping(BodyHandle, sleeping);
        }

        public bool AddForceAtPoint(Vector3 force, Vector3 point, bool wakeUp = true)
        {
            return TryGetActiveWorld(out var w) && w.AddForceAtPoint(BodyHandle, force, point, wakeUp);
        }

        public bool ApplyImpulseAtPoint(Vector3 impulse, Vector3 point, bool wakeUp = true)
        {
            return TryGetActiveWorld(out var w) && w.ApplyImpulseAtPoint(BodyHandle, impulse, point, wakeUp);
        }

        public bool SetAdditionalSolverIterations(uint iterations)
        {
            return TryGetActiveWorld(out var w) && w.SetAdditionalSolverIterations(BodyHandle, iterations);
        }

        public bool SetDominanceGroup(int dominance)
        {
            return TryGetActiveWorld(out var w) && w.SetDominanceGroup(BodyHandle, dominance);
        }

        public bool SetSoftCcdPrediction(float prediction)
        {
            return TryGetActiveWorld(out var w) && w.SetSoftCcdPrediction(BodyHandle, prediction);
        }

        public bool TryGetMass(out float mass)
        {
            if (TryGetActiveWorld(out var w))
            {
                return w.TryGetMass(BodyHandle, out mass);
            }

            mass = 0f;
            return false;
        }

        public bool TryGetTransform(out RapierTransform transform)
        {
            if (TryGetActiveWorld(out var w))
            {
                return w.TryGetTransform(BodyHandle, out transform);
            }

            transform = default;
            return false;
        }

        public string StableId
        {
            get => stableId;
            set => stableId = value ?? string.Empty;
        }

        public bool AutoGenerateStableId
        {
            get => autoGenerateStableId;
            set => autoGenerateStableId = value;
        }

        public void EnsureStableId()
        {
            if (autoGenerateStableId && string.IsNullOrEmpty(stableId))
            {
                stableId = RapierStableId.FromHierarchy(transform, "Body");
            }
        }

        // Pushes serialized authoring state to the freshly created body. RapierBodyDesc already
        // carries type/velocity/damping/CCD/sleep, so this covers the remaining body settings.
        private void ApplyAuthoredSettings(RapierWorld activeWorld)
        {
            if (activeWorld == null || !activeWorld.IsCreated || !BodyHandle.IsValid)
            {
                return;
            }

            EnsureStableId();

            if (!string.IsNullOrEmpty(stableId))
            {
                activeWorld.SetRigidBodyStableId(BodyHandle, RapierWorld.StableIdHash(stableId));
            }

            if (!Mathf.Approximately(gravityScale, 1f))
            {
                activeWorld.SetGravityScale(BodyHandle, gravityScale, false);
            }

            if (softCcdPrediction > 0f)
            {
                activeWorld.SetSoftCcdPrediction(BodyHandle, softCcdPrediction);
            }

            if (additionalSolverIterations > 0)
            {
                activeWorld.SetAdditionalSolverIterations(BodyHandle, additionalSolverIterations);
            }

            if (dominanceGroup != 0)
            {
                activeWorld.SetDominanceGroup(BodyHandle, dominanceGroup);
            }

            if (lockTranslationX || lockTranslationY || lockTranslationZ)
            {
                activeWorld.SetEnabledTranslations(BodyHandle, !lockTranslationX, !lockTranslationY, !lockTranslationZ, false);
            }

            if (lockRotationX || lockRotationY || lockRotationZ)
            {
                activeWorld.SetEnabledRotations(BodyHandle, !lockRotationX, !lockRotationY, !lockRotationZ, false);
            }
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

        internal void RegisterJoint(RapierJointComponent joint)
        {
            if (joint == null || joints.Contains(joint))
            {
                return;
            }

            joints.Add(joint);

            if (IsRegistered)
            {
                joint.CreateInWorld();
            }
        }

        internal void UnregisterJoint(RapierJointComponent joint)
        {
            joints.Remove(joint);
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

            for (var i = 0; i < joints.Count; i++)
            {
                joints[i].ForgetNativeRegistration();
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
