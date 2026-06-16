using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace AFJK.Rapier.Samples
{
    public sealed class CrossHostParityRunner : MonoBehaviour
    {
        private const string FixtureRelativePath = "fixtures/rapier/parity-basic-001.json";
        private const string DefaultFixtureName = "parity-basic-001.json";
        private const string HashVersion = "SceneSyncCanonicalPhysicsHashV1";
        private const string RapierCoreVersion = "0.30.0";

        [SerializeField] private TextAsset fixtureJson;
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool writeResultFile;
        [SerializeField] private string resultFileName = "rapier-parity-unity-result.json";

        private string lastResultJson = "Not run.";
        private string lastStatus = "Not run.";

        private sealed class RuntimeBody
        {
            public ParityBody Definition;
            public RapierRigidBodyHandle Body;
            public RapierColliderHandle Collider;
            public ulong StableId;
        }

        [Serializable]
        private sealed class ParityFixture
        {
            public string profile;
            public string rapierCoreVersion;
            public float timestep;
            public float[] gravity;
            public ParityBody[] bodies;
            public int[] sampleTicks;
        }

        [Serializable]
        private sealed class ParityBody
        {
            public string id;
            public string type;
            public string shape;
            public float[] position;
            public float[] rotation;
            public float[] halfExtents;
            public float density = 1f;
            public float[] linearVelocity;
            public float[] angularVelocity;
            public float linearDamping;
            public float angularDamping;
            public bool canSleep = true;
            public bool ccd;
            public float friction = 0.5f;
            public float restitution = 0.2f;
            public int frictionCombineRule;
            public int restitutionCombineRule;
        }

        private void Start()
        {
            if (runOnStart)
            {
                RunFixture();
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12f, 12f, 620f, 190f), GUI.skin.window);
            GUILayout.Label("Rapier Cross-Host Parity");
            GUILayout.Label(lastStatus);

            if (GUILayout.Button("Run Fixture"))
            {
                RunFixture();
            }

            if (GUILayout.Button("Log Last Result JSON"))
            {
                Debug.Log(lastResultJson, this);
            }

            GUILayout.EndArea();
        }

        public void RunFixture()
        {
            try
            {
                var fixtureSource = ResolveFixtureJson();
                var fixture = JsonUtility.FromJson<ParityFixture>(fixtureSource.Json);
                lastResultJson = RunFixture(fixture, fixtureSource.Name);
                lastStatus = "Cross-host parity fixture completed. Result JSON was logged.";
                Debug.Log(lastResultJson, this);

                if (writeResultFile)
                {
                    var path = Path.Combine(Application.persistentDataPath, string.IsNullOrWhiteSpace(resultFileName)
                        ? "rapier-parity-unity-result.json"
                        : resultFileName);
                    File.WriteAllText(path, lastResultJson, Encoding.UTF8);
                    Debug.Log($"Wrote Rapier parity result to {path}", this);
                }
            }
            catch (Exception ex) when (IsNativeFailure(ex))
            {
                lastStatus = "Rapier native plugin is not available. Build and copy rapier_unity_ffi first.";
                Debug.LogWarning($"{lastStatus}\n{ex}", this);
            }
            catch (Exception ex)
            {
                lastStatus = $"Cross-host parity fixture failed: {ex.Message}";
                Debug.LogError(ex, this);
            }
        }

        private static string RunFixture(ParityFixture fixture, string fixtureName)
        {
            if (fixture == null)
            {
                throw new InvalidOperationException("Parity fixture could not be parsed.");
            }

            if (!string.Equals(fixture.rapierCoreVersion, RapierCoreVersion, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Fixture Rapier core {fixture.rapierCoreVersion} does not match Unity core {RapierCoreVersion}.");
            }

            ValidateFixture(fixture);

            using var world = RapierWorld.Create();
            world.SetGravity(ToVector3(fixture.gravity));
            world.SetTimestep(fixture.timestep);

            var runtimeBodies = CreateBodies(world, fixture);
            var sampleTicks = SortedSampleTicks(fixture.sampleTicks);
            var hashes = new SortedDictionary<int, string>();
            var dumps = new SortedDictionary<int, string>();
            var currentTick = 0;

            foreach (var targetTick in sampleTicks)
            {
                while (currentTick < targetTick)
                {
                    if (!world.Step())
                    {
                        throw new InvalidOperationException($"Rapier step failed at tick {currentTick}.");
                    }

                    currentTick++;
                }

                hashes[targetTick] = HashHex(world.StateHash());
                dumps[targetTick] = BuildDumpJson(world, fixture, runtimeBodies, targetTick);
            }

            return BuildResultJson(fixture, fixtureName, sampleTicks, hashes, dumps);
        }

        private static List<RuntimeBody> CreateBodies(RapierWorld world, ParityFixture fixture)
        {
            var bodies = fixture.bodies;
            var runtimeBodies = new List<RuntimeBody>(bodies.Length);
            foreach (var bodyDef in bodies)
            {
                var fixedBody = string.Equals(bodyDef.type, "fixed", StringComparison.Ordinal);
                var body = world.CreateRigidBody(new RapierBodyDesc
                {
                    BodyType = fixedBody ? RapierRigidBodyType.Fixed : RapierRigidBodyType.Dynamic,
                    Position = ToVector3(bodyDef.position, Vector3.zero),
                    Rotation = ToQuaternion(bodyDef.rotation, Quaternion.identity),
                    LinearVelocity = fixedBody ? Vector3.zero : ToVector3(bodyDef.linearVelocity, Vector3.zero),
                    AngularVelocity = fixedBody ? Vector3.zero : ToVector3(bodyDef.angularVelocity, Vector3.zero),
                    LinearDamping = Mathf.Max(0f, bodyDef.linearDamping),
                    AngularDamping = Mathf.Max(0f, bodyDef.angularDamping),
                    CanSleep = bodyDef.canSleep,
                    CcdEnabled = !fixedBody && bodyDef.ccd
                });

                if (!body.IsValid)
                {
                    throw new InvalidOperationException($"Failed to create rigid body '{bodyDef.id}'.");
                }

                var stableId = RapierWorld.StableIdHash(bodyDef.id);
                if (!world.SetRigidBodyStableId(body, stableId))
                {
                    throw new InvalidOperationException($"Failed to set rigid body stable id for '{bodyDef.id}'.");
                }

                var collider = CreateCollider(world, body, bodyDef);
                if (!collider.IsValid)
                {
                    throw new InvalidOperationException($"Failed to create collider for '{bodyDef.id}'.");
                }

                if (!world.SetColliderStableId(collider, stableId))
                {
                    throw new InvalidOperationException($"Failed to set collider stable id for '{bodyDef.id}'.");
                }

                runtimeBodies.Add(new RuntimeBody
                {
                    Definition = bodyDef,
                    Body = body,
                    Collider = collider,
                    StableId = stableId
                });
            }

            runtimeBodies.Sort((left, right) =>
            {
                var stableCompare = left.StableId.CompareTo(right.StableId);
                return stableCompare != 0 ? stableCompare : string.CompareOrdinal(left.Definition.id, right.Definition.id);
            });
            return runtimeBodies;
        }

        private static void ValidateFixture(ParityFixture fixture)
        {
            if (string.IsNullOrWhiteSpace(fixture.profile))
            {
                throw new InvalidOperationException("Parity fixture is missing profile.");
            }

            if (!IsFinitePositive(fixture.timestep))
            {
                throw new InvalidOperationException("Parity fixture timestep must be finite and positive.");
            }

            RequireVec3(fixture.gravity, "gravity");

            if (fixture.bodies == null || fixture.bodies.Length == 0)
            {
                throw new InvalidOperationException("Parity fixture must contain at least one body.");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < fixture.bodies.Length; i++)
            {
                var body = fixture.bodies[i];
                if (body == null)
                {
                    throw new InvalidOperationException($"Parity fixture body[{i}] is null.");
                }

                if (string.IsNullOrWhiteSpace(body.id))
                {
                    throw new InvalidOperationException($"Parity fixture body[{i}] is missing id.");
                }

                if (!ids.Add(body.id))
                {
                    throw new InvalidOperationException($"Parity fixture contains duplicate body id '{body.id}'.");
                }

                if (!string.Equals(body.type, "fixed", StringComparison.Ordinal) &&
                    !string.Equals(body.type, "dynamic", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Parity fixture body '{body.id}' has unsupported type '{body.type}'.");
                }

                if (!string.Equals(body.shape, "box", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"CrossHostParity v0 only supports box fixtures. Got '{body.shape}' for '{body.id}'.");
                }

                RequireVec3(body.position, $"body '{body.id}' position");
                RequireQuat(body.rotation, $"body '{body.id}' rotation");
                RequirePositiveVec3(body.halfExtents, $"body '{body.id}' halfExtents");
                RequireNonNegative(body.density, $"body '{body.id}' density");
                RequireNonNegative(body.friction, $"body '{body.id}' friction");
                RequireNonNegative(body.restitution, $"body '{body.id}' restitution");
                RequireNonNegative(body.linearDamping, $"body '{body.id}' linearDamping");
                RequireNonNegative(body.angularDamping, $"body '{body.id}' angularDamping");
                RequireCombineRuleZero(body.frictionCombineRule, $"body '{body.id}' frictionCombineRule");
                RequireCombineRuleZero(body.restitutionCombineRule, $"body '{body.id}' restitutionCombineRule");

                if (string.Equals(body.type, "dynamic", StringComparison.Ordinal))
                {
                    if (body.density <= 0f)
                    {
                        throw new InvalidOperationException($"Parity fixture dynamic body '{body.id}' density must be positive.");
                    }

                    RequireVec3(body.linearVelocity, $"body '{body.id}' linearVelocity");
                    RequireVec3(body.angularVelocity, $"body '{body.id}' angularVelocity");
                }
                else
                {
                    if (body.ccd)
                    {
                        throw new InvalidOperationException($"Parity fixture fixed body '{body.id}' cannot enable CCD.");
                    }

                    RequireOptionalZeroVec3(body.linearVelocity, $"body '{body.id}' linearVelocity");
                    RequireOptionalZeroVec3(body.angularVelocity, $"body '{body.id}' angularVelocity");
                }
            }

            if (fixture.sampleTicks == null || fixture.sampleTicks.Length == 0)
            {
                throw new InvalidOperationException("Parity fixture must contain sampleTicks.");
            }

            foreach (var tick in fixture.sampleTicks)
            {
                if (tick < 0)
                {
                    throw new InvalidOperationException("Parity fixture sampleTicks must be non-negative.");
                }
            }
        }

        private static RapierColliderHandle CreateCollider(
            RapierWorld world,
            RapierRigidBodyHandle body,
            ParityBody bodyDef)
        {
            if (!string.Equals(bodyDef.shape, "box", StringComparison.Ordinal))
            {
                throw new NotSupportedException($"CrossHostParity v0 only supports box fixtures. Got '{bodyDef.shape}'.");
            }

            return world.CreateBoxCollider(
                body,
                new RapierBoxColliderDesc
                {
                    HalfExtents = ToVector3(bodyDef.halfExtents, Vector3.one * 0.5f),
                    Density = Mathf.Max(0f, bodyDef.density),
                    Friction = Mathf.Max(0f, bodyDef.friction),
                    HasFriction = true,
                    Restitution = Mathf.Max(0f, bodyDef.restitution),
                    LocalRotation = Quaternion.identity
                });
        }

        private sealed class FixtureSource
        {
            public string Name;
            public string Json;
        }

        private FixtureSource ResolveFixtureJson()
        {
            if (fixtureJson != null)
            {
                return new FixtureSource
                {
                    Name = FixtureNameFromTextAsset(fixtureJson),
                    Json = fixtureJson.text
                };
            }

            var current = new DirectoryInfo(Application.dataPath);
            for (var depth = 0; current != null && depth < 8; depth++, current = current.Parent)
            {
                var candidate = Path.Combine(current.FullName, FixtureRelativePath);
                if (File.Exists(candidate))
                {
                    return new FixtureSource
                    {
                        Name = FixtureRelativePath,
                        Json = File.ReadAllText(candidate, Encoding.UTF8)
                    };
                }
            }

            foreach (var candidate in Directory.GetFiles(Application.dataPath, DefaultFixtureName, SearchOption.AllDirectories))
            {
                var normalized = candidate.Replace('\\', '/');
                if (normalized.EndsWith(FixtureRelativePath, StringComparison.Ordinal))
                {
                    return new FixtureSource
                    {
                        Name = FixtureRelativePath,
                        Json = File.ReadAllText(candidate, Encoding.UTF8)
                    };
                }
            }

            throw new FileNotFoundException($"Could not find {FixtureRelativePath}. Assign a TextAsset or run from the rapier-unity repository.");
        }

        private static int[] SortedSampleTicks(int[] ticks)
        {
            var values = new SortedSet<int>();
            if (ticks != null)
            {
                foreach (var tick in ticks)
                {
                    if (tick >= 0)
                    {
                        values.Add(tick);
                    }
                }
            }

            if (values.Count == 0)
            {
                values.Add(0);
            }

            var result = new int[values.Count];
            values.CopyTo(result);
            return result;
        }

        private static string BuildResultJson(
            ParityFixture fixture,
            string fixtureName,
            int[] sampleTicks,
            SortedDictionary<int, string> hashes,
            SortedDictionary<int, string> dumps)
        {
            var sb = new StringBuilder(8192);
            sb.AppendLine("{");
            WriteProperty(sb, 1, "host", "unity", true);
            WriteProperty(sb, 1, "profile", fixture.profile, true);
            WriteProperty(sb, 1, "rapierCoreVersion", RapierCoreVersion, true);
            WriteProperty(sb, 1, "buildFlavor", "enhanced-determinism", true);
            WriteProperty(sb, 1, "hashVersion", HashVersion, true);
            WriteProperty(sb, 1, "fixture", string.IsNullOrWhiteSpace(fixtureName) ? FixtureRelativePath : fixtureName, true);
            Indent(sb, 1).Append("\"sampleTicks\": [");
            for (var i = 0; i < sampleTicks.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(sampleTicks[i]);
            }
            sb.AppendLine("],");

            Indent(sb, 1).AppendLine("\"hashes\": {");
            WriteStringMap(sb, hashes, 2);
            Indent(sb, 1).AppendLine("},");

            Indent(sb, 1).AppendLine("\"dumps\": {");
            var index = 0;
            foreach (var dump in dumps)
            {
                Indent(sb, 2);
                AppendQuoted(sb, dump.Key.ToString(CultureInfo.InvariantCulture));
                sb.Append(": ");
                sb.Append(dump.Value);
                sb.AppendLine(index < dumps.Count - 1 ? "," : string.Empty);
                index++;
            }
            Indent(sb, 1).AppendLine("}");
            sb.Append("}");
            return sb.ToString();
        }

        private static string BuildDumpJson(
            RapierWorld world,
            ParityFixture fixture,
            List<RuntimeBody> runtimeBodies,
            int tick)
        {
            var sb = new StringBuilder(4096);
            sb.AppendLine("{");
            WriteProperty(sb, 1, "hashVersion", HashVersion, true);
            WriteProperty(sb, 1, "rapierCoreVersion", RapierCoreVersion, true);
            WriteProperty(sb, 1, "tick", tick, true);
            WriteVectorProperty(sb, 1, "gravity", ToVector3(fixture.gravity, new Vector3(0f, -9.81f, 0f)), true);
            WriteProperty(sb, 1, "timestep", fixture.timestep, true);

            Indent(sb, 1).AppendLine("\"bodies\": [");
            for (var i = 0; i < runtimeBodies.Count; i++)
            {
                WriteBodyDump(sb, 2, world, runtimeBodies[i]);
                sb.AppendLine(i < runtimeBodies.Count - 1 ? "," : string.Empty);
            }
            Indent(sb, 1).AppendLine("],");

            Indent(sb, 1).AppendLine("\"colliders\": [");
            for (var i = 0; i < runtimeBodies.Count; i++)
            {
                WriteColliderDump(sb, 2, runtimeBodies[i]);
                sb.AppendLine(i < runtimeBodies.Count - 1 ? "," : string.Empty);
            }
            Indent(sb, 1).Append("]");
            sb.AppendLine();
            sb.Append("  }");
            return sb.ToString();
        }

        private static void WriteBodyDump(StringBuilder sb, int depth, RapierWorld world, RuntimeBody body)
        {
            if (!world.TryGetRigidBodyState(body.Body, out var state))
            {
                throw new InvalidOperationException($"Could not read body state for '{body.Definition.id}'.");
            }

            Indent(sb, depth).AppendLine("{");
            WriteProperty(sb, depth + 1, "id", body.Definition.id, true);
            WriteProperty(sb, depth + 1, "idHash", HashHex(body.StableId), true);
            WriteProperty(sb, depth + 1, "type", body.Definition.type, true);
            WriteVectorProperty(sb, depth + 1, "position", state.Transform.Position, true);
            WriteQuaternionProperty(sb, depth + 1, "rotation", state.Transform.Rotation, true);
            WriteVectorProperty(sb, depth + 1, "linvel", state.LinearVelocity, true);
            WriteVectorProperty(sb, depth + 1, "angvel", state.AngularVelocity, true);
            WriteProperty(sb, depth + 1, "linearDamping", Mathf.Max(0f, body.Definition.linearDamping), true);
            WriteProperty(sb, depth + 1, "angularDamping", Mathf.Max(0f, body.Definition.angularDamping), true);
            WriteProperty(sb, depth + 1, "additionalSolverIterations", 0, true);
            WriteProperty(sb, depth + 1, "canSleep", body.Definition.canSleep, true);
            WriteProperty(sb, depth + 1, "ccd", body.Definition.ccd, true);
            WriteProperty(sb, depth + 1, "sleeping", state.Sleeping, true);
            WriteProperty(sb, depth + 1, "enabled", state.Enabled, false);
            Indent(sb, depth).Append("}");
        }

        private static void WriteColliderDump(StringBuilder sb, int depth, RuntimeBody body)
        {
            Indent(sb, depth).AppendLine("{");
            WriteProperty(sb, depth + 1, "id", body.Definition.id, true);
            WriteProperty(sb, depth + 1, "idHash", HashHex(body.StableId), true);
            WriteProperty(sb, depth + 1, "parentBodyId", body.Definition.id, true);
            WriteProperty(sb, depth + 1, "shape", body.Definition.shape, true);
            WriteVectorProperty(sb, depth + 1, "localPosition", Vector3.zero, true);
            WriteQuaternionProperty(sb, depth + 1, "localRotation", Quaternion.identity, true);
            WriteVectorProperty(sb, depth + 1, "halfExtents", ToVector3(body.Definition.halfExtents, Vector3.one * 0.5f), true);
            WriteProperty(sb, depth + 1, "density", Mathf.Max(0f, body.Definition.density), true);
            WriteProperty(sb, depth + 1, "friction", Mathf.Max(0f, body.Definition.friction), true);
            WriteProperty(sb, depth + 1, "frictionCombineRule", 0, true);
            WriteProperty(sb, depth + 1, "restitution", Mathf.Max(0f, body.Definition.restitution), true);
            WriteProperty(sb, depth + 1, "restitutionCombineRule", 0, true);
            WriteProperty(sb, depth + 1, "sensor", false, true);
            WriteProperty(sb, depth + 1, "enabled", true, false);
            Indent(sb, depth).Append("}");
        }

        private static void WriteStringMap(StringBuilder sb, SortedDictionary<int, string> values, int depth)
        {
            var index = 0;
            foreach (var entry in values)
            {
                Indent(sb, depth);
                AppendQuoted(sb, entry.Key.ToString(CultureInfo.InvariantCulture));
                sb.Append(": ");
                AppendQuoted(sb, entry.Value);
                sb.AppendLine(index < values.Count - 1 ? "," : string.Empty);
                index++;
            }
        }

        private static void WriteProperty(StringBuilder sb, int depth, string name, string value, bool comma)
        {
            Indent(sb, depth);
            AppendQuoted(sb, name);
            sb.Append(": ");
            AppendQuoted(sb, value ?? string.Empty);
            sb.AppendLine(comma ? "," : string.Empty);
        }

        private static void WriteProperty(StringBuilder sb, int depth, string name, int value, bool comma)
        {
            Indent(sb, depth);
            AppendQuoted(sb, name);
            sb.Append(": ");
            sb.Append(value.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine(comma ? "," : string.Empty);
        }

        private static void WriteProperty(StringBuilder sb, int depth, string name, float value, bool comma)
        {
            Indent(sb, depth);
            AppendQuoted(sb, name);
            sb.Append(": ");
            sb.Append(value.ToString("R", CultureInfo.InvariantCulture));
            sb.AppendLine(comma ? "," : string.Empty);
        }

        private static void WriteProperty(StringBuilder sb, int depth, string name, bool value, bool comma)
        {
            Indent(sb, depth);
            AppendQuoted(sb, name);
            sb.Append(": ");
            sb.Append(value ? "true" : "false");
            sb.AppendLine(comma ? "," : string.Empty);
        }

        private static void WriteVectorProperty(StringBuilder sb, int depth, string name, Vector3 value, bool comma)
        {
            Indent(sb, depth);
            AppendQuoted(sb, name);
            sb.Append(": [");
            AppendFloat(sb, value.x);
            sb.Append(", ");
            AppendFloat(sb, value.y);
            sb.Append(", ");
            AppendFloat(sb, value.z);
            sb.AppendLine(comma ? "]," : "]");
        }

        private static void WriteQuaternionProperty(StringBuilder sb, int depth, string name, Quaternion value, bool comma)
        {
            Indent(sb, depth);
            AppendQuoted(sb, name);
            sb.Append(": [");
            AppendFloat(sb, value.x);
            sb.Append(", ");
            AppendFloat(sb, value.y);
            sb.Append(", ");
            AppendFloat(sb, value.z);
            sb.Append(", ");
            AppendFloat(sb, value.w);
            sb.AppendLine(comma ? "]," : "]");
        }

        private static Vector3 ToVector3(float[] values, Vector3 fallback)
        {
            if (values == null || values.Length < 3)
            {
                return fallback;
            }

            return new Vector3(values[0], values[1], values[2]);
        }

        private static Vector3 ToVector3(float[] values)
        {
            return new Vector3(values[0], values[1], values[2]);
        }

        private static Quaternion ToQuaternion(float[] values, Quaternion fallback)
        {
            if (values == null || values.Length < 4)
            {
                return fallback;
            }

            var q = new Quaternion(values[0], values[1], values[2], values[3]);
            var length = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            return length > Mathf.Epsilon
                ? new Quaternion(q.x / length, q.y / length, q.z / length, q.w / length)
                : fallback;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFinitePositive(float value)
        {
            return IsFinite(value) && value > 0f;
        }

        private static void RequireNonNegative(float value, string label)
        {
            if (!IsFinite(value) || value < 0f)
            {
                throw new InvalidOperationException($"Parity fixture {label} must be finite and non-negative.");
            }
        }

        private static void RequireCombineRuleZero(int value, string label)
        {
            if (value != 0)
            {
                throw new InvalidOperationException($"Parity fixture {label} must be 0 in CrossHostParity v0.");
            }
        }

        private static void RequireVec3(float[] values, string label)
        {
            if (values == null || values.Length < 3 ||
                !IsFinite(values[0]) || !IsFinite(values[1]) || !IsFinite(values[2]))
            {
                throw new InvalidOperationException($"Parity fixture {label} must be a finite vec3.");
            }
        }

        private static void RequirePositiveVec3(float[] values, string label)
        {
            RequireVec3(values, label);
            if (values[0] <= 0f || values[1] <= 0f || values[2] <= 0f)
            {
                throw new InvalidOperationException($"Parity fixture {label} components must be positive.");
            }
        }

        private static void RequireOptionalZeroVec3(float[] values, string label)
        {
            if (values == null)
            {
                return;
            }

            RequireVec3(values, label);
            if (Mathf.Abs(values[0]) > Mathf.Epsilon ||
                Mathf.Abs(values[1]) > Mathf.Epsilon ||
                Mathf.Abs(values[2]) > Mathf.Epsilon)
            {
                throw new InvalidOperationException($"Parity fixture fixed {label} must be omitted or zero.");
            }
        }

        private static void RequireQuat(float[] values, string label)
        {
            if (values == null || values.Length < 4 ||
                !IsFinite(values[0]) || !IsFinite(values[1]) ||
                !IsFinite(values[2]) || !IsFinite(values[3]))
            {
                throw new InvalidOperationException($"Parity fixture {label} must be a finite quaternion.");
            }

            var lengthSquared =
                values[0] * values[0] +
                values[1] * values[1] +
                values[2] * values[2] +
                values[3] * values[3];
            if (lengthSquared <= Mathf.Epsilon)
            {
                throw new InvalidOperationException($"Parity fixture {label} quaternion must be non-zero.");
            }
        }

        private static StringBuilder Indent(StringBuilder sb, int depth)
        {
            return sb.Append(' ', depth * 2);
        }

        private static string FixtureNameFromTextAsset(TextAsset asset)
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.name))
            {
                return FixtureRelativePath;
            }

            return asset.name.EndsWith(".json", StringComparison.Ordinal)
                ? asset.name
                : $"{asset.name}.json";
        }

        private static void AppendQuoted(StringBuilder sb, string value)
        {
            sb.Append('"');
            foreach (var ch in value ?? string.Empty)
            {
                switch (ch)
                {
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (char.IsControl(ch))
                        {
                            sb.Append("\\u");
                            sb.Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(ch);
                        }
                        break;
                }
            }
            sb.Append('"');
        }

        private static void AppendFloat(StringBuilder sb, float value)
        {
            sb.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        private static string HashHex(ulong value)
        {
            return value.ToString("x16", CultureInfo.InvariantCulture);
        }

        private static bool IsNativeFailure(Exception ex)
        {
            return ex is DllNotFoundException ||
                ex is EntryPointNotFoundException ||
                ex is BadImageFormatException;
        }
    }
}
