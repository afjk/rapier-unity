using System;
using UnityEngine;

namespace AFJK.Rapier.Samples
{
    /// <summary>
    /// Runs two identical Rapier <b>component</b> worlds side by side and shows that they stay in
    /// sync. Each world is authored in the Scene with <see cref="RapierWorldBehaviour"/>,
    /// <see cref="RapierRigidbody"/>, and <see cref="RapierBoxCollider"/> (a fixed floor, a fixed
    /// wall, and a dynamic dice). A dice is rolled once, then thrown again from the resulting state.
    /// Both worlds receive the same authored setup, the same timestep, the same initial state, and
    /// the same inputs at the same ticks, so their <see cref="RapierWorldBehaviour.StateHash"/>
    /// values — and the dice faces — match throughout the run.
    ///
    /// The two world roots are offset in the Scene only so both are visible. To keep the two native
    /// worlds bit-identical regardless of where their roots sit, every body is normalized into its
    /// own world-root-local frame on reset, and the dice visuals are driven back out into world
    /// space each step. The determinism proof itself never depends on the root offset.
    /// </summary>
    public sealed class DeterministicDiceSample : MonoBehaviour
    {
        private enum Phase
        {
            Idle,
            FirstRoll,
            SecondThrow,
            Finished,
            Diverged
        }

        [Header("Worlds (assign the two RapierWorldBehaviour roots)")]
        [SerializeField] private RapierWorldBehaviour worldA;
        [SerializeField] private RapierWorldBehaviour worldB;

        [Header("Dice (the dynamic RapierRigidbody in each world)")]
        [SerializeField] private RapierRigidbody diceA;
        [SerializeField] private RapierRigidbody diceB;

        [Header("Phase ticks (fixed for a deterministic, easy-to-read run)")]
        [SerializeField] private int firstRollTick;
        [SerializeField] private int secondThrowTick = 240;
        [SerializeField] private int finishTick = 520;

        [Header("First roll (same inputs applied to both dice at firstRollTick)")]
        [SerializeField] private Vector3 firstLinearVelocity = new Vector3(1.25f, 3.5f, 0.75f);
        [SerializeField] private Vector3 firstAngularVelocity = new Vector3(7f, 4f, 9f);

        [Header("Second throw (same impulses applied to both dice at secondThrowTick)")]
        [SerializeField] private Vector3 secondImpulse = new Vector3(0.25f, 4.5f, -0.5f);
        [SerializeField] private Vector3 secondTorqueImpulse = new Vector3(4f, 8f, 2f);

        private Phase phase = Phase.Idle;
        private int tick;
        private ulong hashA;
        private ulong hashB;
        private int divergedTick = -1;
        private int firstFaceA;
        private int firstFaceB;
        private int secondFaceA;
        private int secondFaceB;
        private string status = "Not started.";

        private void Start()
        {
            RunAgain();
        }

        private void FixedUpdate()
        {
            if (phase != Phase.FirstRoll && phase != Phase.SecondThrow)
            {
                return;
            }

            // First roll: same linear and angular velocity on both dice at the same tick.
            if (tick == firstRollTick)
            {
                diceA.SetLinearVelocity(firstLinearVelocity);
                diceA.SetAngularVelocity(firstAngularVelocity);
                diceB.SetLinearVelocity(firstLinearVelocity);
                diceB.SetAngularVelocity(firstAngularVelocity);
            }

            // Second throw: same impulse and torque impulse on both dice, applied to the state that
            // resulted from the first roll. The first-roll face is captured here for the HUD.
            if (tick == secondThrowTick)
            {
                firstFaceA = TopFace(diceA);
                firstFaceB = TopFace(diceB);

                diceA.ApplyImpulse(secondImpulse);
                diceA.ApplyTorqueImpulse(secondTorqueImpulse);
                diceB.ApplyImpulse(secondImpulse);
                diceB.ApplyTorqueImpulse(secondTorqueImpulse);

                phase = Phase.SecondThrow;
            }

            // Always step the two worlds in the same order.
            worldA.Step();
            worldB.Step();

            // Drive each dice's visual transform from its world-root-local Rapier transform.
            SyncDiceVisual(worldA, diceA);
            SyncDiceVisual(worldB, diceB);

            hashA = worldA.StateHash();
            hashB = worldB.StateHash();

            if (hashA != hashB)
            {
                divergedTick = tick;
                phase = Phase.Diverged;
                status = $"Diverged at tick {tick}.";
                return;
            }

            tick++;

            if (tick >= finishTick)
            {
                secondFaceA = TopFace(diceA);
                secondFaceB = TopFace(diceB);
                phase = Phase.Finished;
                status = "Finished. Worlds stayed in sync.";
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12f, 12f, 460f, 360f), GUI.skin.window);

            GUILayout.Label("Deterministic Dice");
            GUILayout.Label("Two Rapier component worlds.");
            GUILayout.Label("Same setup. Same timestep. Same inputs at the same ticks.");
            GUILayout.Space(6f);

            GUILayout.Label($"Phase: {PhaseLabel(phase)}");
            GUILayout.Label($"Tick: {tick}");
            GUILayout.Label($"StateHash A: 0x{hashA:x16}");
            GUILayout.Label($"StateHash B: 0x{hashB:x16}");
            GUILayout.Label($"Status: {(divergedTick < 0 ? "MATCH" : $"DIVERGED (tick {divergedTick})")}");
            GUILayout.Label($"First Result:  A {FaceLabel(firstFaceA)} / B {FaceLabel(firstFaceB)}");
            GUILayout.Label($"Second Result: A {FaceLabel(secondFaceA)} / B {FaceLabel(secondFaceB)}");

            GUILayout.Space(6f);
            GUILayout.Label(status);

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Run Again"))
            {
                RunAgain();
            }

            if (GUILayout.Button("Reset"))
            {
                ResetRun();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        // Rebuilds both worlds into an identical initial state and starts the run.
        private void RunAgain()
        {
            if (!PrepareRun())
            {
                return;
            }

            phase = Phase.FirstRoll;
            status = "Running. Worlds are in sync.";
        }

        // Rebuilds both worlds into an identical initial state but leaves them paused (Idle).
        private void ResetRun()
        {
            if (!PrepareRun())
            {
                return;
            }

            phase = Phase.Idle;
            status = "Reset. Press Run Again to roll.";
        }

        private bool PrepareRun()
        {
            phase = Phase.Idle;
            tick = 0;
            hashA = 0;
            hashB = 0;
            divergedTick = -1;
            firstFaceA = 0;
            firstFaceB = 0;
            secondFaceA = 0;
            secondFaceB = 0;

            if (worldA == null || worldB == null || diceA == null || diceB == null)
            {
                status = "Assign World A/B and Dice A/B in the inspector.";
                Debug.LogWarning(status, this);
                return false;
            }

            if (!TryProbeNative(out var nativeError))
            {
                status = nativeError;
                Debug.LogWarning(nativeError, this);
                return false;
            }

            PrepareWorld(worldA);
            PrepareWorld(worldB);

            hashA = worldA.StateHash();
            hashB = worldB.StateHash();
            return true;
        }

        // Rebuilds a world deterministically, then normalizes every body into the world-root-local
        // frame so the two native worlds are identical no matter where their roots sit in the Scene.
        // The built-in world-space transform sync is disabled because the dice visuals are driven
        // back out into world space explicitly in SyncDiceVisual.
        private void PrepareWorld(RapierWorldBehaviour world)
        {
            world.RegistrationMode = RapierRegistrationMode.HierarchyOrder;
            world.RebuildWorld();

            var root = world.transform;
            var bodies = world.GetComponentsInChildren<RapierRigidbody>(true);
            for (var i = 0; i < bodies.Length; i++)
            {
                var body = bodies[i];
                body.SyncTransformFromRapier = false;

                if (!body.IsRegistered || body.World == null || !body.World.IsCreated)
                {
                    continue;
                }

                var localPosition = root.InverseTransformPoint(body.transform.position);
                var localRotation = Quaternion.Inverse(root.rotation) * body.transform.rotation;
                body.World.SetTransform(body.BodyHandle, new RapierTransform(localPosition, localRotation));
            }
        }

        private static void SyncDiceVisual(RapierWorldBehaviour world, RapierRigidbody dice)
        {
            if (!dice.TryGetTransform(out var local))
            {
                return;
            }

            var root = world.transform;
            dice.transform.SetPositionAndRotation(
                root.TransformPoint(local.Position),
                root.rotation * local.Rotation);
        }

        // Picks the dice face pointing up from its (world-root-local) orientation. The numbering is
        // arbitrary; A and B use the same mapping, so a match proves the worlds agree.
        private static int TopFace(RapierRigidbody dice)
        {
            if (!dice.TryGetTransform(out var local))
            {
                return 0;
            }

            var rotation = local.Rotation;
            var best = -2f;
            var face = 0;

            CheckFace(rotation * Vector3.up, 1, ref best, ref face);
            CheckFace(rotation * Vector3.down, 6, ref best, ref face);
            CheckFace(rotation * Vector3.right, 2, ref best, ref face);
            CheckFace(rotation * Vector3.left, 5, ref best, ref face);
            CheckFace(rotation * Vector3.forward, 3, ref best, ref face);
            CheckFace(rotation * Vector3.back, 4, ref best, ref face);

            return face;
        }

        private static void CheckFace(Vector3 axis, int value, ref float best, ref int face)
        {
            var dot = Vector3.Dot(axis, Vector3.up);
            if (dot > best)
            {
                best = dot;
                face = value;
            }
        }

        private static string PhaseLabel(Phase value)
        {
            switch (value)
            {
                case Phase.FirstRoll:
                    return "First Roll";
                case Phase.SecondThrow:
                    return "Second Throw";
                case Phase.Finished:
                    return "Finished";
                case Phase.Diverged:
                    return "Diverged";
                default:
                    return "Idle";
            }
        }

        private static string FaceLabel(int face)
        {
            return face <= 0 ? "-" : face.ToString();
        }

        private static bool TryProbeNative(out string error)
        {
            try
            {
                using (var world = RapierWorld.Create())
                {
                    world.SetGravity(Vector3.zero);
                    world.SetTimestep(1f / 60f);
                }

                error = null;
                return true;
            }
            catch (Exception ex) when (IsNativeFailure(ex))
            {
                error = "Rapier native plugin is not available. Build the native library and copy it into Packages/com.afjk.rapier/Runtime/Plugins for this platform.";
                return false;
            }
        }

        private static bool IsNativeFailure(Exception ex)
        {
            return ex is DllNotFoundException
                || ex is EntryPointNotFoundException
                || ex is BadImageFormatException
                || ex is InvalidOperationException;
        }
    }
}
