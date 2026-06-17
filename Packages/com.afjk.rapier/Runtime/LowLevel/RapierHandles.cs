using System;
using System.Runtime.InteropServices;

namespace AFJK.Rapier
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RapierRigidBodyHandle : IEquatable<RapierRigidBodyHandle>
    {
        public uint Index;
        public uint Generation;

        public static readonly RapierRigidBodyHandle Invalid = new RapierRigidBodyHandle
        {
            Index = uint.MaxValue,
            Generation = uint.MaxValue
        };

        public bool IsValid => !Equals(Invalid);

        public bool Equals(RapierRigidBodyHandle other)
        {
            return Index == other.Index && Generation == other.Generation;
        }

        public override bool Equals(object obj)
        {
            return obj is RapierRigidBodyHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Index * 397) ^ (int)Generation;
            }
        }

        public override string ToString()
        {
            return IsValid ? $"RigidBody({Index}:{Generation})" : "RigidBody(Invalid)";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RapierColliderHandle : IEquatable<RapierColliderHandle>
    {
        public uint Index;
        public uint Generation;

        public static readonly RapierColliderHandle Invalid = new RapierColliderHandle
        {
            Index = uint.MaxValue,
            Generation = uint.MaxValue
        };

        public bool IsValid => !Equals(Invalid);

        public bool Equals(RapierColliderHandle other)
        {
            return Index == other.Index && Generation == other.Generation;
        }

        public override bool Equals(object obj)
        {
            return obj is RapierColliderHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Index * 397) ^ (int)Generation;
            }
        }

        public override string ToString()
        {
            return IsValid ? $"Collider({Index}:{Generation})" : "Collider(Invalid)";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RapierJointHandle : IEquatable<RapierJointHandle>
    {
        public uint Index;
        public uint Generation;

        public static readonly RapierJointHandle Invalid = new RapierJointHandle
        {
            Index = uint.MaxValue,
            Generation = uint.MaxValue
        };

        public bool IsValid => !Equals(Invalid);

        public bool Equals(RapierJointHandle other)
        {
            return Index == other.Index && Generation == other.Generation;
        }

        public override bool Equals(object obj)
        {
            return obj is RapierJointHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Index * 397) ^ (int)Generation;
            }
        }

        public override string ToString()
        {
            return IsValid ? $"Joint({Index}:{Generation})" : "Joint(Invalid)";
        }
    }
}

