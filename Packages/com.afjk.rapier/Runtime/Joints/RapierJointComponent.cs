using System.Collections.Generic;
using UnityEngine;

namespace AFJK.Rapier
{
    public abstract class RapierJointComponent : MonoBehaviour, IRapierRegistrationOrdered
    {
        [SerializeField] private RapierRigidBodyComponent body1;
        [SerializeField] private RapierRigidBodyComponent body2;
        [SerializeField] private bool registerOnEnable = true;
        [SerializeField] private Vector3 localAnchor1;
        [SerializeField] private Vector3 localAnchor2;

        [Header("Stable Id (optional, for external references)")]
        [SerializeField] private string stableId = string.Empty;

        [Tooltip("Used by RapierWorldComponent.RebuildWorld when its registration mode is ExplicitOrder.")]
        [SerializeField] private int registrationOrder;

        // Limit/motor settings are cached so they can be reapplied when the joint is recreated
        // (for example by RapierWorldComponent.RebuildWorld), keeping the joint configuration stable.
        private readonly List<AxisLimit> limitCache = new List<AxisLimit>();
        private readonly List<AxisMotorPosition> motorPositionCache = new List<AxisMotorPosition>();
        private readonly List<AxisMotorVelocity> motorVelocityCache = new List<AxisMotorVelocity>();
        private readonly List<AxisMotorMaxForce> motorMaxForceCache = new List<AxisMotorMaxForce>();

        public RapierJointHandle JointHandle { get; protected set; } = RapierJointHandle.Invalid;

        public bool IsRegistered => JointHandle.IsValid;

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

        public string StableId
        {
            get => stableId;
            set => stableId = value ?? string.Empty;
        }

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
            CacheLimit(axis, min, max);
            return TryGetActiveWorld(out var world) && world.SetJointLimits(JointHandle, axis, min, max);
        }

        public bool SetMotorPosition(
            RapierJointAxis axis,
            float targetPosition,
            float stiffness,
            float damping)
        {
            CacheMotorPosition(axis, targetPosition, stiffness, damping);
            return TryGetActiveWorld(out var world) &&
                world.SetJointMotorPosition(JointHandle, axis, targetPosition, stiffness, damping);
        }

        public bool SetMotorVelocity(RapierJointAxis axis, float targetVelocity, float factor)
        {
            CacheMotorVelocity(axis, targetVelocity, factor);
            return TryGetActiveWorld(out var world) &&
                world.SetJointMotorVelocity(JointHandle, axis, targetVelocity, factor);
        }

        public bool SetMotorMaxForce(RapierJointAxis axis, float maxForce)
        {
            CacheMotorMaxForce(axis, maxForce);
            return TryGetActiveWorld(out var world) &&
                world.SetJointMotorMaxForce(JointHandle, axis, maxForce);
        }

        // Resolves and tracks both bodies, then creates the native joint. Used by
        // RapierWorldComponent.RebuildWorld so joint creation order is controlled globally.
        internal bool CreateManaged()
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
                !body1.World.IsCreated ||
                !ReferenceEquals(body1.World, body2.World))
            {
                return false;
            }

            body1.TrackJoint(this);
            body2.TrackJoint(this);
            return CreateInWorld();
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

            ReapplyCachedConfig(body1.World);
            return true;
        }

        // Re-pushes cached limits/motors after the native joint is (re)created so the joint keeps
        // its configuration across a managed rebuild.
        private void ReapplyCachedConfig(RapierWorld world)
        {
            for (var i = 0; i < limitCache.Count; i++)
            {
                var l = limitCache[i];
                world.SetJointLimits(JointHandle, l.Axis, l.Min, l.Max);
            }

            for (var i = 0; i < motorPositionCache.Count; i++)
            {
                var m = motorPositionCache[i];
                world.SetJointMotorPosition(JointHandle, m.Axis, m.Target, m.Stiffness, m.Damping);
            }

            for (var i = 0; i < motorVelocityCache.Count; i++)
            {
                var m = motorVelocityCache[i];
                world.SetJointMotorVelocity(JointHandle, m.Axis, m.Target, m.Factor);
            }

            for (var i = 0; i < motorMaxForceCache.Count; i++)
            {
                var m = motorMaxForceCache[i];
                world.SetJointMotorMaxForce(JointHandle, m.Axis, m.MaxForce);
            }
        }

        private void CacheLimit(RapierJointAxis axis, float min, float max)
        {
            for (var i = 0; i < limitCache.Count; i++)
            {
                if (limitCache[i].Axis == axis)
                {
                    limitCache[i] = new AxisLimit { Axis = axis, Min = min, Max = max };
                    return;
                }
            }

            limitCache.Add(new AxisLimit { Axis = axis, Min = min, Max = max });
        }

        private void CacheMotorPosition(RapierJointAxis axis, float target, float stiffness, float damping)
        {
            for (var i = 0; i < motorPositionCache.Count; i++)
            {
                if (motorPositionCache[i].Axis == axis)
                {
                    motorPositionCache[i] = new AxisMotorPosition { Axis = axis, Target = target, Stiffness = stiffness, Damping = damping };
                    return;
                }
            }

            motorPositionCache.Add(new AxisMotorPosition { Axis = axis, Target = target, Stiffness = stiffness, Damping = damping });
        }

        private void CacheMotorVelocity(RapierJointAxis axis, float target, float factor)
        {
            for (var i = 0; i < motorVelocityCache.Count; i++)
            {
                if (motorVelocityCache[i].Axis == axis)
                {
                    motorVelocityCache[i] = new AxisMotorVelocity { Axis = axis, Target = target, Factor = factor };
                    return;
                }
            }

            motorVelocityCache.Add(new AxisMotorVelocity { Axis = axis, Target = target, Factor = factor });
        }

        private void CacheMotorMaxForce(RapierJointAxis axis, float maxForce)
        {
            for (var i = 0; i < motorMaxForceCache.Count; i++)
            {
                if (motorMaxForceCache[i].Axis == axis)
                {
                    motorMaxForceCache[i] = new AxisMotorMaxForce { Axis = axis, MaxForce = maxForce };
                    return;
                }
            }

            motorMaxForceCache.Add(new AxisMotorMaxForce { Axis = axis, MaxForce = maxForce });
        }

        private struct AxisLimit
        {
            public RapierJointAxis Axis;
            public float Min;
            public float Max;
        }

        private struct AxisMotorPosition
        {
            public RapierJointAxis Axis;
            public float Target;
            public float Stiffness;
            public float Damping;
        }

        private struct AxisMotorVelocity
        {
            public RapierJointAxis Axis;
            public float Target;
            public float Factor;
        }

        private struct AxisMotorMaxForce
        {
            public RapierJointAxis Axis;
            public float MaxForce;
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
