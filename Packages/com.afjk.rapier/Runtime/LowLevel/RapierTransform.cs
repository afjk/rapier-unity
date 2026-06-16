using System.Runtime.InteropServices;
using UnityEngine;

namespace AFJK.Rapier
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RapierTransform
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public static RapierTransform Identity => new RapierTransform
        {
            Position = Vector3.zero,
            Rotation = Quaternion.identity
        };

        public RapierTransform(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation == default(Quaternion) ? Quaternion.identity : rotation;
        }
    }
}
