using System;
using System.Collections.Generic;
using UnityEngine;

namespace AFJK.Rapier.Samples
{
    public sealed class RapierJsDemosSample : MonoBehaviour
    {
        private enum DemoKind
        {
            CollisionGroups,
            CharacterController,
            ConvexPolyhedron,
            Ccd,
            Damping,
            Fountain,
            Heightfield,
            Joints,
            KevaTower,
            LockedRotations,
            Platform,
            Pyramid,
            TriangleMesh
        }

        [SerializeField] private DemoKind demo = DemoKind.Pyramid;
        [SerializeField] private float timestep = 1f / 60f;
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool running = true;
        [SerializeField] private int maxFountainBodies = 400;

        private static readonly DemoKind[] DemoValues =
        {
            DemoKind.CollisionGroups,
            DemoKind.CharacterController,
            DemoKind.ConvexPolyhedron,
            DemoKind.Ccd,
            DemoKind.Damping,
            DemoKind.Fountain,
            DemoKind.Heightfield,
            DemoKind.Joints,
            DemoKind.KevaTower,
            DemoKind.LockedRotations,
            DemoKind.Platform,
            DemoKind.Pyramid,
            DemoKind.TriangleMesh
        };

        private static readonly string[] DemoNames =
        {
            "collision groups",
            "character controller",
            "convex polyhedron",
            "CCD",
            "damping",
            "fountain",
            "heightfield",
            "joints",
            "keva tower",
            "locked rotations",
            "platform",
            "pyramid",
            "triangle mesh"
        };

        private sealed class VisualBody
        {
            public string Id;
            public RapierRigidBodyHandle Body;
            public GameObject Visual;
            public bool SyncTransform;
        }

        private readonly List<VisualBody> bodies = new List<VisualBody>();
        private readonly Queue<VisualBody> fountainBodies = new Queue<VisualBody>();

        private GameObject generatedRoot;
        private RapierWorld world;
        private string status = "Not started.";
        private int selectedDemoIndex;
        private int tick;
        private ulong lastHash;
        private bool rebuildRequested;
        private int nextBodyId;
        private int fountainSpawnCounter;
        private RapierColliderHandle lastCollider = RapierColliderHandle.Invalid;

        // Platform demo state.
        private VisualBody platformBody;
        private float platformPhase;

        // Character controller demo state.
        private VisualBody characterBody;
        private RapierQueryShape characterShape;
        private Vector3 characterMovementDirection;

        private void Start()
        {
            selectedDemoIndex = IndexOf(demo);
            if (runOnStart)
            {
                BuildDemo();
            }
        }

        private void Update()
        {
            if (!rebuildRequested)
            {
                return;
            }

            rebuildRequested = false;
            BuildDemo();
        }

        private void FixedUpdate()
        {
            if (!running || world == null || !world.IsCreated)
            {
                return;
            }

            switch (demo)
            {
                case DemoKind.Fountain:
                    PreStepFountain();
                    break;
                case DemoKind.Platform:
                    PreStepPlatform();
                    break;
                case DemoKind.CharacterController:
                    PreStepCharacter();
                    break;
            }

            if (!world.Step())
            {
                running = false;
                status = $"Rapier step failed at tick {tick}.";
                Debug.LogError(status, this);
                return;
            }

            tick++;
            lastHash = world.StateHash();
            UpdateVisuals();
            status = $"{DemoNames[selectedDemoIndex]} running. Tick {tick}, bodies {bodies.Count}, hash {lastHash:x16}.";
        }

        private void OnDisable()
        {
            DisposeWorld();
            DestroyGeneratedRoot();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12f, 12f, 520f, 300f), GUI.skin.window);
            GUILayout.Label("Rapier JS 3D Demo Ports");
            GUILayout.Label(status);
            GUILayout.Label($"Tick: {tick}  Hash: {lastHash:x16}");

            var nextIndex = GUILayout.SelectionGrid(selectedDemoIndex, DemoNames, 2);
            if (nextIndex != selectedDemoIndex)
            {
                selectedDemoIndex = nextIndex;
                demo = DemoValues[selectedDemoIndex];
                RequestRebuild();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(running ? "Pause" : "Run"))
            {
                running = !running;
            }

            if (GUILayout.Button("Restart"))
            {
                RequestRebuild();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("All entries are ported from the Rapier JS 3D demo catalog.");
            GUILayout.EndArea();
        }

        private void RequestRebuild()
        {
            status = "Rebuild requested.";
            rebuildRequested = true;
        }

        private void BuildDemo()
        {
            DisposeWorld();
            DestroyGeneratedRoot();
            bodies.Clear();
            fountainBodies.Clear();
            tick = 0;
            lastHash = 0;
            nextBodyId = 0;
            fountainSpawnCounter = 0;

            if (!TryProbeNative(out var nativeError))
            {
                running = false;
                status = nativeError;
                Debug.LogWarning(nativeError, this);
                return;
            }

            generatedRoot = new GameObject("Generated Rapier JS Demo");
            EnsureCameraAndLight();

            try
            {
                switch (demo)
                {
                    case DemoKind.Ccd:
                        BuildCcd();
                        break;
                    case DemoKind.Damping:
                        BuildDamping();
                        break;
                    case DemoKind.Fountain:
                        BuildFountain();
                        break;
                    case DemoKind.KevaTower:
                        BuildKevaTower();
                        break;
                    case DemoKind.Pyramid:
                        BuildPyramid();
                        break;
                    case DemoKind.CollisionGroups:
                        BuildCollisionGroups();
                        break;
                    case DemoKind.Joints:
                        BuildJoints();
                        break;
                    case DemoKind.Platform:
                        BuildPlatform();
                        break;
                    case DemoKind.LockedRotations:
                        BuildLockedRotations();
                        break;
                    case DemoKind.ConvexPolyhedron:
                        BuildConvexPolyhedron();
                        break;
                    case DemoKind.TriangleMesh:
                        BuildTriangleMesh();
                        break;
                    case DemoKind.Heightfield:
                        BuildHeightfield();
                        break;
                    case DemoKind.CharacterController:
                        BuildCharacterController();
                        break;
                    default:
                        BuildUnsupportedDemo(UnsupportedReason(demo));
                        return;
                }

                running = true;
                UpdateVisuals();
                status = $"{DemoNames[selectedDemoIndex]} loaded from the Rapier JS demo catalog.";
            }
            catch (Exception ex) when (IsNativeLoadFailure(ex))
            {
                DisposeWorld();
                running = false;
                status = "Rapier native plugin is not available. Build and copy rapier_unity_ffi for this platform.";
                Debug.LogWarning($"{status}\n{ex}", this);
            }
        }

        private void BuildUnsupportedDemo(string reason)
        {
            running = false;
            status = $"{DemoNames[selectedDemoIndex]} is not ported yet: {reason}.";
            Debug.Log(status, this);
        }

        private void BuildPyramid()
        {
            CreateWorld(new Vector3(0f, -9.81f, 0f));
            CreateBox("floor", RapierRigidBodyType.Fixed, new Vector3(0f, -0.1f, 0f), Quaternion.identity, new Vector3(30f, 0.1f, 30f), 0f, ColorFor("floor"));

            const float spacing = 1.25f;
            for (var layer = 0; layer < 10; layer++)
            {
                for (var row = layer; row < 10; row++)
                {
                    for (var col = layer; col < 10; col++)
                    {
                        var position = new Vector3(
                            layer * spacing / 2f + (col - layer) * spacing - 10f,
                            layer * spacing + 10f,
                            layer * spacing / 2f + (row - layer) * spacing - 10f);
                        CreateBox(NextId("pyramid-box"), RapierRigidBodyType.Dynamic, position, Quaternion.identity, Vector3.one * 0.5f, 1f, ColorFor("box"));
                    }
                }
            }

            LookAt(new Vector3(-32f, 20f, -28f), Vector3.zero);
        }

        private void BuildKevaTower()
        {
            CreateWorld(new Vector3(0f, -9.81f, 0f));
            CreateBox("floor", RapierRigidBodyType.Fixed, new Vector3(0f, -0.1f, 0f), Quaternion.identity, new Vector3(50f, 0.1f, 50f), 0f, ColorFor("floor"));

            var block = new Vector3(0.1f, 0.5f, 2f);
            var columnsByTier = new[] { 0, 3, 5, 5, 7, 9 };
            var currentY = 0f;

            for (var tier = 5; tier >= 1; tier--)
            {
                var rows = tier;
                var columns = columnsByTier[tier];
                var depth = 3 * tier + 1;
                var span = tier * block.z * 2f;
                CreateKevaStack(block, new Vector3(-span / 2f, currentY, -span / 2f), rows, columns, depth);
                currentY += columns * block.y * 2f + 2f * block.x;
            }

            LookAt(new Vector3(-70f, 18f, 29f), new Vector3(0.6f, 3.1f, -0.3f));
        }

        private void CreateKevaStack(Vector3 block, Vector3 origin, int rows, int columns, int depth)
        {
            var countA = rows;
            var countB = depth;
            var span = 2f * block.z * rows;
            var height = 2f * block.y * columns;
            var pitch = (block.z * rows - block.x) / Mathf.Max(1, depth - 1);
            var shapes = new[]
            {
                block,
                new Vector3(block.z, block.y, block.x)
            };

            for (var layer = 0; layer < columns; layer++)
            {
                var swapped = countA;
                countA = countB;
                countB = swapped;

                var halfExtents = shapes[layer % 2];
                var layerY = halfExtents.y * layer * 2f;

                for (var a = 0; a < countA; a++)
                {
                    var x = layer % 2 == 0 ? pitch * a * 2f : halfExtents.x * a * 2f;
                    for (var b = 0; b < countB; b++)
                    {
                        var z = layer % 2 == 0 ? halfExtents.z * b * 2f : pitch * b * 2f;
                        var position = new Vector3(x + halfExtents.x + origin.x, layerY + halfExtents.y + origin.y, z + halfExtents.z + origin.z);
                        CreateBox(NextId("keva"), RapierRigidBodyType.Dynamic, position, Quaternion.identity, halfExtents, 1f, ColorFor("keva"));
                    }
                }
            }

            var cap = new Vector3(block.z, block.x, block.y);
            for (var x = 0; x < span / (2f * cap.x); x++)
            {
                for (var z = 0; z < span / (2f * cap.z); z++)
                {
                    var position = new Vector3(x * cap.x * 2f + cap.x + origin.x, cap.y + origin.y + height, z * cap.z * 2f + cap.z + origin.z);
                    CreateBox(NextId("keva-cap"), RapierRigidBodyType.Dynamic, position, Quaternion.identity, cap, 1f, ColorFor("keva"));
                }
            }
        }

        private void BuildDamping()
        {
            CreateWorld(Vector3.zero);

            const float fraction = 0.1f;
            for (var i = 0; i < 10; i++)
            {
                var sin = Mathf.Sin(i * fraction * Mathf.PI * 2f);
                var cos = Mathf.Cos(i * fraction * Mathf.PI * 2f);
                CreateBox(
                    NextId("damping-box"),
                    RapierRigidBodyType.Dynamic,
                    new Vector3(sin, cos, 0f),
                    Quaternion.identity,
                    Vector3.one * 0.2f,
                    1f,
                    ColorFor("box"),
                    new Vector3(10f * sin, 10f * cos, 0f),
                    new Vector3(0f, 0f, 100f),
                    (i + 1) * fraction * 10f,
                    (10 - i) * fraction * 10f);
            }

            LookAt(new Vector3(0f, 2f, 20f), new Vector3(0f, 2f, 0f));
        }

        private void BuildCcd()
        {
            CreateWorld(new Vector3(0f, -9.81f, 0f));
            CreateBox("floor", RapierRigidBodyType.Fixed, new Vector3(0f, -0.1f, 0f), Quaternion.identity, new Vector3(30f, 0.1f, 30f), 0f, ColorFor("floor"));

            for (var wall = 0; wall < 5; wall++)
            {
                CreateCcdWall(new Vector3(6f * wall, 0.6f, 0f), 8);
            }

            CreateSphere(
                "ccd-ball",
                RapierRigidBodyType.Dynamic,
                new Vector3(-20f, 2.6f, 0f),
                1f,
                10f,
                ColorFor("sphere"),
                new Vector3(1000f, 0f, 0f),
                Vector3.zero,
                0f,
                0f,
                true);

            LookAt(new Vector3(-32f, 20f, -28f), Vector3.zero);
        }

        private void CreateCcdWall(Vector3 origin, int count)
        {
            for (var row = 0; row < count; row++)
            {
                for (var col = row; col < count; col++)
                {
                    var position = new Vector3(origin.x, origin.y + row, origin.z + 2f * col - row - count);
                    CreateBox(NextId("ccd-wall"), RapierRigidBodyType.Dynamic, position, Quaternion.identity, new Vector3(0.5f, 0.5f, 1f), 1f, ColorFor("box"));
                }
            }
        }

        private void BuildFountain()
        {
            CreateWorld(new Vector3(0f, -9.81f, 0f));
            CreateBox("floor", RapierRigidBodyType.Fixed, Vector3.zero, Quaternion.identity, new Vector3(40f, 0.1f, 40f), 0f, ColorFor("floor"));
            LookAt(new Vector3(-88f, 47f, 84f), new Vector3(0f, 10f, 0f));
        }

        private void PreStepFountain()
        {
            const int spawnInterval = 5;
            var position = new Vector3(0f, 10f, 0f);
            var velocity = new Vector3(0f, 15f, 0f);

            fountainSpawnCounter++;
            if (fountainSpawnCounter % spawnInterval != 0)
            {
                return;
            }

            var shapeIndex = (fountainSpawnCounter / spawnInterval) % 4;
            VisualBody body;
            switch (shapeIndex)
            {
                case 0:
                    body = CreateBox(NextId("fountain-box"), RapierRigidBodyType.Dynamic, position, Quaternion.identity, Vector3.one, 1f, ColorFor("box"), velocity, Vector3.zero, 0f, 0f);
                    break;
                case 1:
                    body = CreateSphere(NextId("fountain-sphere"), RapierRigidBodyType.Dynamic, position, 1f, 1f, ColorFor("sphere"), velocity, Vector3.zero, 0f, 0f, false);
                    break;
                case 2:
                    body = CreateConvexCylinderBody(NextId("fountain-cylinder"), position, 1f, ColorFor("capsule"), velocity);
                    break;
                default:
                    body = CreateConvexConeBody(NextId("fountain-cone"), position, 1f, ColorFor("keva"), velocity);
                    break;
            }

            fountainBodies.Enqueue(body);
            while (fountainBodies.Count > Mathf.Max(1, maxFountainBodies))
            {
                RemoveBody(fountainBodies.Dequeue());
            }
        }

        private void BuildCollisionGroups()
        {
            CreateWorld(new Vector3(0f, -9.81f, 0f));
            CreateCollisionGroupsGround();

            var group1 = RapierWorld.InteractionGroups(0x0001, 0x0001);
            var group2 = RapierWorld.InteractionGroups(0x0002, 0x0002);
            var color1 = new Color(0.37f, 0.73f, 0.38f);
            var color2 = new Color(0.16f, 0.52f, 0.92f);

            const int count = 8;
            const float radius = 0.1f;
            const int layers = 4;
            var shift = radius * 2f;
            var center = shift * (count / 2f);

            for (var layer = 0; layer < layers; layer++)
            {
                for (var xIndex = 0; xIndex < count; xIndex++)
                {
                    for (var zIndex = 0; zIndex < count; zIndex++)
                    {
                        var group = zIndex % 2 == 0 ? group1 : group2;
                        var color = zIndex % 2 == 0 ? color1 : color2;
                        var position = new Vector3(
                            xIndex * shift - center,
                            layer * shift + 2.5f,
                            zIndex * shift - center);

                        CreateBox(
                            NextId("group-box"),
                            RapierRigidBodyType.Dynamic,
                            position,
                            Quaternion.identity,
                            Vector3.one * radius,
                            1f,
                            color);
                        world.SetColliderCollisionGroups(lastCollider, group);
                    }
                }
            }

            LookAt(new Vector3(10f, 5f, 10f), Vector3.zero);
        }

        private void CreateCollisionGroupsGround()
        {
            var body = CreateRigidBody("collision-groups-ground", RapierRigidBodyType.Fixed, Vector3.zero, Quaternion.identity, Vector3.zero, Vector3.zero, 0f, 0f, false);
            CreateBoxCollider("collision-groups-floor", body, new Vector3(5f, 0.1f, 5f), Vector3.zero);

            var group1Collider = CreateBoxCollider("collision-groups-floor-1", body, new Vector3(1f, 0.1f, 1f), new Vector3(0f, 1f, 0f));
            world.SetColliderCollisionGroups(group1Collider, RapierWorld.InteractionGroups(0x0001, 0x0001));

            var group2Collider = CreateBoxCollider("collision-groups-floor-2", body, new Vector3(1f, 0.1f, 1f), new Vector3(0f, 2f, 0f));
            world.SetColliderCollisionGroups(group2Collider, RapierWorld.InteractionGroups(0x0002, 0x0002));

            var visual = new GameObject("collision-groups-ground");
            visual.transform.SetParent(generatedRoot.transform, false);
            CreateBoxVisualChild(visual, "collision-groups-floor", new Vector3(5f, 0.1f, 5f), Vector3.zero, ColorFor("floor"));
            CreateBoxVisualChild(visual, "collision-groups-floor-1", new Vector3(1f, 0.1f, 1f), new Vector3(0f, 1f, 0f), new Color(0.37f, 0.73f, 0.38f));
            CreateBoxVisualChild(visual, "collision-groups-floor-2", new Vector3(1f, 0.1f, 1f), new Vector3(0f, 2f, 0f), new Color(0.16f, 0.52f, 0.92f));
            TrackBody("collision-groups-ground", body, visual, false);
        }

        private void BuildJoints()
        {
            CreateWorld(new Vector3(0f, -9.81f, 0f));
            CreatePrismaticJointChain(new Vector3(20f, 10f, 0f), 5);
            CreateFixedJointGrid(new Vector3(0f, 10f, 0f), 5);
            CreateRevoluteJointChain(new Vector3(20f, 0f, 0f), 3);
            CreateSphericalJointGrid(15);

            LookAt(new Vector3(15f, 5f, 42f), new Vector3(13f, 1f, 1f));
        }

        private void CreatePrismaticJointChain(Vector3 origin, int count)
        {
            const float radius = 0.4f;
            const float shift = 1.0f;

            var parent = CreateBox(
                NextId("prismatic-anchor"),
                RapierRigidBodyType.Fixed,
                origin,
                Quaternion.identity,
                Vector3.one * radius,
                0f,
                ColorFor("floor"));

            for (var i = 0; i < count; i++)
            {
                var position = new Vector3(origin.x, origin.y, origin.z + (i + 1) * shift);
                var child = CreateBox(
                    NextId("prismatic-link"),
                    RapierRigidBodyType.Dynamic,
                    position,
                    Quaternion.identity,
                    Vector3.one * radius,
                    1f,
                    ColorFor("box"));
                var axis = i % 2 == 0 ? new Vector3(1f, 1f, 0f) : new Vector3(-1f, 1f, 0f);
                var joint = world.CreatePrismaticJoint(parent.Body, child.Body, Vector3.zero, new Vector3(0f, 0f, -shift), axis);
                world.SetJointLimits(joint, RapierJointAxis.LinearX, -2f, 2f);
                parent = child;
            }
        }

        private void CreateRevoluteJointChain(Vector3 origin, int count)
        {
            const float radius = 0.4f;
            const float shift = 2.0f;

            var parent = CreateBox(
                NextId("revolute-anchor"),
                RapierRigidBodyType.Fixed,
                new Vector3(origin.x, origin.y, 0f),
                Quaternion.identity,
                Vector3.one * radius,
                0f,
                ColorFor("floor"));

            for (var i = 0; i < count; i++)
            {
                var z = origin.z + i * shift * 2f + shift;
                var positions = new[]
                {
                    new Vector3(origin.x, origin.y, z),
                    new Vector3(origin.x + shift, origin.y, z),
                    new Vector3(origin.x + shift, origin.y, z + shift),
                    new Vector3(origin.x, origin.y, z + shift)
                };

                var links = new VisualBody[4];
                for (var k = 0; k < links.Length; k++)
                {
                    links[k] = CreateBox(
                        NextId("revolute-link"),
                        RapierRigidBodyType.Dynamic,
                        positions[k],
                        Quaternion.identity,
                        Vector3.one * radius,
                        1f,
                        ColorFor("box"));
                }

                world.CreateRevoluteJoint(parent.Body, links[0].Body, Vector3.zero, new Vector3(0f, 0f, -shift), Vector3.forward);
                world.CreateRevoluteJoint(links[0].Body, links[1].Body, Vector3.zero, new Vector3(-shift, 0f, 0f), Vector3.right);
                world.CreateRevoluteJoint(links[1].Body, links[2].Body, Vector3.zero, new Vector3(0f, 0f, -shift), Vector3.forward);
                world.CreateRevoluteJoint(links[2].Body, links[3].Body, Vector3.zero, new Vector3(shift, 0f, 0f), Vector3.right);
                parent = links[3];
            }
        }

        private void CreateFixedJointGrid(Vector3 origin, int count)
        {
            const float radius = 0.4f;
            const float shift = 1.0f;
            var parents = new List<VisualBody>(count * count);

            for (var k = 0; k < count; k++)
            {
                for (var i = 0; i < count; i++)
                {
                    var fixedBody = i == 0 && ((k % 4 == 0 && k != count - 2) || k == count - 1);
                    var child = CreateSphere(
                        NextId("fixed-joint-node"),
                        fixedBody ? RapierRigidBodyType.Fixed : RapierRigidBodyType.Dynamic,
                        new Vector3(origin.x + k * shift, origin.y, origin.z + i * shift),
                        radius,
                        fixedBody ? 0f : 1f,
                        fixedBody ? ColorFor("floor") : ColorFor("sphere"),
                        Vector3.zero,
                        Vector3.zero,
                        0f,
                        0f,
                        false);

                    if (i > 0)
                    {
                        var parent = parents[parents.Count - 1];
                        world.CreateFixedJoint(parent.Body, child.Body, Vector3.zero, new Vector3(0f, 0f, -shift));
                    }

                    if (k > 0)
                    {
                        var parent = parents[parents.Count - count];
                        world.CreateFixedJoint(parent.Body, child.Body, Vector3.zero, new Vector3(-shift, 0f, 0f));
                    }

                    parents.Add(child);
                }
            }
        }

        private void CreateSphericalJointGrid(int count)
        {
            const float radius = 0.4f;
            const float shift = 1.0f;
            var parents = new List<VisualBody>(count * count);

            for (var k = 0; k < count; k++)
            {
                for (var i = 0; i < count; i++)
                {
                    var fixedBody = i == 0 && (k % 4 == 0 || k == count - 1);
                    var child = CreateSphere(
                        NextId("spherical-joint-node"),
                        fixedBody ? RapierRigidBodyType.Fixed : RapierRigidBodyType.Dynamic,
                        new Vector3(k * shift, 0f, i * shift),
                        radius,
                        fixedBody ? 0f : 1f,
                        fixedBody ? ColorFor("floor") : ColorFor("sphere"),
                        Vector3.zero,
                        Vector3.zero,
                        0f,
                        0f,
                        false);

                    if (i > 0)
                    {
                        var parent = parents[parents.Count - 1];
                        world.CreateSphericalJoint(parent.Body, child.Body, Vector3.zero, new Vector3(0f, 0f, -shift));
                    }

                    if (k > 0)
                    {
                        var parent = parents[parents.Count - count];
                        world.CreateSphericalJoint(parent.Body, child.Body, Vector3.zero, new Vector3(-shift, 0f, 0f));
                    }

                    parents.Add(child);
                }
            }
        }

        private void BuildPlatform()
        {
            CreateWorld(new Vector3(0f, -9.81f, 0f));

            BuildPlatformMesh(out var vertices, out var indices, 20, 70f, 4f, 70f);
            platformBody = CreateTrimeshPlatform("platform-trimesh", vertices, indices, ColorFor("floor"));
            platformPhase = 0f;

            const int columns = 4;
            const int layers = 10;
            const float radius = 1f;
            var shift = radius * 3f;
            var centerY = shift * 0.5f;
            var offset = -columns * shift * 0.5f;

            for (var layer = 0; layer < layers; layer++)
            {
                for (var xIndex = 0; xIndex < columns; xIndex++)
                {
                    for (var zIndex = 0; zIndex < columns; zIndex++)
                    {
                        var position = new Vector3(
                            xIndex * shift + offset,
                            layer * shift + centerY + 3f,
                            zIndex * shift + offset);
                        CreatePlatformStackBody(NextId("platform-body"), layer % 5, position, radius);
                    }
                }

                offset -= 0.05f * radius * (columns - 1f);
            }

            LookAt(new Vector3(-88.48024f, 46.91133f, 83.56055f), Vector3.zero);
        }

        private void PreStepPlatform()
        {
            if (platformBody == null || !platformBody.Body.IsValid)
            {
                return;
            }

            platformPhase += 0.016f;
            var dy = Mathf.Sin(platformPhase) * 10f;
            var angularY = Mathf.Sin(platformPhase) * 0.2f;
            world.SetLinearVelocity(platformBody.Body, new Vector3(0f, dy, 0f));
            world.SetAngularVelocity(platformBody.Body, new Vector3(0f, angularY, 0f));
        }

        private VisualBody CreateTrimeshPlatform(string id, Vector3[] vertices, int[] indices, Color color)
        {
            var body = CreateRigidBody(id, RapierRigidBodyType.KinematicVelocityBased, Vector3.zero, Quaternion.identity, Vector3.zero, Vector3.zero, 0f, 0f, false);
            var collider = world.CreateTrimeshCollider(body, vertices, indices, RapierMeshColliderDesc.Default);
            RegisterCollider(id, collider);
            var visual = CreateMeshVisual(id, vertices, indices, color);
            return TrackBody(id, body, visual, true);
        }

        private VisualBody CreatePlatformStackBody(string id, int shapeKind, Vector3 position, float radius)
        {
            switch (shapeKind)
            {
                case 0:
                    return CreateBox(id, RapierRigidBodyType.Dynamic, position, Quaternion.identity, Vector3.one * radius, 1f, ColorFor("box"));
                case 1:
                    return CreateSphere(id, RapierRigidBodyType.Dynamic, position, radius, 1f, ColorFor("sphere"), Vector3.zero, Vector3.zero, 0f, 0f, false);
                case 2:
                    return CreateConvexCylinderBody(id, position, radius, ColorFor("capsule"));
                case 3:
                    return CreateConvexConeBody(id, position, radius, ColorFor("keva"));
                default:
                    return CreateCompoundPlatformBody(id, position, radius);
            }
        }

        private VisualBody CreateConvexCylinderBody(string id, Vector3 position, float radius, Color color, Vector3 linearVelocity = default, float halfHeight = -1f)
        {
            const int segments = 16;
            var cylinderHalfHeight = halfHeight > 0f ? halfHeight : radius;
            var body = CreateRigidBody(id, RapierRigidBodyType.Dynamic, position, Quaternion.identity, linearVelocity, Vector3.zero, 0f, 0f, false);
            var collider = world.CreateConvexHullCollider(body, CylinderHullPoints(cylinderHalfHeight, radius, segments), RapierMeshColliderDesc.Default);
            RegisterCollider(id, collider);

            BuildCylinderMesh(cylinderHalfHeight, radius, segments, out var vertices, out var indices);
            var visual = CreateMeshVisual(id, vertices, indices, color);
            visual.transform.SetPositionAndRotation(position, Quaternion.identity);
            return TrackBody(id, body, visual, true);
        }

        private VisualBody CreateConvexConeBody(string id, Vector3 position, float radius, Color color, Vector3 linearVelocity = default)
        {
            const int segments = 16;
            var body = CreateRigidBody(id, RapierRigidBodyType.Dynamic, position, Quaternion.identity, linearVelocity, Vector3.zero, 0f, 0f, false);
            var collider = world.CreateConvexHullCollider(body, ConeHullPoints(radius, segments), RapierMeshColliderDesc.Default);
            RegisterCollider(id, collider);

            BuildConeMesh(radius, segments, out var vertices, out var indices);
            var visual = CreateMeshVisual(id, vertices, indices, color);
            visual.transform.SetPositionAndRotation(position, Quaternion.identity);
            return TrackBody(id, body, visual, true);
        }

        private VisualBody CreateCompoundPlatformBody(string id, Vector3 position, float radius)
        {
            var body = CreateRigidBody(id, RapierRigidBodyType.Dynamic, position, Quaternion.identity, Vector3.zero, Vector3.zero, 0f, 0f, false);
            var core = Vector3.one * (radius * 0.5f);
            var arm = new Vector3(radius * 0.5f, radius, radius * 0.5f);

            CreateBoxCollider(id + "-core", body, core, Vector3.zero);
            CreateBoxCollider(id + "-arm-positive-x", body, arm, new Vector3(radius, 0f, 0f));
            CreateBoxCollider(id + "-arm-negative-x", body, arm, new Vector3(-radius, 0f, 0f));

            var visual = new GameObject(id);
            visual.transform.SetParent(generatedRoot.transform, false);
            visual.transform.SetPositionAndRotation(position, Quaternion.identity);
            CreateBoxVisualChild(visual, id + "-core", core, Vector3.zero, ColorFor("box"));
            CreateBoxVisualChild(visual, id + "-arm-positive-x", arm, new Vector3(radius, 0f, 0f), ColorFor("box"));
            CreateBoxVisualChild(visual, id + "-arm-negative-x", arm, new Vector3(-radius, 0f, 0f), ColorFor("box"));

            return TrackBody(id, body, visual, true);
        }

        private RapierColliderHandle CreateBoxCollider(string id, RapierRigidBodyHandle body, Vector3 halfExtents, Vector3 localPosition)
        {
            var collider = world.CreateBoxCollider(
                body,
                new RapierBoxColliderDesc
                {
                    HalfExtents = halfExtents,
                    Density = 1f,
                    Friction = 0.5f,
                    HasFriction = true,
                    Restitution = 0f,
                    LocalPosition = localPosition,
                    LocalRotation = Quaternion.identity
                });
            RegisterCollider(id, collider);
            return collider;
        }

        private static void CreateBoxVisualChild(GameObject parent, string id, Vector3 halfExtents, Vector3 localPosition, Color color)
        {
            var child = GameObject.CreatePrimitive(PrimitiveType.Cube);
            child.name = id;
            child.transform.SetParent(parent.transform, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = halfExtents * 2f;
            RemoveUnityCollider(child);
            SetColor(child, color);
        }

        private static Vector3[] CylinderHullPoints(float halfHeight, float radius, int segments)
        {
            var points = new Vector3[segments * 2];

            for (var i = 0; i < segments; i++)
            {
                var angle = Mathf.PI * 2f * i / segments;
                var x = Mathf.Cos(angle) * radius;
                var z = Mathf.Sin(angle) * radius;
                points[i] = new Vector3(x, halfHeight, z);
                points[i + segments] = new Vector3(x, -halfHeight, z);
            }

            return points;
        }

        private static Vector3[] ConeHullPoints(float radius, int segments)
        {
            var points = new Vector3[segments + 1];
            points[0] = new Vector3(0f, radius, 0f);

            for (var i = 0; i < segments; i++)
            {
                var angle = Mathf.PI * 2f * i / segments;
                points[i + 1] = new Vector3(Mathf.Cos(angle) * radius, -radius, Mathf.Sin(angle) * radius);
            }

            return points;
        }

        private static void BuildCylinderMesh(float halfHeight, float radius, int segments, out Vector3[] vertices, out int[] indices)
        {
            vertices = new Vector3[segments * 2 + 2];
            vertices[0] = new Vector3(0f, halfHeight, 0f);
            vertices[1] = new Vector3(0f, -halfHeight, 0f);

            for (var i = 0; i < segments; i++)
            {
                var angle = Mathf.PI * 2f * i / segments;
                var x = Mathf.Cos(angle) * radius;
                var z = Mathf.Sin(angle) * radius;
                vertices[i + 2] = new Vector3(x, halfHeight, z);
                vertices[i + segments + 2] = new Vector3(x, -halfHeight, z);
            }

            indices = new int[segments * 12];
            var t = 0;
            for (var i = 0; i < segments; i++)
            {
                var topCurrent = i + 2;
                var topNext = (i + 1) % segments + 2;
                var bottomCurrent = i + segments + 2;
                var bottomNext = (i + 1) % segments + segments + 2;

                indices[t++] = topCurrent;
                indices[t++] = bottomCurrent;
                indices[t++] = topNext;
                indices[t++] = topNext;
                indices[t++] = bottomCurrent;
                indices[t++] = bottomNext;
                indices[t++] = 0;
                indices[t++] = topNext;
                indices[t++] = topCurrent;
                indices[t++] = 1;
                indices[t++] = bottomCurrent;
                indices[t++] = bottomNext;
            }
        }

        private static void BuildConeMesh(float radius, int segments, out Vector3[] vertices, out int[] indices)
        {
            vertices = new Vector3[segments + 2];
            vertices[0] = new Vector3(0f, radius, 0f);
            vertices[1] = new Vector3(0f, -radius, 0f);

            for (var i = 0; i < segments; i++)
            {
                var angle = Mathf.PI * 2f * i / segments;
                vertices[i + 2] = new Vector3(Mathf.Cos(angle) * radius, -radius, Mathf.Sin(angle) * radius);
            }

            indices = new int[segments * 6];
            var t = 0;
            for (var i = 0; i < segments; i++)
            {
                var current = i + 2;
                var next = (i + 1) % segments + 2;
                indices[t++] = 0;
                indices[t++] = current;
                indices[t++] = next;
                indices[t++] = 1;
                indices[t++] = next;
                indices[t++] = current;
            }
        }

        private static void BuildPlatformMesh(out Vector3[] vertices, out int[] indices, int subdivisions, float width, float height, float depth)
        {
            var vertexCount = subdivisions + 1;
            vertices = new Vector3[vertexCount * vertexCount];
            var elementWidth = 1f / subdivisions;
            var randomState = 0x7472696du;

            for (var row = 0; row <= subdivisions; row++)
            {
                for (var column = 0; column <= subdivisions; column++)
                {
                    var x = (column * elementWidth - 0.5f) * width;
                    var y = NextPlatformRandom(ref randomState) * height;
                    var z = (row * elementWidth - 0.5f) * depth;
                    vertices[row * vertexCount + column] = new Vector3(x, y, z);
                }
            }

            indices = new int[subdivisions * subdivisions * 6];
            var t = 0;
            for (var row = 0; row < subdivisions; row++)
            {
                for (var column = 0; column < subdivisions; column++)
                {
                    var i1 = row * vertexCount + column;
                    var i2 = row * vertexCount + column + 1;
                    var i3 = (row + 1) * vertexCount + column;
                    var i4 = (row + 1) * vertexCount + column + 1;
                    indices[t++] = i1;
                    indices[t++] = i3;
                    indices[t++] = i2;
                    indices[t++] = i3;
                    indices[t++] = i4;
                    indices[t++] = i2;
                }
            }
        }

        private static float NextPlatformRandom(ref uint state)
        {
            state = state * 1664525u + 1013904223u;
            return (state & 0x00ffffff) / 16777216f;
        }

        private void BuildLockedRotations()
        {
            CreateWorld(new Vector3(0f, -9.81f, 0f));
            const float groundHeight = 0.1f;
            CreateBox("floor", RapierRigidBodyType.Fixed, new Vector3(0f, -groundHeight, 0f), Quaternion.identity, new Vector3(1.7f, groundHeight, 1.7f), 0f, ColorFor("floor"));

            var xRotor = CreateBox(
                "locked-x-rotor",
                RapierRigidBodyType.Dynamic,
                new Vector3(0f, 3f, 0f),
                Quaternion.identity,
                new Vector3(0.2f, 0.6f, 2f),
                1f,
                ColorFor("box"));
            world.SetEnabledTranslations(xRotor.Body, false, false, false);
            world.SetEnabledRotations(xRotor.Body, true, false, false);

            var lockedCylinder = CreateConvexCylinderBody("locked-cylinder", new Vector3(0.2f, 5f, 0.4f), 0.4f, ColorFor("capsule"), Vector3.zero, 0.6f);
            world.SetEnabledRotations(lockedCylinder.Body, false, false, false);

            LookAt(new Vector3(-10f, 3f, 0f), new Vector3(0f, 3f, 0f));
        }

        private void BuildConvexPolyhedron()
        {
            CreateWorld(new Vector3(0f, -9.81f, 0f));
            CreateBox("floor", RapierRigidBodyType.Fixed, new Vector3(0f, -0.1f, 0f), Quaternion.identity, new Vector3(20f, 0.1f, 20f), 0f, ColorFor("floor"));

            var points = IcosahedronPoints(0.7f);
            for (var i = 0; i < 12; i++)
            {
                var position = new Vector3(i % 4 * 1.5f - 2.25f, 3f + i / 4 * 1.6f, 0f);
                CreateConvexBody(NextId("convex"), position, points, ColorFor("sphere"), 0.7f);
            }

            LookAt(new Vector3(0f, 6f, 14f), new Vector3(0f, 3f, 0f));
        }

        private VisualBody CreateConvexBody(string id, Vector3 position, Vector3[] points, Color color, float visualRadius)
        {
            var body = CreateRigidBody(id, RapierRigidBodyType.Dynamic, position, Quaternion.identity, Vector3.zero, Vector3.zero, 0f, 0f, false);
            var collider = world.CreateConvexHullCollider(body, points, RapierMeshColliderDesc.Default);
            RegisterCollider(id, collider);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ConfigureVisual(visual, id, position, Quaternion.identity, Vector3.one * visualRadius * 2f, color);
            return TrackBody(id, body, visual, true);
        }

        private void BuildTriangleMesh()
        {
            CreateWorld(new Vector3(0f, -9.81f, 0f));
            BuildPlatformMesh(out var vertices, out var indices, 20, 70f, 4f, 70f);
            CreateTrimeshGround("trimesh-ground", vertices, indices, ColorFor("floor"));

            CreateStackedShapeGrid("trimesh-body");

            LookAt(new Vector3(-88.48024f, 46.91133f, 83.56055f), Vector3.zero);
        }

        private void CreateStackedShapeGrid(string idPrefix)
        {
            const int columns = 4;
            const int layers = 10;
            const float radius = 1f;
            var shift = radius * 3f;
            var centerY = shift * 0.5f;
            var offset = -columns * shift * 0.5f;

            for (var layer = 0; layer < layers; layer++)
            {
                for (var xIndex = 0; xIndex < columns; xIndex++)
                {
                    for (var zIndex = 0; zIndex < columns; zIndex++)
                    {
                        var position = new Vector3(
                            xIndex * shift + offset,
                            layer * shift + centerY + 3f,
                            zIndex * shift + offset);
                        CreatePlatformStackBody(NextId(idPrefix), layer % 5, position, radius);
                    }
                }

                offset -= 0.05f * radius * (columns - 1f);
            }
        }

        private void BuildHeightfield()
        {
            CreateWorld(new Vector3(0f, -9.81f, 0f));

            const int subdivisions = 20;
            var samples = subdivisions + 1;
            var heights = new float[samples * samples];
            var randomState = 0x68656967u;
            for (var i = 0; i < heights.Length; i++)
            {
                heights[i] = NextPlatformRandom(ref randomState);
            }

            var scale = new Vector3(70f, 4f, 70f);
            CreateHeightfieldGround("heightfield-ground", heights, samples, samples, scale, ColorFor("keva"));

            CreateStackedShapeGrid("heightfield-body");

            LookAt(new Vector3(-88.48024f, 46.91133f, 83.56055f), Vector3.zero);
        }

        private void BuildCharacterController()
        {
            CreateWorld(new Vector3(0f, -9.81f, 0f));
            CreateBox("floor", RapierRigidBodyType.Fixed, Vector3.zero, Quaternion.identity, new Vector3(15f, 0.1f, 15f), 0f, ColorFor("floor"));

            const float radius = 0.5f;
            const int count = 5;
            var shift = radius * 2.5f;
            var center = count * radius;
            const float height = 5f;

            for (var layer = 0; layer < count; layer++)
            {
                for (var row = layer; row < count; row++)
                {
                    for (var column = layer; column < count; column++)
                    {
                        var position = new Vector3(
                            layer * shift * 0.5f + (column - layer) * shift - center,
                            layer * shift * 0.5f + height,
                            layer * shift * 0.5f + (row - layer) * shift - center);
                        CreateBox(
                            NextId("character-block"),
                            RapierRigidBodyType.Dynamic,
                            position,
                            Quaternion.identity,
                            new Vector3(radius, radius * 0.5f, radius),
                            1f,
                            ColorFor("box"));
                    }
                }
            }

            characterBody = CreateCharacterControllerBody("character", new Vector3(-10f, 4f, -10f), 1.2f, 0.6f);
            characterShape = RapierQueryShape.Capsule(1.2f, 0.6f);
            characterMovementDirection = new Vector3(0f, -0.2f, 0f);

            LookAt(new Vector3(-40f, 19.73f, 0f), new Vector3(0f, -0.4126f, 0f));
        }

        private void PreStepCharacter()
        {
            if (characterBody == null || !characterBody.Body.IsValid)
            {
                return;
            }

            var dt = Mathf.Max(0.0001f, timestep);
            const float speed = 0.2f;
            characterMovementDirection = new Vector3(0f, Input.GetKey(KeyCode.Space) ? speed : -speed, 0f);

            if (Input.GetKey(KeyCode.UpArrow))
            {
                characterMovementDirection.x = speed;
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                characterMovementDirection.x = -speed;
            }

            if (Input.GetKey(KeyCode.LeftArrow))
            {
                characterMovementDirection.z = -speed;
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                characterMovementDirection.z = speed;
            }

            if (!world.TryGetTransform(characterBody.Body, out var current))
            {
                return;
            }

            var controller = RapierCharacterController.Default;
            controller.AutostepEnabled = true;
            controller.AutostepMaxHeight = 0.7f;
            controller.AutostepMinWidth = 0.3f;
            controller.AutostepIncludeDynamicBodies = true;
            controller.SnapToGroundEnabled = true;
            controller.SnapToGroundDistance = 0.7f;

            var filter = RapierQueryFilter.Default.ExcludingBody(characterBody.Body);
            if (world.MoveCharacter(characterShape, new RapierTransform(current.Position, current.Rotation), characterMovementDirection, dt, controller, filter, out var movement))
            {
                world.SetNextKinematicTranslation(characterBody.Body, current.Position + movement.Translation);
            }
        }

        private VisualBody CreateCharacterControllerBody(string id, Vector3 position, float halfHeight, float radius)
        {
            const int segments = 16;
            var body = CreateRigidBody(id, RapierRigidBodyType.KinematicPositionBased, position, Quaternion.identity, Vector3.zero, Vector3.zero, 0f, 0f, false);
            var collider = world.CreateConvexHullCollider(body, CylinderHullPoints(halfHeight, radius, segments), RapierMeshColliderDesc.Default);
            RegisterCollider(id, collider);

            BuildCylinderMesh(halfHeight, radius, segments, out var vertices, out var indices);
            var visual = CreateMeshVisual(id, vertices, indices, ColorFor("capsule"));
            visual.transform.SetPositionAndRotation(position, Quaternion.identity);
            return TrackBody(id, body, visual, true);
        }

        private void CreateTrimeshGround(string id, Vector3[] vertices, int[] indices, Color color)
        {
            var body = CreateRigidBody(id, RapierRigidBodyType.Fixed, Vector3.zero, Quaternion.identity, Vector3.zero, Vector3.zero, 0f, 0f, false);
            var collider = world.CreateTrimeshCollider(body, vertices, indices, RapierMeshColliderDesc.Default);
            RegisterCollider(id, collider);
            var visual = CreateMeshVisual(id, vertices, indices, color);
            TrackBody(id, body, visual, false);
        }

        private void CreateHeightfieldGround(string id, float[] heights, int rows, int columns, Vector3 scale, Color color)
        {
            var body = CreateRigidBody(id, RapierRigidBodyType.Fixed, Vector3.zero, Quaternion.identity, Vector3.zero, Vector3.zero, 0f, 0f, false);
            var collider = world.CreateHeightfieldCollider(body, heights, rows, columns, scale, RapierMeshColliderDesc.Default);
            RegisterCollider(id, collider);
            BuildHeightfieldMesh(heights, rows, columns, scale, out var vertices, out var indices);
            var visual = CreateMeshVisual(id, vertices, indices, color);
            TrackBody(id, body, visual, false);
        }

        private static void BuildHeightfieldMesh(float[] heights, int rows, int columns, Vector3 scale, out Vector3[] vertices, out int[] indices)
        {
            vertices = new Vector3[rows * columns];
            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < columns; c++)
                {
                    var x = ((float)c / (columns - 1) - 0.5f) * scale.x;
                    var z = ((float)r / (rows - 1) - 0.5f) * scale.z;
                    var y = heights[r * columns + c] * scale.y;
                    vertices[r * columns + c] = new Vector3(x, y, z);
                }
            }

            indices = BuildGridIndices(rows, columns);
        }

        private static int[] BuildGridIndices(int rows, int columns)
        {
            var indices = new int[(rows - 1) * (columns - 1) * 6];
            var t = 0;
            for (var r = 0; r < rows - 1; r++)
            {
                for (var c = 0; c < columns - 1; c++)
                {
                    var i0 = r * columns + c;
                    var i1 = r * columns + c + 1;
                    var i2 = (r + 1) * columns + c;
                    var i3 = (r + 1) * columns + c + 1;
                    indices[t++] = i0;
                    indices[t++] = i2;
                    indices[t++] = i1;
                    indices[t++] = i1;
                    indices[t++] = i2;
                    indices[t++] = i3;
                }
            }

            return indices;
        }

        private GameObject CreateMeshVisual(string id, Vector3[] vertices, int[] indices, Color color)
        {
            var go = new GameObject(id);
            go.transform.SetParent(generatedRoot.transform, false);

            var mesh = new Mesh { name = id + "-mesh" };
            if (vertices.Length > 65000)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            mesh.vertices = vertices;
            mesh.triangles = indices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.material = CreateMaterial(color);
            return go;
        }

        private static Material CreateMaterial(Color color)
        {
            var shader = Shader.Find("Standard")
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Sprites/Default");
            var material = new Material(shader);
            material.color = color;
            return material;
        }

        private static Vector3[] IcosahedronPoints(float radius)
        {
            var t = (1f + Mathf.Sqrt(5f)) / 2f;
            var points = new[]
            {
                new Vector3(-1f, t, 0f), new Vector3(1f, t, 0f), new Vector3(-1f, -t, 0f), new Vector3(1f, -t, 0f),
                new Vector3(0f, -1f, t), new Vector3(0f, 1f, t), new Vector3(0f, -1f, -t), new Vector3(0f, 1f, -t),
                new Vector3(t, 0f, -1f), new Vector3(t, 0f, 1f), new Vector3(-t, 0f, -1f), new Vector3(-t, 0f, 1f)
            };

            for (var i = 0; i < points.Length; i++)
            {
                points[i] = points[i].normalized * radius;
            }

            return points;
        }

        private void CreateWorld(Vector3 gravity)
        {
            world = RapierWorld.Create();
            world.SetGravity(gravity);
            world.SetTimestep(Mathf.Max(0.0001f, timestep));
        }

        private VisualBody CreateBox(
            string id,
            RapierRigidBodyType bodyType,
            Vector3 position,
            Quaternion rotation,
            Vector3 halfExtents,
            float density,
            Color color,
            Vector3 linearVelocity = default,
            Vector3 angularVelocity = default,
            float linearDamping = 0f,
            float angularDamping = 0f,
            bool ccd = false)
        {
            var body = CreateRigidBody(id, bodyType, position, rotation, linearVelocity, angularVelocity, linearDamping, angularDamping, ccd);
            var collider = world.CreateBoxCollider(
                body,
                new RapierBoxColliderDesc
                {
                    HalfExtents = halfExtents,
                    Density = density,
                    Friction = 0.5f,
                    HasFriction = true,
                    Restitution = 0f,
                    LocalRotation = Quaternion.identity
                });
            RegisterCollider(id, collider);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ConfigureVisual(visual, id, position, rotation, halfExtents * 2f, color);
            return TrackBody(id, body, visual, bodyType != RapierRigidBodyType.Fixed);
        }

        private VisualBody CreateSphere(
            string id,
            RapierRigidBodyType bodyType,
            Vector3 position,
            float radius,
            float density,
            Color color,
            Vector3 linearVelocity,
            Vector3 angularVelocity,
            float linearDamping,
            float angularDamping,
            bool ccd)
        {
            var body = CreateRigidBody(id, bodyType, position, Quaternion.identity, linearVelocity, angularVelocity, linearDamping, angularDamping, ccd);
            var collider = world.CreateSphereCollider(
                body,
                new RapierSphereColliderDesc
                {
                    Radius = radius,
                    Density = density,
                    Friction = 0.5f,
                    HasFriction = true,
                    Restitution = 0f,
                    LocalRotation = Quaternion.identity
                });
            RegisterCollider(id, collider);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ConfigureVisual(visual, id, position, Quaternion.identity, Vector3.one * radius * 2f, color);
            return TrackBody(id, body, visual, bodyType != RapierRigidBodyType.Fixed);
        }

        private VisualBody CreateCapsule(
            string id,
            RapierRigidBodyType bodyType,
            Vector3 position,
            float halfHeight,
            float radius,
            float density,
            Color color,
            Vector3 linearVelocity)
        {
            var body = CreateRigidBody(id, bodyType, position, Quaternion.identity, linearVelocity, Vector3.zero, 0f, 0f, false);
            var collider = world.CreateCapsuleCollider(
                body,
                new RapierCapsuleColliderDesc
                {
                    HalfHeight = halfHeight,
                    Radius = radius,
                    Density = density,
                    Friction = 0.5f,
                    HasFriction = true,
                    Restitution = 0f,
                    LocalRotation = Quaternion.identity
                });
            RegisterCollider(id, collider);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var totalHeight = halfHeight * 2f + radius * 2f;
            ConfigureVisual(visual, id, position, Quaternion.identity, new Vector3(radius * 2f, totalHeight / 2f, radius * 2f), color);
            return TrackBody(id, body, visual, bodyType != RapierRigidBodyType.Fixed);
        }

        private RapierRigidBodyHandle CreateRigidBody(
            string id,
            RapierRigidBodyType bodyType,
            Vector3 position,
            Quaternion rotation,
            Vector3 linearVelocity,
            Vector3 angularVelocity,
            float linearDamping,
            float angularDamping,
            bool ccd)
        {
            var body = world.CreateRigidBody(
                new RapierBodyDesc
                {
                    BodyType = bodyType,
                    Position = position,
                    Rotation = rotation == default(Quaternion) ? Quaternion.identity : rotation,
                    LinearVelocity = bodyType == RapierRigidBodyType.Dynamic ? linearVelocity : Vector3.zero,
                    AngularVelocity = bodyType == RapierRigidBodyType.Dynamic ? angularVelocity : Vector3.zero,
                    LinearDamping = Mathf.Max(0f, linearDamping),
                    AngularDamping = Mathf.Max(0f, angularDamping),
                    CanSleep = bodyType == RapierRigidBodyType.Dynamic,
                    CcdEnabled = bodyType == RapierRigidBodyType.Dynamic && ccd
                });

            if (!body.IsValid)
            {
                throw new InvalidOperationException($"Failed to create Rapier body '{id}'.");
            }

            var stableId = RapierWorld.StableIdHash(id);
            world.SetRigidBodyStableId(body, stableId);
            return body;
        }

        private void RegisterCollider(string id, RapierColliderHandle collider)
        {
            if (!collider.IsValid)
            {
                throw new InvalidOperationException($"Failed to create Rapier collider '{id}'.");
            }

            world.SetColliderStableId(collider, RapierWorld.StableIdHash(id));
            lastCollider = collider;
        }

        private VisualBody TrackBody(string id, RapierRigidBodyHandle body, GameObject visual, bool syncTransform)
        {
            var visualBody = new VisualBody
            {
                Id = id,
                Body = body,
                Visual = visual,
                SyncTransform = syncTransform
            };
            bodies.Add(visualBody);
            return visualBody;
        }

        private void ConfigureVisual(GameObject visual, string id, Vector3 position, Quaternion rotation, Vector3 scale, Color color)
        {
            visual.name = id;
            visual.transform.SetParent(generatedRoot.transform, false);
            visual.transform.SetPositionAndRotation(position, rotation);
            visual.transform.localScale = scale;
            RemoveUnityCollider(visual);
            SetColor(visual, color);
        }

        private void UpdateVisuals()
        {
            for (var i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                if (!body.SyncTransform || body.Visual == null || world == null)
                {
                    continue;
                }

                if (world.TryGetTransform(body.Body, out var transform))
                {
                    body.Visual.transform.SetPositionAndRotation(transform.Position, transform.Rotation);
                }
            }
        }

        private void RemoveBody(VisualBody body)
        {
            if (body == null)
            {
                return;
            }

            if (world != null && body.Body.IsValid)
            {
                world.DestroyRigidBody(body.Body);
            }

            bodies.Remove(body);
            if (body.Visual != null)
            {
                Destroy(body.Visual);
            }
        }

        private string NextId(string prefix)
        {
            nextBodyId++;
            return $"{prefix}-{nextBodyId:0000}";
        }

        private static int IndexOf(DemoKind value)
        {
            for (var i = 0; i < DemoValues.Length; i++)
            {
                if (DemoValues[i] == value)
                {
                    return i;
                }
            }

            return 0;
        }

        private static string UnsupportedReason(DemoKind value)
        {
            return $"the '{value}' demo is not wired up";
        }

        private static Color ColorFor(string role)
        {
            switch (role)
            {
                case "floor":
                    return new Color(0.28f, 0.32f, 0.36f);
                case "sphere":
                    return new Color(0.16f, 0.52f, 0.92f);
                case "capsule":
                    return new Color(0.37f, 0.73f, 0.38f);
                case "keva":
                    return new Color(0.86f, 0.63f, 0.35f);
                default:
                    return new Color(0.95f, 0.55f, 0.18f);
            }
        }

        private void EnsureCameraAndLight()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Rapier Demo Camera");
                cameraObject.transform.SetParent(generatedRoot.transform, false);
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            if (UnityEngine.Object.FindObjectOfType<Light>() == null)
            {
                var lightObject = new GameObject("Rapier Demo Light");
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

        private void DisposeWorld()
        {
            if (world != null)
            {
                world.Dispose();
                world = null;
            }
        }

        private void DestroyGeneratedRoot()
        {
            if (generatedRoot != null)
            {
                Destroy(generatedRoot);
                generatedRoot = null;
            }
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
            var renderer = gameObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }
    }
}
