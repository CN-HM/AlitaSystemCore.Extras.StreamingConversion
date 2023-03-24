using System.Runtime.InteropServices;

namespace AlitaSystemCore.Extras.StreamingConversion.Internal;

public static class StructUtils
{
    public static byte[] StructToBytes<T>(T obj) where T : struct
    {
        var size  = Marshal.SizeOf(obj);
        var bytes = new byte[size];
        var ptr   = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(obj, ptr, false);
        Marshal.Copy(ptr, bytes, 0, size);
        Marshal.FreeHGlobal(ptr);
        return bytes;
    }
}