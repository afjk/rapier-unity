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
        [SerializeField] private int maxFountainBodies = 240;

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
        private RapierColliderHandle lastCollider = RapierColliderHandle.Invalid;

        // Platform demo state.
        private VisualBody platformBody;
        private float platformPhase;

        // Character controller demo state.
        private VisualBody characterBody;
        private RapierQueryShape characterShape;
        private Vector3 characterVelocity;

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
            if (tick % 5 != 0)
            {
                return;
            }

            var shapeIndex = (tick / 5) % 3;
            VisualBody body;
            switch (shapeIndex)
            {
                case 0:
                    body = CreateBox(NextId("fountain-box"), RapierRigidBodyType.Dynamic, new Vector3(0f, 10f, 0f), Quaternion.identity, Vector3.one, 1f, ColorFor("box"), new Vector3(0f, 15f, 0f), Vector3.zero, 0f, 0f);
                    break;
                case 1:
                    body = CreateSphere(NextId("fountain-sphere"), RapierRigidBodyType.Dynamic, new Vector3(0f, 10f, 0f), 1f, 1f, ColorFor("sphere"), new Vector3(0f, 15f, 0f), Vector3.zero, 0f, 0f, false);
                    break;
                default:
                    body = CreateCapsule(NextId("fountain-capsule"), RapierRigidBodyType.Dynamic, new Vector3(0f, 10f, 0f), 1f, 0.4f, 1f, ColorFor("capsule"), new Vector3(0f, 15f, 0f));
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
            CreateBox("floor", RapierRigidBodyType.Fixed, new Vector3(0f, -0.1f, 0f), Quaternion.identity, new Vector3(20f, 0.1f, 20f), 0f, ColorFor("floor"));
            // Floor is a member of both groups so either set of boxes can land on it.
            world.SetColliderCollisionGroups(lastCollider, RapierWorld.InteractionGroups(0x0003, 0x0003));

            for (var i = 0; i < 8; i++)
            {
                var groupBit = i % 2 == 0 ? (ushort)0x0001 : (ushort)0x0002;
                var color = i % 2 == 0 ? new Color(0.9f, 0.4f, 0.3f) : new Color(0.3f, 0.5f, 0.9f);
                var x = i % 2 == 0 ? -0.2f : 0.2f;
                CreateBox(NextId("group-box"), RapierRigidBodyType.Dynamic, new Vector3(x, 2f + i * 1.2f, 0f), Quaternion.identity, Vector3.one * 0.5f, 1f, color);
                // Each box only collides with members of its own group (plus the floor).
                world.SetColliderCollisionGroups(lastCollider, RapierWorld.InteractionGroups(groupBit, groupBit));
            }

            LookAt(new Vector3(0f, 6f, 16f), new Vector3(0f, 4f, 0f));
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
            CreateBox("floor", RapierRigidBodyType.Fixed, new Vector3(0f, -0.1f, 0f), Quaternion.identity, new Vector3(20f, 0.1f, 20f), 0f, ColorFor("floor"));
            platformBody = CreateBox("platform", RapierRigidBodyType.KinematicPositionBased, new Vector3(0f, 1f, 0f), Quaternion.identity, new Vector3(3f, 0.25f, 3f), 0f, ColorFor("keva"));
            platformPhase = 0f;

            for (var i = 0; i < 5; i++)
            {
                CreateBox(NextId("platform-box"), RapierRigidBodyType.Dynamic, new Vector3(0f, 3f + i * 1.1f, 0f), Quaternion.identity, Vector3.one * 0.4f, 1f, ColorFor("box"));
            }

            LookAt(new Vector3(0f, 6f, 16f), new Vector3(0f, 2f, 0f));
        }

        private void PreStepPlatform()
        {
            if (platformBody == null || !platformBody.Body.IsValid)
            {
                return;
            }

            platformPhase += Mathf.Max(0.0001f, timestep);
            var x = Mathf.Sin(platformPhase) * 4f;
            world.SetNextKinematicTranslation(platformBody.Body, new Vector3(x, 1f, 0f));
        }

        private void BuildLockedRotations()
        {
            CreateWorld(new Vector3(0f, -9.81f, 0f));
            CreateBox("floor", RapierRigidBodyType.Fixed, new Vector3(0f, -0.1f, 0f), Quaternion.identity, new Vector3(20f, 0.1f, 20f), 0f, ColorFor("floor"));

            for (var i = 0; i < 6; i++)
            {
                var body = CreateBox(NextId("locked-box"), RapierRigidBodyType.Dynamic, new Vector3(i * 1.5f - 4f, 5f, 0f), Quaternion.Euler(0f, 0f, 25f), new Vector3(0.3f, 1.2f, 0.3f), 1f, ColorFor("box"));
                // Locking rotations keeps the tall boxes upright as they settle.
                world.SetEnabledRotations(body.Body, false, false, false);
            }

            LookAt(new Vector3(0f, 5f, 16f), new Vector3(0f, 2f, 0f));
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
            BuildGridSurface(out var vertices, out var indices, 16, 1.4f, 1.2f);
            CreateTrimeshGround("trimesh-ground", vertices, indices, ColorFor("floor"));

            for (var i = 0; i < 12; i++)
            {
                CreateBox(NextId("trimesh-box"), RapierRigidBodyType.Dynamic, new Vector3(i % 4 * 2f - 3f, 8f + i / 4 * 1.4f, i / 4 * 1.5f - 1.5f), Quaternion.identity, Vector3.one * 0.5f, 1f, ColorFor("box"));
            }

            LookAt(new Vector3(0f, 12f, 20f), new Vector3(0f, 2f, 0f));
        }

        private void BuildHeightfield()
        {
            CreateWorld(new Vector3(0f, -9.81f, 0f));

            const int n = 16;
            var heights = new float[n * n];
            for (var r = 0; r < n; r++)
            {
                for (var c = 0; c < n; c++)
                {
                    heights[r * n + c] = Mathf.Sin(r * 0.6f) * Mathf.Cos(c * 0.6f) * 0.5f;
                }
            }

            var scale = new Vector3(20f, 4f, 20f);
            CreateHeightfieldGround("heightfield-ground", heights, n, n, scale, ColorFor("keva"));

            for (var i = 0; i < 12; i++)
            {
                CreateBox(NextId("hf-box"), RapierRigidBodyType.Dynamic, new Vector3(i % 4 * 2f - 3f, 8f + i / 4 * 1.4f, 0f), Quaternion.identity, Vector3.one * 0.5f, 1f, ColorFor("box"));
            }

            LookAt(new Vector3(0f, 14f, 22f), Vector3.zero);
        }

        private void BuildCharacterController()
        {
            CreateWorld(new Vector3(0f, -9.81f, 0f));
            CreateBox("floor", RapierRigidBodyType.Fixed, new Vector3(0f, -0.1f, 0f), Quaternion.identity, new Vector3(20f, 0.1f, 20f), 0f, ColorFor("floor"));

            for (var i = 0; i < 4; i++)
            {
                CreateBox(NextId("step"), RapierRigidBodyType.Fixed, new Vector3(2f + i * 1.5f, 0.25f + i * 0.25f, 0f), Quaternion.identity, new Vector3(0.75f, 0.25f + i * 0.25f, 2f), 0f, ColorFor("keva"));
            }

            characterBody = CreateCapsule("character", RapierRigidBodyType.KinematicPositionBased, new Vector3(-4f, 2f, 0f), 0.5f, 0.4f, 1f, ColorFor("capsule"), Vector3.zero);
            characterShape = RapierQueryShape.Capsule(0.5f, 0.4f);
            characterVelocity = Vector3.zero;

            LookAt(new Vector3(0f, 6f, 14f), new Vector3(2f, 1f, 0f));
        }

        private void PreStepCharacter()
        {
            if (characterBody == null || !characterBody.Body.IsValid)
            {
                return;
            }

            var dt = Mathf.Max(0.0001f, timestep);
            characterVelocity.y -= 9.81f * dt;

            if (!world.TryGetTransform(characterBody.Body, out var current))
            {
                return;
            }

            var desired = new Vector3(1.5f * dt, characterVelocity.y * dt, 0f);
            var controller = RapierCharacterController.Default;
            controller.AutostepEnabled = true;
            controller.AutostepMaxHeight = 0.4f;
            controller.AutostepMinWidth = 0.2f;
            controller.SnapToGroundEnabled = true;
            controller.SnapToGroundDistance = 0.3f;

            if (world.MoveCharacter(characterShape, new RapierTransform(current.Position, current.Rotation), desired, dt, controller, RapierQueryFilter.Default, out var movement))
            {
                world.SetNextKinematicTranslation(characterBody.Body, current.Position + movement.Translation);
                if (movement.Grounded)
                {
                    characterVelocity.y = 0f;
                }

                if (current.Position.x > 8f)
                {
                    world.SetTransform(characterBody.Body, new RapierTransform(new Vector3(-4f, 2f, 0f), Quaternion.identity));
                    characterVelocity = Vector3.zero;
                }
            }
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

        private static void BuildGridSurface(out Vector3[] vertices, out int[] indices, int n, float extent, float amplitude)
        {
            vertices = new Vector3[n * n];
            var half = extent * (n - 1) / 2f;
            for (var r = 0; r < n; r++)
            {
                for (var c = 0; c < n; c++)
                {
                    var x = c * extent - half;
                    var z = r * extent - half;
                    var y = Mathf.Sin(c * 0.5f) * Mathf.Cos(r * 0.5f) * amplitude;
                    vertices[r * n + c] = new Vector3(x, y, z);
                }
            }

            indices = BuildGridIndices(n, n);
        }

        private static void BuildHeightfieldMesh(float[] heights, int rows, int columns, Vector3 scale, out Vector3[] vertices, out int[] indices)
        {
            vertices = new Vector3[rows * columns];
            for (var r = 0; r < rows; r++)
            {
                for (var c = 0; c < columns; c++)
                {
                    var x = ((float)r / (rows - 1) - 0.5f) * scale.x;
                    var z = ((float)c / (columns - 1) - 0.5f) * scale.z;
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
