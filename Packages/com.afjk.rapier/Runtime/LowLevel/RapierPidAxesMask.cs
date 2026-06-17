using System;

namespace AFJK.Rapier
{
    [Flags]
    public enum RapierPidAxesMask : byte
    {
        None = 0,
        LinX = 1 << 0,
        LinY = 1 << 1,
        LinZ = 1 << 2,
        AngX = 1 << 3,
        AngY = 1 << 4,
        AngZ = 1 << 5,
        AllLin = LinX | LinY | LinZ,
        AllAng = AngX | AngY | AngZ,
        All = AllLin | AllAng
    }
}
