using System;
using UnityEngine;

namespace AFJK.Rapier.Samples
{
    public sealed class DeterministicReplaySample : MonoBehaviour
    {
        [SerializeField] private int totalTicks = 600;
        [SerializeField] private float timestep = 1f / 60f;
        [SerializeField] private Vector3 gravity = new Vector3(0f, -9.81f, 0f);

        private GameObject visualRoot;
        private GameObject bodyAVisual;
        private GameObject bodyBVisual;
        private string status = "Not started.";
        private int comparedTicks;
        private ulong hashA;
        private ulong hashB;

        private static readonly Vector3 WorldAOffset = new Vector3(-3f, 0f, 0f);
        private static readonly Vector3 WorldBOffset = new Vector3(3f, 0f, 0f);

        private void Start()
        {
            BuildVisuals();
            RunReplay();
        }

        private void OnDisable()
        {
            if (visualRoot != null)
            {
                Destroy(visualRoot);
                visualRoot = null;
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12f, 12f, 460f, 165f), GUI.skin.window);
            GUILayout.Label("Rapier Deterministic Replay");
            GUILayout.Label(status);
            GUILayout.Label($"Compared ticks: {comparedTicks}");
            GUILayout.Label($"World A hash: {hashA}");
            GUILayout.Label($"World B hash: {hashB}");

            if (GUILayout.Button("Run Replay Again"))
            {
                RunReplay();
            }

            GUILayout.EndArea();
        }

        public void RunReplay()
        {
            comparedTicks = 0;
            hashA = 0;
            hashB = 0;

            try
            {
                using (var worldA = RapierWorld.Create())
                using (var worldB = RapierWorld.Create())
                {
                    ConfigureWorld(worldA);
                    ConfigureWorld(worldB);

                    var bodyA = CreateScenario(worldA);
                    var bodyB = CreateScenario(worldB);

                    for (var tick = 0; tick < totalTicks; tick++)
                    {
                        if (!worldA.Step() || !worldB.Step())
                        {
                            status = $"Step failed at tick {tick}.";
                            Debug.LogError(status, this);
                            return;
                        }

                        comparedTicks = tick + 1;
                        hashA = worldA.StateHash();
                        hashB = worldB.StateHash();

                        if (hashA != hashB)
                        {
                            status = $"Hash mismatch at tick {tick}: {hashA} != {hashB}.";
                            Debug.LogError(status, this);
                            UpdateBodyVisuals(worldA, bodyA, worldB, bodyB);
                            return;
                        }
                    }

                    UpdateBodyVisuals(worldA, bodyA, worldB, bodyB);
                }

                status = $"Replay matched for {comparedTicks} ticks.";
                Debug.Log($"DeterministicReplay matched for {comparedTicks} ticks. Hash {hashA}", this);
            }
            catch (Exception ex) when (IsNativeFailure(ex))
            {
                status = "Rapier native plugin is not available. Build the native library and copy it into Packages/com.afjk.rapier/Runtime/Plugins for this platform.";
                Debug.LogWarning(status, this);
            }
        }

        private void ConfigureWorld(RapierWorld world)
        {
            world.SetGravity(gravity);
            world.SetTimestep(timestep);
        }

        private static RapierRigidBodyHandle CreateScenario(RapierWorld world)
        {
            var floor = world.CreateRigidBody(RapierBodyDesc.Fixed(new Vector3(0f, -0.5f, 0f)));
            world.CreateBoxCollider(
                floor,
                new RapierBoxColliderDesc
                {
                    HalfExtents = new Vector3(3f, 0.5f, 3f),
                    Density = 0f,
                    LocalRotation = Quaternion.identity
                });

            var body = world.CreateRigidBody(
                new RapierBodyDesc
                {
                    BodyType = RapierRigidBodyType.Dynamic,
                    Position = new Vector3(0f, 5f, 0f),
                    Rotation = Quaternion.identity,
                    CanSleep = true
                });

            world.CreateBoxCollider(
                body,
                new RapierBoxColliderDesc
                {
                    HalfExtents = Vector3.one * 0.5f,
                    Density = 1f,
                    LocalRotation = Quaternion.identity
                });

            return body;
        }

        private void BuildVisuals()
        {
            if (visualRoot != null)
            {
                Destroy(visualRoot);
            }

            visualRoot = new GameObject("Generated Deterministic Replay Visuals");
            CreateFloorVisual("World A Floor", WorldAOffset);
            CreateFloorVisual("World B Floor", WorldBOffset);
            bodyAVisual = CreateBodyVisual("World A Body", WorldAOffset + new Vector3(0f, 5f, 0f), new Color(0.16f, 0.52f, 0.92f));
            bodyBVisual = CreateBodyVisual("World B Body", WorldBOffset + new Vector3(0f, 5f, 0f), new Color(0.95f, 0.55f, 0.18f));
        }

        private void UpdateBodyVisuals(
            RapierWorld worldA,
            RapierRigidBodyHandle bodyA,
            RapierWorld worldB,
            RapierRigidBodyHandle bodyB)
        {
            if (bodyAVisual != null && worldA.TryGetTransform(bodyA, out var transformA))
            {
                bodyAVisual.transform.SetPositionAndRotation(transformA.Position + WorldAOffset, transformA.Rotation);
            }

            if (bodyBVisual != null && worldB.TryGetTransform(bodyB, out var transformB))
            {
                bodyBVisual.transform.SetPositionAndRotation(transformB.Position + WorldBOffset, transformB.Rotation);
            }
        }

        private void CreateFloorVisual(string name, Vector3 offset)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = name;
            floor.transform.SetParent(visualRoot.transform, false);
            floor.transform.position = offset + new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(6f, 1f, 6f);
            RemoveUnityCollider(floor);
            SetColor(floor, new Color(0.28f, 0.32f, 0.36f));
        }

        private GameObject CreateBodyVisual(string name, Vector3 position, Color color)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = name;
            body.transform.SetParent(visualRoot.transform, false);
            body.transform.position = position;
            body.transform.localScale = Vector3.one;
            RemoveUnityCollider(body);
            SetColor(body, color);
            return body;
        }

        private static bool IsNativeFailure(Exception ex)
        {
            return ex is DllNotFoundException
                || ex is EntryPointNotFoundException
                || ex is BadImageFormatException
                || ex is InvalidOperationException;
        }

        private static void RemoveUnityCollider(GameObject gameObject)
        {
            var unityCollider = gameObject.GetComponent<Collider>();
            if (unityCollider != null)
            {
                UnityEngine.Object.Destroy(unityCollider);
            }
        }

        private static void SetColor(GameObject gameObject, Color color)
        {
            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.material.color = color;
            }
        }
    }
}
