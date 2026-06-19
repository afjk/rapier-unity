using System;
using UnityEngine;

namespace AFJK.Rapier
{
    /// <summary>
    /// Builds Rapier component GameObjects from a neutral <see cref="RapierSceneDescription"/>.
    /// <para>
    /// This is the foundation for path-2 of the project goals ("generate from Scene Sync"): a
    /// downstream, Scene Sync-aware adapter converts its own scene/physics data into a
    /// <see cref="RapierSceneDescription"/> and calls this importer. The importer itself — and the
    /// Rapier components it creates — have no dependency on Scene Sync. Source-specific metadata is
    /// recorded on a <see cref="RapierImportedObject"/> component, not on the core components.
    /// </para>
    /// <para>
    /// Imported components use <c>RegisterOnEnable = false</c>; the world is constructed in one
    /// deterministic pass via <see cref="RapierWorldBehaviour.RebuildWorld"/> using the
    /// description's <see cref="RapierSceneDescription.registrationMode"/>, so the same description
    /// produces the same world on every host.
    /// </para>
    /// </summary>
    public static class RapierSceneImporter
    {
        /// <summary>Parses a JSON <see cref="RapierSceneDescription"/> and imports it.</summary>
        public static RapierWorldBehaviour ImportJson(string json, Transform parent = null, string worldName = "Rapier Imported World")
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentException("JSON is null or empty.", nameof(json));
            }

            var description = JsonUtility.FromJson<RapierSceneDescription>(json);
            if (description == null)
            {
                throw new ArgumentException("Could not parse a RapierSceneDescription from the JSON.", nameof(json));
            }

            return Import(description, parent, worldName);
        }

        /// <summary>
        /// Creates a world GameObject with a <see cref="RapierWorldBehaviour"/> plus a body
        /// GameObject (with collider components and a <see cref="RapierImportedObject"/>) for each
        /// entry in <paramref name="description"/>, then deterministically rebuilds the world.
        /// </summary>
        public static RapierWorldBehaviour Import(RapierSceneDescription description, Transform parent = null, string worldName = "Rapier Imported World")
        {
            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            var worldObject = new GameObject(string.IsNullOrEmpty(worldName) ? "Rapier Imported World" : worldName);
            if (parent != null)
            {
                worldObject.transform.SetParent(parent, false);
            }

            var world = worldObject.AddComponent<RapierWorldBehaviour>();
            world.StepMode = RapierWorldStepMode.Manual;
            world.Gravity = description.gravity;
            world.Timestep = Mathf.Max(0.000001f, description.timestep);
            world.RegistrationMode = description.registrationMode;

            if (description.bodies != null)
            {
                for (var i = 0; i < description.bodies.Count; i++)
                {
                    BuildBody(world, description.bodies[i], description.sourceSystem);
                }
            }

            // Single deterministic construction pass (bodies, then colliders, then joints).
            world.RebuildWorld();
            return world;
        }

        private static void BuildBody(RapierWorldBehaviour world, RapierBodyDescription body, string sourceSystem)
        {
            if (body == null)
            {
                return;
            }

            var go = new GameObject(string.IsNullOrEmpty(body.id) ? "Imported Body" : body.id);
            go.SetActive(false);
            go.transform.SetParent(world.transform, false);
            go.transform.SetPositionAndRotation(body.position, Quaternion.Euler(body.eulerAngles));

            var rigidBody = go.AddComponent<RapierRigidbody>();
            rigidBody.RegisterOnEnable = false;
            rigidBody.BodyType = body.bodyType;
            rigidBody.StableId = body.id;
            rigidBody.RegistrationOrder = body.order;
            rigidBody.InitialLinearVelocity = body.linearVelocity;
            rigidBody.InitialAngularVelocity = body.angularVelocity;
            rigidBody.LinearDamping = body.linearDamping;
            rigidBody.AngularDamping = body.angularDamping;
            rigidBody.GravityScale = body.gravityScale;
            rigidBody.CcdEnabled = body.ccdEnabled;

            var meta = go.AddComponent<RapierImportedObject>();
            meta.SourceSystem = sourceSystem;
            meta.SourceId = body.id;
            meta.SourceOrder = body.order;

            if (body.colliders != null)
            {
                for (var i = 0; i < body.colliders.Count; i++)
                {
                    BuildCollider(go, body.colliders[i]);
                }
            }

            // Active so RebuildWorld discovers it; RegisterOnEnable is false so it does not
            // self-register out of order.
            go.SetActive(true);
        }

        private static void BuildCollider(GameObject go, RapierColliderDescription collider)
        {
            if (collider == null)
            {
                return;
            }

            RapierCollider component;
            switch (collider.shape)
            {
                case RapierImportColliderShape.Sphere:
                    var sphere = go.AddComponent<RapierSphereCollider>();
                    sphere.Radius = collider.radius;
                    component = sphere;
                    break;
                case RapierImportColliderShape.Capsule:
                    var capsule = go.AddComponent<RapierCapsuleCollider>();
                    capsule.HalfHeight = collider.halfHeight;
                    capsule.Radius = collider.radius;
                    component = capsule;
                    break;
                default:
                    var box = go.AddComponent<RapierBoxCollider>();
                    box.HalfExtents = collider.halfExtents;
                    component = box;
                    break;
            }

            component.RegisterOnEnable = false;
            component.StableId = collider.id;
            component.Density = collider.density;
            component.Friction = collider.friction;
            component.Restitution = collider.restitution;
            component.IsSensor = collider.isSensor;
            component.LocalPosition = collider.localPosition;
        }
    }
}
