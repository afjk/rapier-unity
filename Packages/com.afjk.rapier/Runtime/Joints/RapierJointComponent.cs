using UnityEngine;

namespace AFJK.Rapier
{
    public abstract class RapierJointComponent : MonoBehaviour
    {
        [SerializeField] private RapierRigidBodyComponent body1;
        [SerializeField] private RapierRigidBodyComponent body2;
        [SerializeField] private bool registerOnEnable = true;
        [SerializeField] private Vector3 localAnchor1;
        [SerializeField] private Vector3 localAnchor2;

        public RapierJointHandle JointHandle { get; protected set; } = RapierJointHandle.Invalid;

        public bool IsRegistered => JointHandle.IsValid;

        public RapierRigidBodyComponent Body1
        {
            get => body1;
            set => body1 = value;
        }

        public RapierRigidBodyComponent Body2
        {
            get => body2;
            set => body2 = value;
        }

        public Vector3 LocalAnchor1
        {
            get => localAnchor1;
            set => localAnchor1 = value;
        }

        public Vector3 LocalAnchor2
        {
            get => localAnchor2;
            set => localAnchor2 = value;
        }

        public bool Register()
        {
            if (IsRegistered)
            {
                return true;
            }

            if (!ResolveBodies(true))
            {
                return false;
            }

            TrackBodies();

            if (!body1.IsRegistered && !body1.Register())
            {
                return false;
            }

            if (!body2.IsRegistered && !body2.Register())
            {
                return false;
            }

            if (!ReferenceEquals(body1.World, body2.World))
            {
                Debug.LogWarning($"{GetType().Name} requires both bodies to belong to the same {nameof(RapierWorldComponent)}.", this);
                return false;
            }

            return CreateInWorld();
        }

        public void Unregister()
        {
            DestroyInWorld();

            if (body1 != null)
            {
                body1.UnregisterJoint(this);
            }

            if (body2 != null && !ReferenceEquals(body1, body2))
            {
                body2.UnregisterJoint(this);
            }
        }

        public bool RemoveJoint()
        {
            return DestroyInWorld();
        }

        public bool SetLimits(RapierJointAxis axis, float min, float max)
        {
            return TryGetActiveWorld(out var world) && world.SetJointLimits(JointHandle, axis, min, max);
        }

        public bool SetMotorPosition(
            RapierJointAxis axis,
            float targetPosition,
            float stiffness,
            float damping)
        {
            return TryGetActiveWorld(out var world) &&
                world.SetJointMotorPosition(JointHandle, axis, targetPosition, stiffness, damping);
        }

        public bool SetMotorVelocity(RapierJointAxis axis, float targetVelocity, float factor)
        {
            return TryGetActiveWorld(out var world) &&
                world.SetJointMotorVelocity(JointHandle, axis, targetVelocity, factor);
        }

        public bool SetMotorMaxForce(RapierJointAxis axis, float maxForce)
        {
            return TryGetActiveWorld(out var world) &&
                world.SetJointMotorMaxForce(JointHandle, axis, maxForce);
        }

        internal bool CreateInWorld()
        {
            if (IsRegistered)
            {
                return true;
            }

            if (!ResolveBodies(false) ||
                ReferenceEquals(body1, body2) ||
                !body1.IsRegistered ||
                !body2.IsRegistered ||
                body1.World == null ||
                body2.World == null ||
                !body1.World.IsCreated ||
                !ReferenceEquals(body1.World, body2.World))
            {
                return false;
            }

            JointHandle = CreateJoint(body1.World, body1.BodyHandle, body2.BodyHandle);
            if (!JointHandle.IsValid)
            {
                Debug.LogWarning($"Failed to create Rapier joint for {GetType().Name}.", this);
                return false;
            }

            return true;
        }

        internal bool DestroyInWorld()
        {
            if (!JointHandle.IsValid)
            {
                return false;
            }

            var removed = TryGetActiveWorld(out var world) && world.RemoveJoint(JointHandle);
            JointHandle = RapierJointHandle.Invalid;
            return removed;
        }

        internal void ForgetNativeRegistration()
        {
            JointHandle = RapierJointHandle.Invalid;
        }

        protected abstract RapierJointHandle CreateJoint(
            RapierWorld world,
            RapierRigidBodyHandle body1Handle,
            RapierRigidBodyHandle body2Handle);

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
        }

        private bool ResolveBodies(bool warn)
        {
            if (body1 == null)
            {
                body1 = GetComponentInParent<RapierRigidBodyComponent>();
            }

            if (body1 == null)
            {
                if (warn)
                {
                    Debug.LogWarning($"{GetType().Name} requires a primary {nameof(RapierRigidBodyComponent)}.", this);
                }

                return false;
            }

            if (body2 == null)
            {
                if (warn)
                {
                    Debug.LogWarning($"{GetType().Name} requires a connected {nameof(RapierRigidBodyComponent)}.", this);
                }

                return false;
            }

            if (ReferenceEquals(body1, body2))
            {
                if (warn)
                {
                    Debug.LogWarning($"{GetType().Name} requires two different rigid bodies.", this);
                }

                return false;
            }

            return true;
        }

        private void TrackBodies()
        {
            body1.RegisterJoint(this);
            body2.RegisterJoint(this);
        }

        private bool TryGetActiveWorld(out RapierWorld world)
        {
            world = body1 != null ? body1.World : null;
            return JointHandle.IsValid && world != null && world.IsCreated;
        }
    }
}
