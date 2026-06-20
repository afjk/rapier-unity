using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace AFJK.Rapier.Samples
{
    /// <summary>
    /// Runs two identical Rapier <b>component</b> worlds side by side and shows that they stay in
    /// sync. Each world is authored in the Scene with <see cref="RapierWorldBehaviour"/>,
    /// <see cref="RapierRigidbody"/>, and <see cref="RapierBoxCollider"/> (a fixed floor, a fixed
    /// three fixed walls, and a dynamic dice). A dice is rolled once automatically, then can be
    /// thrown manually with the Space key. Both worlds receive the same authored setup, the same
    /// timestep, the same initial state, and the same inputs at the same ticks, so their
    /// <see cref="RapierWorldBehaviour.StateHash"/> values — and the dice faces — match throughout
    /// the run.
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
            AwaitingThrow,
            ChargingThrow,
            ManualThrow,
            Diverged
        }

        [Header("Worlds (assign the two RapierWorldBehaviour roots)")]
        [SerializeField] private RapierWorldBehaviour worldA;
        [SerializeField] private RapierWorldBehaviour worldB;

        [Header("Dice (the dynamic RapierRigidbody in each world)")]
        [SerializeField] private RapierRigidbody diceA;
        [SerializeField] private RapierRigidbody diceB;

        [Header("First auto roll timing")]
        [SerializeField] private int firstRollTick;
        [FormerlySerializedAs("secondThrowTick")]
        [SerializeField] private int firstResultTick = 240;
        [SerializeField] private int finishTick = 520;

        [Header("First roll (same inputs applied to both dice at firstRollTick)")]
        [SerializeField] private Vector3 firstLinearVelocity = new Vector3(1.25f, 3.5f, 0.75f);
        [SerializeField] private Vector3 firstAngularVelocity = new Vector3(7f, 4f, 9f);

        [Header("Manual throw (same impulses applied to both dice when Space is released)")]
        [SerializeField] private Vector3 secondImpulse = new Vector3(0.25f, 4.5f, -0.5f);
        [SerializeField] private Vector3 secondTorqueImpulse = new Vector3(4f, 8f, 2f);

        [Header("HUD")]
        [SerializeField] private int hudFontSize = 18;

        private const string DicePipsRootName = "Generated Dice Pips";
        private const float PipFaceOffset = 0.53f;
        private const float PipGridOffset = 0.22f;
        private const float PipDiameter = 0.12f;
        private const float ChargeSpinDegreesPerSecond = 540f;
        private const float FullChargeTicks = 90f;

        private static Material pipMaterial;
        private static Material onePipMaterial;

        private Phase phase = Phase.Idle;
        private int tick;
        private ulong hashA;
        private ulong hashB;
        private int divergedTick = -1;
        private int firstFaceA;
        private int firstFaceB;
        private int secondFaceA;
        private int secondFaceB;
        private int chargeTicks;
        private int manualThrowCount;
        private bool chargeRequested;
        private bool releaseRequested;
        private string status = "Not started.";
        private Vector2 hudScrollPosition;
        private GUISkin cachedHudSkin;
        private GUIStyle hudWindowStyle;
        private GUIStyle hudTitleStyle;
        private GUIStyle hudLabelStyle;
        private GUIStyle hudButtonStyle;
        private int cachedHudFontSize;

        // Per-world snapshot of the authored bodies and their initial parent-local poses, captured
        // before the simulation runs. Used to restore the exact starting state on every reset, since
        // SyncDiceVisual overwrites the dice GameObject transforms while the run plays.
        private WorldCache cacheA;
        private WorldCache cacheB;

        private sealed class WorldCache
        {
            public RapierWorldBehaviour World;
            public RapierRigidbody[] Bodies;
            public Vector3[] LocalPositions;
            public Quaternion[] LocalRotations;
        }

        private void Awake()
        {
            EnsureDicePips(diceA);
            EnsureDicePips(diceB);

            cacheA = BuildCache(worldA);
            cacheB = BuildCache(worldB);
        }

        private void Start()
        {
            RunAgain();
        }

        private void Update()
        {
            if ((phase == Phase.AwaitingThrow || phase == Phase.ManualThrow || CanInterruptFirstRoll()) &&
                Input.GetKeyDown(KeyCode.Space))
            {
                chargeRequested = true;
            }

            if (phase == Phase.ChargingThrow && Input.GetKeyUp(KeyCode.Space))
            {
                releaseRequested = true;
            }
        }

        private void FixedUpdate()
        {
            switch (phase)
            {
                case Phase.FirstRoll:
                    FixedUpdateFirstRoll();
                    break;
                case Phase.AwaitingThrow:
                    FixedUpdateAwaitingThrow();
                    break;
                case Phase.ChargingThrow:
                    FixedUpdateChargingThrow();
                    break;
                case Phase.ManualThrow:
                    FixedUpdateManualThrow();
                    break;
            }
        }

        private void OnGUI()
        {
            EnsureHudStyles();

            GUILayout.BeginArea(new Rect(12f, 12f, 740f, 500f), hudWindowStyle);
            hudScrollPosition = GUILayout.BeginScrollView(
                hudScrollPosition,
                false,
                false,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            GUILayout.Label("Deterministic Dice", hudTitleStyle);
            GUILayout.Label("Two Rapier component worlds.", hudLabelStyle);
            GUILayout.Label("Same setup. Same timestep. Same inputs at the same ticks.", hudLabelStyle);
            GUILayout.Space(8f);

            GUILayout.Label($"Phase: {PhaseLabel(phase)}", hudLabelStyle);
            GUILayout.Label($"Tick: {tick}", hudLabelStyle);
            GUILayout.Label($"StateHash A: 0x{hashA:x16}", hudLabelStyle);
            GUILayout.Label($"StateHash B: 0x{hashB:x16}", hudLabelStyle);
            GUILayout.Label($"Status: {(divergedTick < 0 ? "MATCH" : $"DIVERGED (tick {divergedTick})")}", hudLabelStyle);
            GUILayout.Label($"First Result:  A {FaceLabel(firstFaceA)} / B {FaceLabel(firstFaceB)}", hudLabelStyle);
            GUILayout.Label($"Manual Result: A {FaceLabel(secondFaceA)} / B {FaceLabel(secondFaceB)}", hudLabelStyle);
            GUILayout.Label($"Manual Throws: {manualThrowCount}", hudLabelStyle);

            GUILayout.Space(8f);
            GUILayout.Label(status, hudLabelStyle);

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Run Again", hudButtonStyle, GUILayout.Height(cachedHudFontSize + 14f)))
            {
                RunAgain();
            }

            if (GUILayout.Button("Reset", hudButtonStyle, GUILayout.Height(cachedHudFontSize + 14f)))
            {
                ResetRun();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void EnsureHudStyles()
        {
            var fontSize = Mathf.Max(12, hudFontSize);
            if (hudLabelStyle != null && cachedHudSkin == GUI.skin && cachedHudFontSize == fontSize)
            {
                return;
            }

            cachedHudSkin = GUI.skin;
            cachedHudFontSize = fontSize;
            hudWindowStyle = new GUIStyle(GUI.skin.window)
            {
                fontSize = fontSize,
                padding = new RectOffset(14, 14, 12, 12)
            };
            hudTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize + 4,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            hudLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                wordWrap = true
            };
            hudButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = fontSize
            };
        }

        // Rebuilds both worlds into an identical initial state and starts the run.
        private void RunAgain()
        {
            if (!PrepareRun())
            {
                return;
            }

            phase = Phase.FirstRoll;
            status = "Auto first roll running. Worlds are in sync.";
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
            chargeTicks = 0;
            manualThrowCount = 0;
            chargeRequested = false;
            releaseRequested = false;

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

            EnsureDicePips(diceA);
            EnsureDicePips(diceB);

            // Build the caches on demand in case Awake ran before the references were assigned.
            cacheA = cacheA ?? BuildCache(worldA);
            cacheB = cacheB ?? BuildCache(worldB);

            PrepareWorld(cacheA);
            PrepareWorld(cacheB);

            hashA = worldA.StateHash();
            hashB = worldB.StateHash();
            return true;
        }

        private void FixedUpdateFirstRoll()
        {
            // First roll: same linear and angular velocity on both dice at the same tick.
            if (tick == firstRollTick)
            {
                diceA.SetLinearVelocity(firstLinearVelocity);
                diceA.SetAngularVelocity(firstAngularVelocity);
                diceB.SetLinearVelocity(firstLinearVelocity);
                diceB.SetAngularVelocity(firstAngularVelocity);
            }

            if (!StepWorldsAndCompare())
            {
                return;
            }

            tick++;

            if (tick >= FirstManualThrowReadyTick())
            {
                firstFaceA = TopFace(diceA);
                firstFaceB = TopFace(diceB);

                if (chargeRequested || Input.GetKey(KeyCode.Space))
                {
                    BeginManualCharge();
                    chargeRequested = false;
                    return;
                }

                status = "First roll is live. Hold Space anytime to spin and toss.";
            }

            if (tick >= firstResultTick)
            {
                firstFaceA = TopFace(diceA);
                firstFaceB = TopFace(diceB);
            }

            if (tick >= finishTick)
            {
                firstFaceA = TopFace(diceA);
                firstFaceB = TopFace(diceB);
                phase = Phase.AwaitingThrow;
                status = "First roll complete. Hold Space to spin the dice; release to throw.";
            }
        }

        private void FixedUpdateAwaitingThrow()
        {
            if (chargeRequested || Input.GetKey(KeyCode.Space))
            {
                BeginManualCharge();
            }

            chargeRequested = false;
            releaseRequested = false;
        }

        private void BeginManualCharge()
        {
            chargeTicks = 0;
            phase = Phase.ChargingThrow;
            status = "Charging throw. Release Space to toss.";

            FreezeDice(diceA);
            FreezeDice(diceB);
            UpdateHashesAndCompare();
        }

        private void FixedUpdateChargingThrow()
        {
            if (releaseRequested || !Input.GetKey(KeyCode.Space))
            {
                ReleaseManualThrow();
                return;
            }

            chargeRequested = false;
            RotateChargingDice();
        }

        private void RotateChargingDice()
        {
            var delta = Quaternion.AngleAxis(
                ChargeSpinDegreesPerSecond * Time.fixedDeltaTime,
                ChargeSpinAxis());

            RotateDiceForCharge(worldA, diceA, delta);
            RotateDiceForCharge(worldB, diceB, delta);
            chargeTicks++;

            if (UpdateHashesAndCompare())
            {
                status = $"Charging throw ({chargeTicks} ticks). Release Space to toss.";
            }
        }

        private void ReleaseManualThrow()
        {
            releaseRequested = false;
            chargeRequested = false;

            var charge = Mathf.Clamp01(chargeTicks / FullChargeTicks);
            var impulse = secondImpulse * Mathf.Lerp(0.85f, 1.2f, charge);
            var torqueImpulse = secondTorqueImpulse + ChargeSpinAxis() * Mathf.Lerp(0f, 3f, charge);

            diceA.ApplyImpulse(impulse);
            diceA.ApplyTorqueImpulse(torqueImpulse);
            diceB.ApplyImpulse(impulse);
            diceB.ApplyTorqueImpulse(torqueImpulse);

            manualThrowCount++;
            phase = Phase.ManualThrow;
            status = $"Manual throw {manualThrowCount} released. Hold Space anytime to throw again.";
        }

        private void FixedUpdateManualThrow()
        {
            if (chargeRequested || Input.GetKey(KeyCode.Space))
            {
                BeginManualCharge();
                chargeRequested = false;
                return;
            }

            if (!StepWorldsAndCompare())
            {
                return;
            }

            tick++;

            secondFaceA = TopFace(diceA);
            secondFaceB = TopFace(diceB);
            status = "Manual throw is live. Hold Space anytime to spin and toss again.";
        }

        private bool StepWorldsAndCompare()
        {
            // Always step the two worlds in the same order.
            worldA.Step();
            worldB.Step();

            // Drive each dice's visual transform from its world-root-local Rapier transform.
            SyncDiceVisual(worldA, diceA);
            SyncDiceVisual(worldB, diceB);

            return UpdateHashesAndCompare();
        }

        private bool UpdateHashesAndCompare()
        {
            hashA = worldA.StateHash();
            hashB = worldB.StateHash();

            if (hashA == hashB)
            {
                return true;
            }

            divergedTick = tick;
            phase = Phase.Diverged;
            status = $"Diverged at tick {tick}.";
            return false;
        }

        private int FirstManualThrowReadyTick()
        {
            return Mathf.Max(1, firstRollTick + 1);
        }

        private bool CanInterruptFirstRoll()
        {
            return phase == Phase.FirstRoll && tick >= FirstManualThrowReadyTick();
        }

        private static void FreezeDice(RapierRigidbody dice)
        {
            if (dice == null)
            {
                return;
            }

            dice.SetLinearVelocity(Vector3.zero, false);
            dice.SetAngularVelocity(Vector3.zero, false);
        }

        private static void RotateDiceForCharge(RapierWorldBehaviour world, RapierRigidbody dice, Quaternion delta)
        {
            if (world == null || dice == null || dice.World == null || !dice.TryGetTransform(out var local))
            {
                return;
            }

            dice.World.SetTransform(dice.BodyHandle, new RapierTransform(local.Position, delta * local.Rotation));
            FreezeDice(dice);
            SyncDiceVisual(world, dice);
        }

        private static Vector3 ChargeSpinAxis()
        {
            return new Vector3(0.45f, 1f, 0.25f).normalized;
        }

        // Snapshots a world's bodies and their authored parent-local poses so every run can start
        // from the exact same state, even after a previous run moved the dice.
        private static WorldCache BuildCache(RapierWorldBehaviour world)
        {
            if (world == null)
            {
                return null;
            }

            var bodies = world.GetComponentsInChildren<RapierRigidbody>(true);
            var cache = new WorldCache
            {
                World = world,
                Bodies = bodies,
                LocalPositions = new Vector3[bodies.Length],
                LocalRotations = new Quaternion[bodies.Length]
            };

            for (var i = 0; i < bodies.Length; i++)
            {
                cache.LocalPositions[i] = bodies[i].transform.localPosition;
                cache.LocalRotations[i] = bodies[i].transform.localRotation;
            }

            return cache;
        }

        private static void EnsureDicePips(RapierRigidbody dice)
        {
            if (dice == null || dice.transform.Find(DicePipsRootName) != null)
            {
                return;
            }

            var root = new GameObject(DicePipsRootName).transform;
            root.SetParent(dice.transform, false);
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;

            AddFacePips(root, Vector3.up, Vector3.right, Vector3.forward, 1);
            AddFacePips(root, Vector3.down, Vector3.right, Vector3.back, 6);
            AddFacePips(root, Vector3.right, Vector3.back, Vector3.up, 2);
            AddFacePips(root, Vector3.left, Vector3.forward, Vector3.up, 5);
            AddFacePips(root, Vector3.forward, Vector3.right, Vector3.up, 3);
            AddFacePips(root, Vector3.back, Vector3.left, Vector3.up, 4);
        }

        private static void AddFacePips(Transform parent, Vector3 normal, Vector3 u, Vector3 v, int face)
        {
            switch (face)
            {
                case 1:
                    AddPip(parent, normal, u, v, 0f, 0f, face);
                    break;
                case 2:
                    AddPip(parent, normal, u, v, -PipGridOffset, -PipGridOffset, face);
                    AddPip(parent, normal, u, v, PipGridOffset, PipGridOffset, face);
                    break;
                case 3:
                    AddPip(parent, normal, u, v, -PipGridOffset, -PipGridOffset, face);
                    AddPip(parent, normal, u, v, 0f, 0f, face);
                    AddPip(parent, normal, u, v, PipGridOffset, PipGridOffset, face);
                    break;
                case 4:
                    AddPip(parent, normal, u, v, -PipGridOffset, -PipGridOffset, face);
                    AddPip(parent, normal, u, v, -PipGridOffset, PipGridOffset, face);
                    AddPip(parent, normal, u, v, PipGridOffset, -PipGridOffset, face);
                    AddPip(parent, normal, u, v, PipGridOffset, PipGridOffset, face);
                    break;
                case 5:
                    AddPip(parent, normal, u, v, -PipGridOffset, -PipGridOffset, face);
                    AddPip(parent, normal, u, v, -PipGridOffset, PipGridOffset, face);
                    AddPip(parent, normal, u, v, 0f, 0f, face);
                    AddPip(parent, normal, u, v, PipGridOffset, -PipGridOffset, face);
                    AddPip(parent, normal, u, v, PipGridOffset, PipGridOffset, face);
                    break;
                case 6:
                    AddPip(parent, normal, u, v, -PipGridOffset, -PipGridOffset, face);
                    AddPip(parent, normal, u, v, -PipGridOffset, 0f, face);
                    AddPip(parent, normal, u, v, -PipGridOffset, PipGridOffset, face);
                    AddPip(parent, normal, u, v, PipGridOffset, -PipGridOffset, face);
                    AddPip(parent, normal, u, v, PipGridOffset, 0f, face);
                    AddPip(parent, normal, u, v, PipGridOffset, PipGridOffset, face);
                    break;
            }
        }

        private static void AddPip(Transform parent, Vector3 normal, Vector3 u, Vector3 v, float x, float y, int face)
        {
            var pip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pip.name = "Pip";
            pip.transform.SetParent(parent, false);
            pip.transform.localPosition = normal * PipFaceOffset + u * x + v * y;
            pip.transform.localRotation = Quaternion.identity;
            pip.transform.localScale = Vector3.one * PipDiameter * (face == 1 ? 2f : 1f);

            var collider = pip.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = pip.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = PipMaterial(face == 1);
                if (material != null)
                {
                    renderer.sharedMaterial = material;
                }
            }
        }

        private static Material PipMaterial(bool isOnePip)
        {
            if (isOnePip && onePipMaterial != null)
            {
                return onePipMaterial;
            }

            if (!isOnePip && pipMaterial != null)
            {
                return pipMaterial;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader)
            {
                name = isOnePip ? "Deterministic Dice One Pip" : "Deterministic Dice Pip",
                color = isOnePip ? new Color(0.9f, 0.05f, 0.03f, 1f) : new Color(0.02f, 0.02f, 0.02f, 1f)
            };

            if (isOnePip)
            {
                onePipMaterial = material;
            }
            else
            {
                pipMaterial = material;
            }

            return material;
        }

        // Restores every body to its authored pose, rebuilds the world deterministically, then
        // normalizes every body into the world-root-local frame so the two native worlds are
        // identical no matter where their roots sit in the Scene. The built-in world-space transform
        // sync is disabled because the dice visuals are driven back out explicitly in SyncDiceVisual.
        private void PrepareWorld(WorldCache cache)
        {
            var world = cache.World;
            var root = world.transform;

            // Restore the authored starting pose before (re)registering, so the reset state never
            // depends on where the previous run left the dice.
            for (var i = 0; i < cache.Bodies.Length; i++)
            {
                var body = cache.Bodies[i];
                if (body == null)
                {
                    continue;
                }

                body.SyncTransformFromRapier = false;
                body.transform.localPosition = cache.LocalPositions[i];
                body.transform.localRotation = cache.LocalRotations[i];
            }

            world.RegistrationMode = RapierRegistrationMode.HierarchyOrder;
            world.RebuildWorld();

            for (var i = 0; i < cache.Bodies.Length; i++)
            {
                var body = cache.Bodies[i];
                if (body == null || !body.IsRegistered || body.World == null || !body.World.IsCreated)
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
                case Phase.AwaitingThrow:
                    return "Awaiting Throw";
                case Phase.ChargingThrow:
                    return "Charging Throw";
                case Phase.ManualThrow:
                    return "Manual Throw";
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
            // Limited to plugin-loading failures so genuine logic errors are not swallowed.
            return ex is DllNotFoundException
                || ex is EntryPointNotFoundException
                || ex is BadImageFormatException;
        }
    }
}
