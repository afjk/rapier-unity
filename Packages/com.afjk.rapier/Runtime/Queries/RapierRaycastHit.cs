using UnityEngine;

namespace AFJK.Rapier
{
    public readonly struct RapierRaycastHit
    {
        public RapierRaycastHit(
            RapierColliderHandle collider,
            Vector3 point,
            Vector3 normal,
            float distance)
        {
            Collider = collider;
            Point = point;
            Normal = normal;
            Distance = distance;
        }

        public RapierColliderHandle Collider { get; }

        public Vector3 Point { get; }

        public Vector3 Normal { get; }

        public float Distance { get; }
    }
}

