using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace PrintManagement.Services;

public static unsafe partial class SpoolerService
{
    private const int ERROR_INSUFFICIENT_BUFFER = 122;
    private const int ERROR_INVALID_ENVIRONMENT = 1215;

    private delegate bool SpoolerCall(byte* buffer, uint size, out uint needed, out uint count);
    private delegate T BufferParser<T>(byte* buffer, uint count);

    // ───────────── Shared helpers (used by both partial files) ─────────────

    /// <summary>
    /// The standard "ask for size, allocate, fetch" spooler pattern.
    /// Throws on real errors instead of silently returning empty.
    /// </summary>
    private static T QueryBuffer<T>(SpoolerCall call, BufferParser<T> parse, T empty)
    {
        if (call(null, 0, out uint needed, out _))
            return empty;

        int err = Marshal.GetLastWin32Error();

        if (err == 0 && needed > 0)
            err = ERROR_INSUFFICIENT_BUFFER;

        if (err != ERROR_INSUFFICIENT_BUFFER)
            throw new Win32Exception(err);


        for (int attempt = 0; attempt < 5; attempt++)
        {
            byte* buffer = (byte*)NativeMemory.Alloc(needed);
            try
            {
                if (call(buffer, needed, out uint newNeeded, out uint count))
                    return parse(buffer, count); // must copy everything out before returning

                err = Marshal.GetLastWin32Error();
                if (err != ERROR_INSUFFICIENT_BUFFER)
                    throw new Win32Exception(err);

                needed = newNeeded; // data changed between calls, retry
            }
            finally
            {
                NativeMemory.Free(buffer);
            }
        }

        throw new InvalidOperationException("Spooler data kept changing while enumerating.");
    }

    private static string S(PWSTR p) => p.ToString() ?? string.Empty;

    private static string Str(IntPtr p) =>
        p == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUni(p) ?? string.Empty;
}