using UnityEngine;

namespace AFJK.Rapier
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Rapier/Controllers/Rapier Character Controller")]
    public sealed class RapierCharacterControllerBehaviour : MonoBehaviour
    {
        [SerializeField] private RapierRigidbody rigidBody;
        [SerializeField] private bool registerBodyOnEnable = true;
        [SerializeField] private RapierShapeType shapeType = RapierShapeType.Capsule;
        [SerializeField] private Vector3 halfExtents = Vector3.one * 0.5f;
        [SerializeField] private float radius = 0.4f;
        [SerializeField] private float halfHeight = 0.5f;
        [SerializeField] private Vector3 up = Vector3.up;
        [SerializeField] private float offset = 0.01f;
        [SerializeField] private bool slide = true;
        [SerializeField] private bool autostepEnabled;
        [SerializeField] private float autostepMaxHeight = 0.25f;
        [SerializeField] private float autostepMinWidth = 0.1f;
        [SerializeField] private bool autostepIncludeDynamicBodies;
        [SerializeField] private float maxSlopeClimbAngle = 45f;
        [SerializeField] private float minSlopeSlideAngle = 30f;
        [SerializeField] private bool snapToGroundEnabled;
        [SerializeField] private float snapToGroundDistance = 0.1f;
        [SerializeField] private float normalNudgeFactor = 1.0e-4f;
        [SerializeField] private RapierQueryFilterFlags filterFlags;
        [SerializeField] private bool useCollisionGroups;
        [SerializeField] private uint collisionGroups;
        [SerializeField] private bool excludeOwnBody = true;

        public RapierCharacterMovement LastMovement { get; private set; }

        public RapierRigidbody RigidBody
        {
            get => rigidBody;
            set => rigidBody = value;
        }

        public RapierShapeType ShapeType
        {
            get => shapeType;
            set => shapeType = value;
        }

        public Vector3 HalfExtents
        {
            get => halfExtents;
            set => halfExtents = Vector3.Max(value, Vector3.zero);
        }

        public float Radius
        {
            get => radius;
            set => radius = Mathf.Max(0f, value);
        }

        public float HalfHeight
        {
            get => halfHeight;
            set => halfHeight = Mathf.Max(0f, value);
        }

        public Vector3 Up
        {
            get => up == Vector3.zero ? Vector3.up : up;
            set => up = value == Vector3.zero ? Vector3.up : value;
        }

        public bool ExcludeOwnBody
        {
            get => excludeOwnBody;
            set => excludeOwnBody = value;
        }

        public RapierCharacterController Controller
        {
            get => CreateController();
            set => ApplyController(value);
        }

        public RapierQueryShape Shape
        {
            get => CreateShape();
            set => ApplyShape(value);
        }

        public RapierQueryFilter QueryFilter => ApplyOwnBodyExclusion(CreateFilter());

        public bool ComputeMovement(
            Vector3 desiredTranslation,
            float deltaTime,
            out RapierCharacterMovement movement)
        {
            return TryComputeMovement(desiredTranslation, deltaTime, CreateFilter(), true, out movement, out _);
        }

        public bool ComputeMovement(
            Vector3 desiredTranslation,
            float deltaTime,
            RapierQueryFilter filter,
            out RapierCharacterMovement movement)
        {
            return TryComputeMovement(desiredTranslation, deltaTime, filter, true, out movement, out _);
        }

        public bool Move(
            Vector3 desiredTranslation,
            float deltaTime,
            out RapierCharacterMovement movement)
        {
            return Move(desiredTranslation, deltaTime, CreateFilter(), out movement);
        }

        public bool Move(
            Vector3 desiredTranslation,
            float deltaTime,
            RapierQueryFilter filter,
            out RapierCharacterMovement movement)
        {
            movement = default;
            if (!EnsurePositionBasedKinematicBody())
            {
                return false;
            }

            if (!TryComputeMovement(desiredTranslation, deltaTime, filter, true, out movement, out var current))
            {
                return false;
            }

            return rigidBody.World.SetNextKinematicTranslation(
                rigidBody.BodyHandle,
                current.Position + movement.Translation);
        }

        public bool Move(Vector3 desiredTranslation, float deltaTime)
        {
            return Move(desiredTranslation, deltaTime, out _);
        }

        private bool TryComputeMovement(
            Vector3 desiredTranslation,
            float deltaTime,
            RapierQueryFilter filter,
            bool applyOwnBodyExclusion,
            out RapierCharacterMovement movement,
            out RapierTransform current)
        {
            movement = default;
            current = default;

            if (!EnsureBodyRegistered(true) ||
                rigidBody.World == null ||
                !rigidBody.World.IsCreated ||
                !rigidBody.World.TryGetTransform(rigidBody.BodyHandle, out current))
            {
                return false;
            }

            if (applyOwnBodyExclusion)
            {
                filter = ApplyOwnBodyExclusion(filter);
            }

            if (!rigidBody.World.MoveCharacter(
                CreateShape(),
                current,
                desiredTranslation,
                Mathf.Max(0.000001f, deltaTime),
                CreateController(),
                filter,
                out movement))
            {
                return false;
            }

            LastMovement = movement;
            return true;
        }

        private RapierQueryFilter ApplyOwnBodyExclusion(RapierQueryFilter filter)
        {
            if (excludeOwnBody && rigidBody != null && rigidBody.BodyHandle.IsValid)
            {
                filter = filter.ExcludingBody(rigidBody.BodyHandle);
            }

            return filter;
        }

        private bool EnsureBodyRegistered(bool warn)
        {
            if (rigidBody == null)
            {
                rigidBody = GetComponentInParent<RapierRigidbody>();
            }

            if (rigidBody == null)
            {
                if (warn)
                {
                    Debug.LogWarning($"{nameof(RapierCharacterControllerBehaviour)} requires a {nameof(RapierRigidbody)}.", this);
                }

                return false;
            }

            return rigidBody.IsRegistered || rigidBody.Register();
        }

        private bool EnsurePositionBasedKinematicBody()
        {
            if (!EnsureBodyRegistered(true))
            {
                return false;
            }

            if (rigidBody.BodyType == RapierRigidBodyType.KinematicPositionBased)
            {
                return true;
            }

            Debug.LogWarning($"{nameof(RapierCharacterControllerBehaviour)}.{nameof(Move)} requires a {nameof(RapierRigidBodyType.KinematicPositionBased)} body.", this);
            return false;
        }

        private RapierQueryShape CreateShape()
        {
            switch (shapeType)
            {
                case RapierShapeType.Ball:
                    return RapierQueryShape.Ball(radius);
                case RapierShapeType.Cuboid:
                    return RapierQueryShape.Cuboid(halfExtents);
                default:
                    return RapierQueryShape.Capsule(halfHeight, radius);
            }
        }

        private void ApplyShape(RapierQueryShape shape)
        {
            shapeType = shape.ShapeType;
            halfExtents = Vector3.Max(shape.HalfExtents, Vector3.zero);
            radius = Mathf.Max(0f, shape.Radius);
            halfHeight = Mathf.Max(0f, shape.HalfHeight);
        }

        private RapierCharacterController CreateController()
        {
            return new RapierCharacterController
            {
                Up = Up,
                Offset = Mathf.Max(0f, offset),
                Slide = slide,
                AutostepEnabled = autostepEnabled,
                AutostepMaxHeight = Mathf.Max(0f, autostepMaxHeight),
                AutostepMinWidth = Mathf.Max(0f, autostepMinWidth),
                AutostepIncludeDynamicBodies = autostepIncludeDynamicBodies,
                MaxSlopeClimbAngle = maxSlopeClimbAngle * Mathf.Deg2Rad,
                MinSlopeSlideAngle = minSlopeSlideAngle * Mathf.Deg2Rad,
                SnapToGroundEnabled = snapToGroundEnabled,
                SnapToGroundDistance = Mathf.Max(0f, snapToGroundDistance),
                NormalNudgeFactor = Mathf.Max(0f, normalNudgeFactor)
            };
        }

        private void ApplyController(RapierCharacterController controller)
        {
            up = controller.Up == Vector3.zero ? Vector3.up : controller.Up;
            offset = Mathf.Max(0f, controller.Offset);
            slide = controller.Slide;
            autostepEnabled = controller.AutostepEnabled;
            autostepMaxHeight = Mathf.Max(0f, controller.AutostepMaxHeight);
            autostepMinWidth = Mathf.Max(0f, controller.AutostepMinWidth);
            autostepIncludeDynamicBodies = controller.AutostepIncludeDynamicBodies;
            maxSlopeClimbAngle = controller.MaxSlopeClimbAngle * Mathf.Rad2Deg;
            minSlopeSlideAngle = controller.MinSlopeSlideAngle * Mathf.Rad2Deg;
            snapToGroundEnabled = controller.SnapToGroundEnabled;
            snapToGroundDistance = Mathf.Max(0f, controller.SnapToGroundDistance);
            normalNudgeFactor = Mathf.Max(0f, controller.NormalNudgeFactor);
        }

        private RapierQueryFilter CreateFilter()
        {
            var filter = RapierQueryFilter.Default.WithFlags(filterFlags);
            if (useCollisionGroups)
            {
                filter = filter.WithGroups(collisionGroups);
            }

            return filter;
        }

        private void OnEnable()
        {
            if (registerBodyOnEnable)
            {
                EnsureBodyRegistered(false);
            }
        }

        private void OnValidate()
        {
            halfExtents = Vector3.Max(halfExtents, Vector3.zero);
            radius = Mathf.Max(0f, radius);
            halfHeight = Mathf.Max(0f, halfHeight);
            up = up == Vector3.zero ? Vector3.up : up;
            offset = Mathf.Max(0f, offset);
            autostepMaxHeight = Mathf.Max(0f, autostepMaxHeight);
            autostepMinWidth = Mathf.Max(0f, autostepMinWidth);
            snapToGroundDistance = Mathf.Max(0f, snapToGroundDistance);
            normalNudgeFactor = Mathf.Max(0f, normalNudgeFactor);
        }
    }
}
