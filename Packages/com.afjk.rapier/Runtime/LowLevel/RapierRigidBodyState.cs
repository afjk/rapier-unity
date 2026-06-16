using UnityEngine;

namespace AFJK.Rapier
{
    public readonly struct RapierRigidBodyState
    {
        public RapierTransform Transform { get; }
        public Vector3 LinearVelocity { get; }
        public Vector3 AngularVelocity { get; }
        public bool Sleeping { get; }
        public bool Enabled { get; }

        internal RapierRigidBodyState(RapierNative.RigidBodyStateNative native)
        {
            Transform = native.Transform;
            LinearVelocity = native.LinearVelocity;
            AngularVelocity = native.AngularVelocity;
            Sleeping = native.Sleeping != 0;
            Enabled = native.Enabled != 0;
        }
    }
}
