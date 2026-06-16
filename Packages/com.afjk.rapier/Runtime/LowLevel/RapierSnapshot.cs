using System;

namespace AFJK.Rapier
{
    public readonly struct RapierSnapshot
    {
        public RapierSnapshot(byte[] bytes)
        {
            Bytes = bytes ?? Array.Empty<byte>();
        }

        public byte[] Bytes { get; }

        public bool IsEmpty => Bytes == null || Bytes.Length == 0;
    }
}

