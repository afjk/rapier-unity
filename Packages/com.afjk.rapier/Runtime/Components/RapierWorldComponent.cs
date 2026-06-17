using System.Collections.Generic;
using UnityEngine;

namespace AFJK.Rapier
{
    public enum RapierWorldStepMode
    {
        Manual = 0,
        FixedUpdate = 1
    }

    [DisallowMultipleComponent]
    public sealed class RapierWorldComponent : MonoBehaviour
    {
        [SerializeField] private RapierWorldStepMode stepMode = RapierWorldStepMode.FixedUpdate;
        [SerializeField] private Vector3 gravity = new Vector3(0f, -9.81f, 0f);
        [SerializeField] private float timestep = 1f / 60f;
        [SerializeField] private bool logStateHash;

        private readonly List<RapierRigidBodyComponent> bodies = new List<RapierRigidBodyComponent>();
        private RapierWorld world;

        public RapierWorldStepMode StepMode
        {
            get => stepMode;
            set => stepMode = value;
        }

        public Vector3 Gravity
        {
            get => gravity;
            set
            {
                gravity = value;
                if (world != null && world.IsCreated)
                {
                    world.SetGravity(gravity);
                }
            }
        }

        public float Timestep
        {
            get => timestep;
            set
            {
                timestep = Mathf.Max(value, 0.000001f);
                if (world != null && world.IsCreated)
                {
                    world.SetTimestep(timestep);
                }
            }
        }

        public bool LogStateHash
        {
            get => logStateHash;
            set => logStateHash = value;
        }

        public RapierWorld World => world;

        public RapierWorld EnsureWorld()
        {
            if (world != null && world.IsCreated)
            {
                return world;
            }

            world = RapierWorld.Create();
            ApplySettings();
            return world;
        }

        public bool Step()
        {
            var activeWorld = EnsureWorld();

            for (var i = 0; i < bodies.Count; i++)
            {
                bodies[i].SyncTransformToRapierBeforeStepIfNeeded();
            }

            if (!activeWorld.Step())
            {
                return false;
            }

            for (var i = 0; i < bodies.Count; i++)
            {
                bodies[i].SyncTransformFromRapierIfNeeded();
            }

            if (logStateHash)
            {
                Debug.Log($"Rapier state hash: {activeWorld.StateHash()}");
            }

            return true;
        }

        public ulong StateHash()
        {
            return EnsureWorld().StateHash();
        }

        public int SnapshotSize()
        {
            return EnsureWorld().SnapshotSize();
        }

        public bool TryCreateSnapshot(out RapierSnapshot snapshot)
        {
            return EnsureWorld().TryCreateSnapshot(out snapshot);
        }

        public bool TryReadSnapshot(RapierSnapshot snapshot)
        {
            return EnsureWorld().TryReadSnapshot(snapshot);
        }

        public int DrainCollisionEvents(RapierCollisionEvent[] results)
        {
            return EnsureWorld().DrainCollisionEvents(results);
        }

        public int DrainContactForceEvents(RapierContactForceEvent[] results)
        {
            return EnsureWorld().DrainContactForceEvents(results);
        }

        internal void RegisterBody(RapierRigidBodyComponent body)
        {
            if (body == null || bodies.Contains(body))
            {
                return;
            }

            bodies.Add(body);
        }

        internal void UnregisterBody(RapierRigidBodyComponent body)
        {
            bodies.Remove(body);
        }

        private void OnEnable()
        {
            EnsureWorld();
            for (var i = bodies.Count - 1; i >= 0; i--)
            {
                var body = bodies[i];
                if (body == null)
                {
                    bodies.RemoveAt(i);
                    continue;
                }

                if (body.isActiveAndEnabled)
                {
                    body.Register();
                }
            }
        }

        private void OnDisable()
        {
            for (var i = bodies.Count - 1; i >= 0; i--)
            {
                if (bodies[i] != null)
                {
                    bodies[i].ForgetNativeRegistration(this);
                }
            }

            if (world != null)
            {
                world.Dispose();
                world = null;
            }
        }

        private void FixedUpdate()
        {
            if (stepMode == RapierWorldStepMode.FixedUpdate)
            {
                Step();
            }
        }

        private void OnValidate()
        {
            if (timestep <= 0f)
            {
                timestep = 1f / 60f;
            }
        }

        private void ApplySettings()
        {
            world.SetGravity(gravity);
            world.SetTimestep(timestep);
        }
    }
}
