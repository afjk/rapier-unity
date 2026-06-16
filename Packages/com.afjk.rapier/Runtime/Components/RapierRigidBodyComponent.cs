using System.Collections.Generic;
using UnityEngine;

namespace AFJK.Rapier
{
    [DisallowMultipleComponent]
    public sealed class RapierRigidBodyComponent : MonoBehaviour
    {
        [SerializeField] private RapierWorldComponent worldComponent;
        [SerializeField] private RapierRigidBodyType bodyType = RapierRigidBodyType.Dynamic;
        [SerializeField] private bool registerOnEnable = true;
        [SerializeField] private bool syncTransformFromRapier = true;
        [SerializeField] private bool syncTransformToRapierOnRegister = true;
        [SerializeField] private bool syncTransformToRapierBeforeStep;
        [SerializeField] private bool canSleep = true;
        [SerializeField] private bool ccdEnabled;
        [SerializeField] private Vector3 initialLinearVelocity;
        [SerializeField] private Vector3 initialAngularVelocity;
        [SerializeField] private float linearDamping;
        [SerializeField] private float angularDamping;

        private readonly List<RapierColliderComponent> colliders = new List<RapierColliderComponent>();

        public RapierRigidBodyHandle BodyHandle { get; private set; } = RapierRigidBodyHandle.Invalid;

        public bool IsRegistered => BodyHandle.IsValid;

        public RapierWorldComponent WorldComponent => worldComponent;

        public RapierWorld World => worldComponent != null ? worldComponent.World : null;

        public bool SyncTransformFromRapier
        {
            get => syncTransformFromRapier;
            set => syncTransformFromRapier = value;
        }

        public bool SyncTransformToRapierBeforeStep
        {
            get => syncTransformToRapierBeforeStep;
            set => syncTransformToRapierBeforeStep = value;
        }

        public bool Register()
        {
            if (IsRegistered)
            {
                return true;
            }

            if (worldComponent == null)
            {
                worldComponent = GetComponentInParent<RapierWorldComponent>();
            }

            if (worldComponent == null)
            {
                Debug.LogWarning($"{nameof(RapierRigidBodyComponent)} requires a {nameof(RapierWorldComponent)}.", this);
                return false;
            }

            var world = worldComponent.EnsureWorld();
            BodyHandle = world.CreateRigidBody(CreateDesc());

            if (!BodyHandle.IsValid)
            {
                Debug.LogWarning("Failed to create Rapier rigid body.", this);
                return false;
            }

            worldComponent.RegisterBody(this);

            if (syncTransformToRapierOnRegister)
            {
                PushTransformToRapier();
            }

            for (var i = 0; i < colliders.Count; i++)
            {
                colliders[i].CreateInWorld(this);
            }

            return true;
        }

        public void Unregister()
        {
            for (var i = colliders.Count - 1; i >= 0; i--)
            {
                colliders[i].DestroyInWorld();
            }

            if (BodyHandle.IsValid && worldComponent != null && worldComponent.World != null && worldComponent.World.IsCreated)
            {
                worldComponent.World.DestroyRigidBody(BodyHandle);
            }

            BodyHandle = RapierRigidBodyHandle.Invalid;

            if (worldComponent != null)
            {
                worldComponent.UnregisterBody(this);
            }
        }

        public bool PushTransformToRapier()
        {
            if (!IsRegistered || World == null || !World.IsCreated)
            {
                return false;
            }

            return World.SetTransform(
                BodyHandle,
                new RapierTransform(transform.position, transform.rotation));
        }

        public bool PullTransformFromRapier()
        {
            if (!IsRegistered || World == null || !World.IsCreated)
            {
                return false;
            }

            if (!World.TryGetTransform(BodyHandle, out var rapierTransform))
            {
                return false;
            }

            transform.SetPositionAndRotation(rapierTransform.Position, rapierTransform.Rotation);
            return true;
        }

        internal void RegisterCollider(RapierColliderComponent collider)
        {
            if (collider == null || colliders.Contains(collider))
            {
                return;
            }

            colliders.Add(collider);

            if (IsRegistered)
            {
                collider.CreateInWorld(this);
            }
        }

        internal void UnregisterCollider(RapierColliderComponent collider)
        {
            colliders.Remove(collider);
        }

        internal void SyncTransformToRapierBeforeStepIfNeeded()
        {
            if (syncTransformToRapierBeforeStep)
            {
                PushTransformToRapier();
            }
        }

        internal void SyncTransformFromRapierIfNeeded()
        {
            if (syncTransformFromRapier)
            {
                PullTransformFromRapier();
            }
        }

        internal void ForgetNativeRegistration(RapierWorldComponent owner)
        {
            if (owner != worldComponent)
            {
                return;
            }

            BodyHandle = RapierRigidBodyHandle.Invalid;
            for (var i = 0; i < colliders.Count; i++)
            {
                colliders[i].ForgetNativeRegistration();
            }
        }

        private void OnEnable()
        {
            if (registerOnEnable)
            {
                Register();
            }
        }

        private void OnDisable()
        {
            Unregister();
        }

        private RapierBodyDesc CreateDesc()
        {
            return new RapierBodyDesc
            {
                BodyType = bodyType,
                Position = transform.position,
                Rotation = transform.rotation,
                LinearVelocity = initialLinearVelocity,
                AngularVelocity = initialAngularVelocity,
                LinearDamping = linearDamping,
                AngularDamping = angularDamping,
                CanSleep = canSleep,
                CcdEnabled = ccdEnabled
            };
        }
    }
}

