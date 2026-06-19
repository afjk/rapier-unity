using System;
using UnityEngine;

namespace AFJK.Rapier.Samples
{
    /// <summary>
    /// Demonstrates path-2 of the project goals ("generate from an importer"): a neutral
    /// <see cref="RapierSceneDescription"/> is turned into Rapier component GameObjects by
    /// <see cref="RapierSceneImporter"/>, then simulated. A real Scene Sync adapter would live
    /// downstream and only need to produce the description; everything below is Scene Sync-agnostic.
    ///
    /// Assign a JSON <see cref="TextAsset"/> (see scene-import-example.json) to import from JSON, or
    /// leave it empty to import a small built-in description.
    /// </summary>
    public sealed class SceneImportSample : MonoBehaviour
    {
        [SerializeField] private TextAsset descriptionJson;
        [SerializeField] private bool runOnStart = true;

        private GameObject generatedRoot;
        private RapierWorldComponent world;
        private string status = "Not started.";
        private int tick;
        private ulong lastHash;
        private bool running;
        private bool rebuildRequested;
        private bool debugDraw;
        private Material debugMaterial;
        private Vector3[] debugVertices;
        private Color[] debugColors;

        private void Start()
        {
            if (runOnStart)
            {
                BuildScene();
            }
        }

        private void Update()
        {
            if (!rebuildRequested)
            {
                return;
            }

            rebuildRequested = false;
            BuildScene();
        }

        private void FixedUpdate()
        {
            if (!running || world == null || world.World == null || !world.World.IsCreated)
            {
                return;
            }

            if (!world.Step())
            {
                running = false;
                status = $"Rapier step failed at tick {tick}.";
                return;
            }

            tick++;
            lastHash = world.StateHash();
            status = $"Imported scene running. Tick {tick}, hash {lastHash:x16}.";
        }

        private void OnDisable()
        {
            DestroyGeneratedRoot();
            if (debugMaterial != null)
            {
                DestroyImmediate(debugMaterial);
                debugMaterial = null;
            }
        }

        private void BuildScene()
        {
            DestroyGeneratedRoot();
            tick = 0;
            lastHash = 0;
            running = false;

            if (!TryProbeNative(out var nativeError))
            {
                status = nativeError;
                Debug.LogWarning(nativeError, this);
                return;
            }

            generatedRoot = new GameObject("Generated Imported Scene");
            EnsureCameraAndLight();

            try
            {
                var description = ResolveDescription();
                world = RapierSceneImporter.Import(description, generatedRoot.transform, "Imported Rapier World");
                AddVisuals(world);
                running = true;
                status = $"Imported {description.bodies.Count} bodies ({description.registrationMode} order) from "
                    + (descriptionJson != null ? descriptionJson.name : "the built-in description") + ".";
                LookAt(new Vector3(0f, 6f, -14f), new Vector3(0f, 2f, 0f));
            }
            catch (Exception ex) when (IsNativeLoadFailure(ex))
            {
                DestroyGeneratedRoot();
                status = "Rapier native plugin is not available. Build and copy rapier_unity_ffi for this platform.";
                Debug.LogWarning($"{status}\n{ex}", this);
            }
        }

        private RapierSceneDescription ResolveDescription()
        {
            if (descriptionJson != null && !string.IsNullOrEmpty(descriptionJson.text))
            {
                var parsed = JsonUtility.FromJson<RapierSceneDescription>(descriptionJson.text);
                if (parsed != null)
                {
                    return parsed;
                }

                Debug.LogWarning("Could not parse the assigned JSON; using the built-in description.", this);
            }

            return BuildDefaultDescription();
        }

        // A small built-in description: a fixed floor plus three falling primitives.
        private static RapierSceneDescription BuildDefaultDescription()
        {
            var description = new RapierSceneDescription
            {
                gravity = new Vector3(0f, -9.81f, 0f),
                timestep = 1f / 60f,
                registrationMode = RapierRegistrationMode.StableId,
                sourceSystem = "BuiltInExample"
            };

            description.bodies.Add(new RapierBodyDescription
            {
                id = "floor",
                order = 0,
                bodyType = RapierRigidBodyType.Fixed,
                position = new Vector3(0f, -0.5f, 0f),
                colliders =
                {
                    new RapierColliderDescription
                    {
                        id = "floor-box",
                        shape = RapierImportColliderShape.Box,
                        halfExtents = new Vector3(8f, 0.5f, 8f),
                        density = 0f,
                        friction = 0.7f
                    }
                }
            });

            AddPrimitiveBody(description, "box-a", 1, RapierImportColliderShape.Box, new Vector3(-1.5f, 6f, 0f));
            AddPrimitiveBody(description, "sphere-b", 2, RapierImportColliderShape.Sphere, new Vector3(0f, 8f, 0.5f));
            AddPrimitiveBody(description, "capsule-c", 3, RapierImportColliderShape.Capsule, new Vector3(1.5f, 7f, -0.5f));
            return description;
        }

        private static void AddPrimitiveBody(RapierSceneDescription description, string id, int order, RapierImportColliderShape shape, Vector3 position)
        {
            description.bodies.Add(new RapierBodyDescription
            {
                id = id,
                order = order,
                bodyType = RapierRigidBodyType.Dynamic,
                position = position,
                colliders =
                {
                    new RapierColliderDescription
                    {
                        id = id + "-collider",
                        shape = shape,
                        halfExtents = Vector3.one * 0.5f,
                        radius = 0.5f,
                        halfHeight = 0.5f,
                        density = 1f,
                        friction = 0.5f,
                        restitution = 0.2f
                    }
                }
            });
        }

        // Adds a Unity primitive per imported collider for visualization. The body GameObject is the
        // physics object (its transform is synced by RapierRigidBodyComponent); the primitive is a
        // child that follows it.
        private static void AddVisuals(RapierWorldComponent worldComponent)
        {
            var bodies = worldComponent.GetComponentsInChildren<RapierRigidBodyComponent>(true);
            for (var i = 0; i < bodies.Length; i++)
            {
                var body = bodies[i];
                var colliders = body.GetComponents<RapierColliderComponent>();
                var color = body.BodyType == RapierRigidBodyType.Fixed
                    ? new Color(0.28f, 0.32f, 0.36f)
                    : new Color(0.16f, 0.52f, 0.92f);

                for (var c = 0; c < colliders.Length; c++)
                {
                    AddColliderVisual(body.transform, colliders[c], color);
                }
            }
        }

        private static void AddColliderVisual(Transform parent, RapierColliderComponent collider, Color color)
        {
            GameObject visual;
            switch (collider)
            {
                case RapierSphereCollider sphere:
                    visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    visual.transform.localScale = Vector3.one * sphere.Radius * 2f;
                    break;
                case RapierCapsuleCollider capsule:
                    visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    var totalHeight = capsule.HalfHeight * 2f + capsule.Radius * 2f;
                    visual.transform.localScale = new Vector3(capsule.Radius * 2f, totalHeight / 2f, capsule.Radius * 2f);
                    break;
                case RapierBoxCollider box:
                    visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    visual.transform.localScale = box.HalfExtents * 2f;
                    break;
                default:
                    return;
            }

            visual.name = collider.GetType().Name + " Visual";
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = collider.LocalPosition;
            visual.transform.localRotation = collider.LocalRotation;

            var unityCollider = visual.GetComponent<Collider>();
            if (unityCollider != null)
            {
                Destroy(unityCollider);
            }

            var renderer = visual.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var shader = Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
                renderer.material = new Material(shader) { color = color };
            }
        }

        private void OnRenderObject()
        {
            if (!debugDraw || world == null || world.World == null || !world.World.IsCreated)
            {
                return;
            }

            const int maxLines = 8192;
            if (debugColors == null || debugColors.Length < maxLines)
            {
                debugColors = new Color[maxLines];
            }

            if (debugVertices == null || debugVertices.Length < maxLines * 2)
            {
                debugVertices = new Vector3[maxLines * 2];
            }

            var lines = world.World.DebugRender(debugVertices, debugColors);
            if (lines <= 0)
            {
                return;
            }

            EnsureDebugMaterial();
            debugMaterial.SetPass(0);
            GL.PushMatrix();
            GL.Begin(GL.LINES);
            for (var i = 0; i < lines; i++)
            {
                GL.Color(debugColors[i]);
                GL.Vertex(debugVertices[i * 2]);
                GL.Vertex(debugVertices[(i * 2) + 1]);
            }

            GL.End();
            GL.PopMatrix();
        }

        private void EnsureDebugMaterial()
        {
            if (debugMaterial != null)
            {
                return;
            }

            var shader = Shader.Find("Hidden/Internal-Colored");
            debugMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            debugMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            debugMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            debugMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            debugMaterial.SetInt("_ZWrite", 0);
            debugMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12f, 12f, 460f, 180f), GUI.skin.window);
            GUILayout.Label("Rapier Scene Import (Scene Sync foundation)");
            GUILayout.Label(status);
            GUILayout.Label($"Tick: {tick}  Hash: {lastHash:x16}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(running ? "Pause" : "Run"))
            {
                running = !running;
            }

            if (GUILayout.Button("Reimport"))
            {
                rebuildRequested = true;
            }
            GUILayout.EndHorizontal();

            debugDraw = GUILayout.Toggle(debugDraw, " Debug draw colliders");
            GUILayout.EndArea();
        }

        private void EnsureCameraAndLight()
        {
            if (Camera.main == null)
            {
                var cameraObject = new GameObject("Scene Import Camera");
                cameraObject.transform.SetParent(generatedRoot.transform, false);
                cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            if (UnityEngine.Object.FindObjectOfType<Light>() == null)
            {
                var lightObject = new GameObject("Scene Import Light");
                lightObject.transform.SetParent(generatedRoot.transform, false);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }

        private static void LookAt(Vector3 eye, Vector3 target)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.transform.position = eye;
            camera.transform.LookAt(target);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 500f;
        }

        private static bool TryProbeNative(out string error)
        {
            try
            {
                using (var probe = RapierWorld.Create())
                {
                    probe.SetGravity(Vector3.zero);
                    probe.SetTimestep(1f / 60f);
                }

                error = null;
                return true;
            }
            catch (Exception ex) when (IsNativeFailure(ex))
            {
                error = "Rapier native plugin is not available. Build and copy rapier_unity_ffi for this platform.";
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

        private static bool IsNativeLoadFailure(Exception ex)
        {
            return ex is DllNotFoundException
                || ex is EntryPointNotFoundException
                || ex is BadImageFormatException;
        }

        private void DestroyGeneratedRoot()
        {
            if (generatedRoot != null)
            {
                Destroy(generatedRoot);
                generatedRoot = null;
            }

            world = null;
        }
    }
}
