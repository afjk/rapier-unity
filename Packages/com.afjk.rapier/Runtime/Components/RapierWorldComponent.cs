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
        [Tooltip("Order used by RebuildWorld when (re)creating bodies, colliders, and joints.")]
        [SerializeField] private RapierRegistrationMode registrationMode = RapierRegistrationMode.HierarchyOrder;

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

        public RapierRegistrationMode RegistrationMode
        {
            get => registrationMode;
            set => registrationMode = value;
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

        /// <summary>
        /// Discards any existing native world and rebuilds it deterministically: collects every
        /// Rapier body/collider/joint in this world's hierarchy, orders them by
        /// <see cref="RegistrationMode"/>, and creates them in that order (bodies, then colliders,
        /// then joints). This is the explicit, environment-independent path used by Scene Sync
        /// import and network bridges; the per-component <c>registerOnEnable</c> path is unaffected.
        /// </summary>
        public RapierWorld RebuildWorld()
        {
            TeardownWorld();
            var activeWorld = EnsureWorld();

            var bodyComponents = CollectOrdered<RapierRigidBodyComponent>();
            for (var i = 0; i < bodyComponents.Count; i++)
            {
                bodyComponents[i].CreateManaged(this);
            }

            var colliderComponents = CollectOrdered<RapierColliderComponent>();
            for (var i = 0; i < colliderComponents.Count; i++)
            {
                colliderComponents[i].CreateManaged();
            }

            var jointComponents = CollectOrdered<RapierJointComponent>();
            for (var i = 0; i < jointComponents.Count; i++)
            {
                jointComponents[i].CreateManaged();
            }

            return activeWorld;
        }

        // Forgets all native registrations and disposes the world so RebuildWorld starts clean.
        // Component tracking lists (body -> colliders/joints) persist and are reused on rebuild.
        private void TeardownWorld()
        {
            for (var i = bodies.Count - 1; i >= 0; i--)
            {
                if (bodies[i] != null)
                {
                    bodies[i].ForgetNativeRegistration(this);
                }
            }

            bodies.Clear();

            if (world != null)
            {
                world.Dispose();
                world = null;
            }
        }

        // Collects this world's components (active+enabled, in hierarchy order) and sorts them by
        // mode. The isActiveAndEnabled filter matches the registerOnEnable path, which only
        // registers components whose OnEnable has run, so both paths register the same set.
        private List<T> CollectOrdered<T>() where T : Component, IRapierRegistrationOrdered
        {
            var found = GetComponentsInChildren<T>(false);
            var list = new List<T>(found.Length);
            for (var i = 0; i < found.Length; i++)
            {
                if (found[i] is Behaviour behaviour && !behaviour.isActiveAndEnabled)
                {
                    continue;
                }

                // Generate any opted-in auto stable ids before sorting so StableId mode can use them.
                found[i].EnsureStableId();
                list.Add(found[i]);
            }

            SortByMode(list);
            return list;
        }

        private void SortByMode<T>(List<T> list) where T : Component, IRapierRegistrationOrdered
        {
            if (registrationMode == RapierRegistrationMode.HierarchyOrder)
            {
                return; // GetComponentsInChildren already returns hierarchy (depth-first) order.
            }

            // Capture the hierarchy index so it can serve as a stable, deterministic tie-breaker
            // (List.Sort is not a stable sort on its own).
            var hierarchyIndex = new Dictionary<T, int>(list.Count);
            for (var i = 0; i < list.Count; i++)
            {
                hierarchyIndex[list[i]] = i;
            }

            if (registrationMode == RapierRegistrationMode.StableId)
            {
                var missingId = false;
                for (var i = 0; i < list.Count; i++)
                {
                    if (string.IsNullOrEmpty(list[i].StableId))
                    {
                        missingId = true;
                        break;
                    }
                }

                if (missingId)
                {
                    Debug.LogWarning(
                        $"{nameof(RapierWorldComponent)} StableId registration mode found {typeof(T).Name} components without a StableId; those are registered after the identified ones, in hierarchy order.",
                        this);
                }

                // Components with a StableId come first (ordered by id); components without one are
                // placed after them, in hierarchy order. Hierarchy index is the final tie-break.
                list.Sort((a, b) =>
                {
                    var aEmpty = string.IsNullOrEmpty(a.StableId);
                    var bEmpty = string.IsNullOrEmpty(b.StableId);
                    if (aEmpty != bEmpty)
                    {
                        return aEmpty ? 1 : -1;
                    }

                    if (aEmpty)
                    {
                        return hierarchyIndex[a].CompareTo(hierarchyIndex[b]);
                    }

                    var r = string.CompareOrdinal(a.StableId, b.StableId);
                    return r != 0 ? r : hierarchyIndex[a].CompareTo(hierarchyIndex[b]);
                });
                return;
            }

            // ExplicitOrder: RegistrationOrder first, then StableId, then hierarchy index.
            list.Sort((a, b) =>
            {
                var r = a.RegistrationOrder.CompareTo(b.RegistrationOrder);
                if (r != 0)
                {
                    return r;
                }

                r = string.CompareOrdinal(a.StableId ?? string.Empty, b.StableId ?? string.Empty);
                return r != 0 ? r : hierarchyIndex[a].CompareTo(hierarchyIndex[b]);
            });
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
