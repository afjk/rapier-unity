using System;
using UnityEngine;

namespace AFJK.Rapier.Samples
{
    public sealed class BasicFallingBallSample : MonoBehaviour
    {
        [SerializeField] private Vector3 gravity = new Vector3(0f, -9.81f, 0f);
        [SerializeField] private float timestep = 1f / 60f;
        [SerializeField] private bool logStateHashEverySecond = true;

        private GameObject generatedRoot;
        private RapierWorldBehaviour worldComponent;
        private RapierRigidbody ballBody;
        private string status = "Not started.";
        private int fixedTick;
        private ulong lastHash;
        private bool rebuildRequested;

        private void Start()
        {
            BuildSample();
        }

        private void Update()
        {
            if (!rebuildRequested)
            {
                return;
            }

            rebuildRequested = false;
            BuildSample();
        }

        private void FixedUpdate()
        {
            if (worldComponent == null || worldComponent.World == null || !worldComponent.World.IsCreated)
            {
                return;
            }

            fixedTick++;
            lastHash = worldComponent.World.StateHash();

            if (ballBody != null)
            {
                status = $"Tick {fixedTick}  Ball Y {ballBody.transform.position.y:0.000}  Hash {lastHash}";
            }

            var ticksPerSecond = Mathf.Max(1, Mathf.RoundToInt(1f / timestep));
            if (logStateHashEverySecond && fixedTick % ticksPerSecond == 0)
            {
                Debug.Log($"BasicFallingBall tick {fixedTick}: Rapier state hash {lastHash}", this);
            }
        }

        private void OnDisable()
        {
            DestroyGeneratedRoot();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12f, 12f, 420f, 150f), GUI.skin.window);
            GUILayout.Label("Rapier Basic Falling Ball");
            GUILayout.Label(status);

            if (GUILayout.Button("Reset Sample"))
            {
                RequestRebuild();
            }

            GUILayout.EndArea();
        }

        private void RequestRebuild()
        {
            DestroyGeneratedRoot();
            status = "Reset requested.";
            rebuildRequested = true;
        }

        private void BuildSample()
        {
            DestroyGeneratedRoot();
            fixedTick = 0;
            lastHash = 0;
            ballBody = null;

            if (!TryProbeNative(out var nativeError))
            {
                status = nativeError;
                Debug.LogWarning(nativeError, this);
                return;
            }

            generatedRoot = new GameObject("Generated Basic Falling Ball");

            var worldObject = new GameObject("Rapier World");
            worldObject.transform.SetParent(generatedRoot.transform, false);
            worldComponent = worldObject.AddComponent<RapierWorldBehaviour>();
            worldComponent.Gravity = gravity;
            worldComponent.Timestep = timestep;
            worldComponent.StepMode = RapierWorldStepMode.FixedUpdate;
            worldComponent.LogStateHash = false;

            CreateFloor(worldObject.transform);
            CreateBall(worldObject.transform);

            status = "Rapier world created. The ball is simulated by Rapier, not Unity Physics.";
        }

        private void CreateFloor(Transform parent)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Rapier Fixed Floor";
            floor.SetActive(false);
            floor.transform.SetParent(parent, false);
            floor.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(12f, 1f, 12f);
            RemoveUnityCollider(floor);
            SetColor(floor, new Color(0.28f, 0.32f, 0.36f));

            var body = floor.AddComponent<RapierRigidbody>();
            body.BodyType = RapierRigidBodyType.Fixed;

            var collider = floor.AddComponent<RapierBoxCollider>();
            collider.HalfExtents = Vector3.one * 0.5f;
            collider.Density = 0f;

            floor.SetActive(true);
        }

        private void CreateBall(Transform parent)
        {
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Rapier Dynamic Ball";
            ball.SetActive(false);
            ball.transform.SetParent(parent, false);
            ball.transform.localPosition = new Vector3(0f, 5f, 0f);
            ball.transform.localScale = Vector3.one;
            RemoveUnityCollider(ball);
            SetColor(ball, new Color(0.16f, 0.52f, 0.92f));

            ballBody = ball.AddComponent<RapierRigidbody>();
            ballBody.BodyType = RapierRigidBodyType.Dynamic;
            ballBody.SyncTransformFromRapier = true;

            var collider = ball.AddComponent<RapierSphereCollider>();
            collider.Radius = 0.5f;
            collider.Density = 1f;

            ball.SetActive(true);
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

        private static void RemoveUnityCollider(GameObject gameObject)
        {
            var unityCollider = gameObject.GetComponent<Collider>();
            if (unityCollider == null)
            {
                return;
            }

            UnityEngine.Object.Destroy(unityCollider);
        }

        private static void SetColor(GameObject gameObject, Color color)
        {
            var meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.material.color = color;
            }
        }

        private void DestroyGeneratedRoot()
        {
            if (generatedRoot == null)
            {
                worldComponent = null;
                return;
            }

            Destroy(generatedRoot);
            generatedRoot = null;
            worldComponent = null;
            ballBody = null;
        }
    }
}
