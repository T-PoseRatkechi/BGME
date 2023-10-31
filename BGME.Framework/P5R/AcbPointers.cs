namespace BGME.Framework.P5R;

internal unsafe static class AcbPointers
{
    public static nint? AcbAddres_1
    {
        get
        {
            var pointer = (nint*)(Utilities.BaseAddress + 0x26E05A0);
            if (*pointer == 0)
            {
                Log.Warning("Attempted to load ACB when no DLC BGM loaded.");
                return null;
            }

            var address = *(nint*)(*pointer + 0x18);
            return address;
        }
    }

    public static nint? AcbAddress_2
    {
        get
        {
            var pointer = (nint*)(Utilities.BaseAddress + 0x26E05A0);
            if (*pointer == 0)
            {
                Log.Warning("Attempted to load ACB when no DLC BGM loaded.");
                return null;
            }

            var address = *(nint*)(*pointer + 0x1D0);
            return address;
        }
    }

    public static nint AcbAddress_3
    {
        get
        {
            var pointer = (nint*)(Utilities.BaseAddress + 0x29A3D30);
            var address = *(nint*)(*pointer + 0x18);
            Log.Warning("ACB address may point to unused data.");
            return address;
        }
    }

    public static nint? AcbAddress_4
    {
        get
        {
            var pointer = (nint*)(Utilities.BaseAddress + 0x29A3D30);
            var address = *(nint*)(*pointer + 0x1D0);
            Log.Warning("ACB address may point to unused data.");
            return address;
        }
    }
}
