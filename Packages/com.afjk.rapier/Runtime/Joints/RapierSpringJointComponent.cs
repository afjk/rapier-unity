using UnityEngine;

namespace AFJK.Rapier
{
    public sealed class RapierSpringJointComponent : RapierJointComponent
    {
        [SerializeField] private float restLength = 1f;
        [SerializeField] private float stiffness = 100f;
        [SerializeField] private float damping = 5f;

        public float RestLength
        {
            get => restLength;
            set => restLength = Mathf.Max(0f, value);
        }

        public float Stiffness
        {
            get => stiffness;
            set => stiffness = Mathf.Max(0f, value);
        }

        public float Damping
        {
            get => damping;
            set => damping = Mathf.Max(0f, value);
        }

        protected override RapierJointHandle CreateJoint(
            RapierWorld world,
            RapierRigidBodyHandle body1Handle,
            RapierRigidBodyHandle body2Handle)
        {
            return world.CreateSpringJoint(
                body1Handle,
                body2Handle,
                LocalAnchor1,
                LocalAnchor2,
                restLength,
                stiffness,
                damping);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            restLength = Mathf.Max(0f, restLength);
            stiffness = Mathf.Max(0f, stiffness);
            damping = Mathf.Max(0f, damping);
        }
    }
}
