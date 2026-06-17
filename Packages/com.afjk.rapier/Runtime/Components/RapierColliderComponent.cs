using UnityEngine;

namespace AFJK.Rapier
{
    public abstract class RapierColliderComponent : MonoBehaviour
    {
        [SerializeField] private RapierRigidBodyComponent rigidBody;
        [SerializeField] private bool registerOnEnable = true;
        [SerializeField] private bool isSensor;
        [SerializeField] private float density = 1f;
        [SerializeField] private float friction = 0.5f;
        [SerializeField] private float restitution;
        [SerializeField] private Vector3 localPosition;
        [SerializeField] private Quaternion localRotation = Quaternion.identity;

        public RapierColliderHandle ColliderHandle { get; protected set; } = RapierColliderHandle.Invalid;

        public bool IsRegistered => ColliderHandle.IsValid;

        public RapierRigidBodyComponent RigidBody => rigidBody;

        public bool IsSensor
        {
            get => isSensor;
            set => isSensor = value;
        }

        public float Density
        {
            get => density;
            set => density = Mathf.Max(0f, value);
        }

        public float Friction
        {
            get => friction;
            set => friction = Mathf.Max(0f, value);
        }

        public float Restitution
        {
            get => restitution;
            set => restitution = Mathf.Max(0f, value);
        }

        public Vector3 LocalPosition
        {
            get => localPosition;
            set => localPosition = value;
        }

        public Quaternion LocalRotation
        {
            get => localRotation == default(Quaternion) ? Quaternion.identity : localRotation;
            set => localRotation = value == default(Quaternion) ? Quaternion.identity : value;
        }

        public bool Register()
        {
            if (IsRegistered)
            {
                return true;
            }

            if (rigidBody == null)
            {
                rigidBody = GetComponentInParent<RapierRigidBodyComponent>();
            }

            if (rigidBody == null)
            {
                Debug.LogWarning($"{GetType().Name} requires a {nameof(RapierRigidBodyComponent)}.", this);
                return false;
            }

            rigidBody.RegisterCollider(this);

            if (!rigidBody.IsRegistered && !rigidBody.Register())
            {
                return false;
            }

            return CreateInWorld(rigidBody);
        }

        public void Unregister()
        {
            DestroyInWorld();

            if (rigidBody != null)
            {
                rigidBody.UnregisterCollider(this);
            }
        }

        private bool TryGetActiveWorld(out RapierWorld activeWorld)
        {
            activeWorld = rigidBody != null ? rigidBody.World : null;
            return IsRegistered && activeWorld != null && activeWorld.IsCreated;
        }

        public bool SetFriction(float value)
        {
            return TryGetActiveWorld(out var w) && w.SetColliderFriction(ColliderHandle, value);
        }

        public bool SetRestitution(float value)
        {
            return TryGetActiveWorld(out var w) && w.SetColliderRestitution(ColliderHandle, value);
        }

        public bool SetDensity(float value)
        {
            return TryGetActiveWorld(out var w) && w.SetColliderDensity(ColliderHandle, value);
        }

        public bool SetSensor(bool value)
        {
            return TryGetActiveWorld(out var w) && w.SetColliderSensor(ColliderHandle, value);
        }

        public bool SetColliderEnabled(bool value)
        {
            return TryGetActiveWorld(out var w) && w.SetColliderEnabled(ColliderHandle, value);
        }

        public bool SetFrictionCombineRule(RapierCoefficientCombineRule rule)
        {
            return TryGetActiveWorld(out var w) && w.SetColliderFrictionCombineRule(ColliderHandle, rule);
        }

        public bool SetRestitutionCombineRule(RapierCoefficientCombineRule rule)
        {
            return TryGetActiveWorld(out var w) && w.SetColliderRestitutionCombineRule(ColliderHandle, rule);
        }

        public bool SetCollisionGroups(uint groups)
        {
            return TryGetActiveWorld(out var w) && w.SetColliderCollisionGroups(ColliderHandle, groups);
        }

        public bool SetSolverGroups(uint groups)
        {
            return TryGetActiveWorld(out var w) && w.SetColliderSolverGroups(ColliderHandle, groups);
        }

        public bool SetActiveEvents(RapierActiveEvents events)
        {
            return TryGetActiveWorld(out var w) && w.SetColliderActiveEvents(ColliderHandle, events);
        }

        public bool SetActiveCollisionTypes(RapierActiveCollisionTypes types)
        {
            return TryGetActiveWorld(out var w) && w.SetColliderActiveCollisionTypes(ColliderHandle, types);
        }

        public bool SetContactForceEventThreshold(float threshold)
        {
            return TryGetActiveWorld(out var w) && w.SetColliderContactForceEventThreshold(ColliderHandle, threshold);
        }

        internal bool CreateInWorld(RapierRigidBodyComponent body)
        {
            if (IsRegistered)
            {
                return true;
            }

            if (body == null || !body.IsRegistered || body.World == null || !body.World.IsCreated)
            {
                return false;
            }

            ColliderHandle = CreateCollider(body.World, body.BodyHandle);
            if (!ColliderHandle.IsValid)
            {
                Debug.LogWarning($"Failed to create Rapier collider for {GetType().Name}.", this);
                return false;
            }

            return true;
        }

        internal void DestroyInWorld()
        {
            if (!ColliderHandle.IsValid)
            {
                return;
            }

            if (rigidBody != null && rigidBody.World != null && rigidBody.World.IsCreated)
            {
                rigidBody.World.DestroyCollider(ColliderHandle);
            }

            ColliderHandle = RapierColliderHandle.Invalid;
        }

        internal void ForgetNativeRegistration()
        {
            ColliderHandle = RapierColliderHandle.Invalid;
        }

        protected abstract RapierColliderHandle CreateCollider(
            RapierWorld world,
            RapierRigidBodyHandle body);

        protected virtual void OnEnable()
        {
            if (registerOnEnable)
            {
                Register();
            }
        }

        protected virtual void OnDisable()
        {
            Unregister();
        }

        protected virtual void OnValidate()
        {
            if (density < 0f)
            {
                density = 0f;
            }

            if (friction < 0f)
            {
                friction = 0f;
            }

            if (restitution < 0f)
            {
                restitution = 0f;
            }

            if (localRotation == default(Quaternion))
            {
                localRotation = Quaternion.identity;
            }
        }
    }
}
