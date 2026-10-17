using Microsoft.Win32.SafeHandles;
using PrintManagement.Pages.CustomFilters.Filters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;

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

    private static string FormatServerName(string? serverName) => string.IsNullOrEmpty(serverName) ? $"{Environment.MachineName} (local)" : serverName;

    // TEMP for testing; later: prompt in the UI and store the password safely
    public static (string User, string Domain, string Password)? Credentials { get; set; }


    private const int LOGON32_LOGON_NEW_CREDENTIALS = 9;
    private const int LOGON32_PROVIDER_WINNT50 = 3;

    /// <summary>Like "runas /netonly": local identity unchanged, network calls use these credentials.</summary>
    public static T RunWithCredentials<T>(string user, string domain, string password, Func<T> action)
    {
        if (!PInvoke.LogonUser(user, domain, password,
                LOGON32_LOGON.LOGON32_LOGON_NEW_CREDENTIALS,
                LOGON32_PROVIDER.LOGON32_PROVIDER_WINNT50,
                out var raw))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        // CsWin32 returns its own SafeHandle, but RunImpersonated wants a SafeAccessTokenHandle.
        using var token = new SafeAccessTokenHandle(raw.DangerousGetHandle());
        raw.SetHandleAsInvalid();   // ownership moved: otherwise both would close the same handle
        return WindowsIdentity.RunImpersonated(token, action);
    }

    /// <summary>Remote servers only: local calls never need the test credentials.</summary>
    private static T WithCreds<T>(string? serverName, Func<T> action) =>
        !string.IsNullOrWhiteSpace(serverName) && Credentials is { } c
            ? RunWithCredentials(c.User, c.Domain, c.Password, action)
            : action();

    public static Task<IReadOnlyList<PrinterFilterModel>> GetPrintersAsync(
        string? serverName = null, bool includeConnections = true)
        => Task.Run(() => WithCreds(serverName, () => GetPrinters(serverName, includeConnections)));

    public static Task<IReadOnlyList<DriversFilterModel>> GetDriversAsync(string? serverName = null)
        => Task.Run(() => WithCreds(serverName, () => GetDrivers(serverName)));
}