using System.Runtime.InteropServices;

namespace BGME.Framework.CRI.Types;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct CriAtomExCategoryInfo
{
    public uint groupNo;
    public uint id;
    public byte* name;
    public uint numCueLimits;
    public float volume;
}
