using UnityEngine;

namespace AFJK.Rapier
{
    /// <summary>
    /// Wraps a Rapier PID controller for a single dynamic <see cref="RapierRigidBodyComponent"/>.
    /// The controller computes velocity corrections that push the body toward a target pose, which
    /// is useful for kinematic-feeling control of dynamic bodies (see the JS "PID controller" demo).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RapierPidControllerComponent : MonoBehaviour
    {
        [SerializeField] private RapierRigidBodyComponent rigidBody;
        [SerializeField] private float kp = 60f;
        [SerializeField] private float ki;
        [SerializeField] private float kd = 1f;
        [SerializeField] private RapierPidAxesMask axes = RapierPidAxesMask.AllAng;

        private RapierPidControllerHandle controller = RapierPidControllerHandle.Invalid;
        private RapierWorld createdInWorld;

        public RapierRigidBodyComponent RigidBody
        {
            get => rigidBody;
            set => rigidBody = value;
        }

        public float Kp
        {
            get => kp;
            set => kp = value;
        }

        public float Ki
        {
            get => ki;
            set => ki = value;
        }

        public float Kd
        {
            get => kd;
            set => kd = value;
        }

        public RapierPidAxesMask Axes
        {
            get => axes;
            set
            {
                axes = value;
                if (controller.IsValid && TryGetWorld(out var world))
                {
                    world.SetPidControllerAxes(controller, axes);
                }
            }
        }

        public RapierPidControllerHandle ControllerHandle => controller;

        public bool IsRegistered => controller.IsValid;

        public bool EnsureController()
        {
            if (!TryGetWorld(out var world))
            {
                return false;
            }

            // If the world was rebuilt (e.g. RapierWorldComponent.RebuildWorld), the cached
            // controller belongs to a disposed world; drop it and recreate against the new world.
            if (controller.IsValid && !ReferenceEquals(createdInWorld, world))
            {
                controller = RapierPidControllerHandle.Invalid;
                createdInWorld = null;
            }

            if (controller.IsValid)
            {
                return true;
            }

            controller = world.CreatePidController(kp, ki, kd, axes);
            if (!controller.IsValid)
            {
                Debug.LogWarning($"{nameof(RapierPidControllerComponent)} failed to create a PID controller.", this);
                return false;
            }

            createdInWorld = world;
            return true;
        }

        public bool ResetIntegrals()
        {
            return EnsureController() && createdInWorld.ResetPidControllerIntegrals(controller);
        }

        /// <summary>Applies a linear correction toward <paramref name="targetPosition"/>.</summary>
        public bool ApplyLinearCorrection(Vector3 targetPosition, Vector3 targetLinearVelocity)
        {
            if (!EnsureController() || rigidBody == null || !rigidBody.IsRegistered)
            {
                return false;
            }

            return createdInWorld.ApplyPidLinearCorrection(
                controller,
                rigidBody.BodyHandle,
                targetPosition,
                targetLinearVelocity);
        }

        /// <summary>Applies an angular correction toward <paramref name="targetRotation"/>.</summary>
        public bool ApplyAngularCorrection(Quaternion targetRotation, Vector3 targetAngularVelocity)
        {
            if (!EnsureController() || rigidBody == null || !rigidBody.IsRegistered)
            {
                return false;
            }

            return createdInWorld.ApplyPidAngularCorrection(
                controller,
                rigidBody.BodyHandle,
                targetRotation,
                targetAngularVelocity);
        }

        private bool TryGetWorld(out RapierWorld world)
        {
            if (rigidBody == null)
            {
                rigidBody = GetComponentInParent<RapierRigidBodyComponent>();
            }

            world = rigidBody != null ? rigidBody.World : null;
            if (world == null || !world.IsCreated)
            {
                world = null;
                return false;
            }

            return true;
        }

        private void OnDisable()
        {
            if (controller.IsValid && createdInWorld != null && createdInWorld.IsCreated)
            {
                createdInWorld.DestroyPidController(controller);
            }

            controller = RapierPidControllerHandle.Invalid;
            createdInWorld = null;
        }
    }
}
