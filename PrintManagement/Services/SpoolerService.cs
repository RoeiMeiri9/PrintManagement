using Microsoft.Win32.SafeHandles;
using PrintManagement.Models.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;

namespace PrintManagement.Services;

public static partial class SpoolerService
{
    private const int ERROR_INSUFFICIENT_BUFFER = 122;
    private const int ERROR_INVALID_ENVIRONMENT = 1215;

    private delegate bool SpoolerCall(byte* buffer, uint size, out uint needed, out uint count);
    private delegate T BufferParser<T>(byte* buffer, uint count);

    private const string WrongCredentialsMessage = "The username or password is incorrect.";

    private static unsafe T QueryBuffer<T>(SpoolerCall call, BufferParser<T> parse, T empty)
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
                    return parse(buffer, count);

                err = Marshal.GetLastWin32Error();

                // בדיקת הרשאות גם בתוך הלולאה ליתר בטחון
                if (err == 5 || err == 1312 || err == 1797)
                {
                    throw new Win32Exception(err);
                }

                if (err != ERROR_INSUFFICIENT_BUFFER)
                    throw new Win32Exception(err);

                needed = newNeeded;
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


    public static T RunWithCredentials<T>(string user, string domain, string password, Func<T> action)
    {
        if (!PInvoke.LogonUser(user, domain, password,
                LOGON32_LOGON.LOGON32_LOGON_NEW_CREDENTIALS,
                LOGON32_PROVIDER.LOGON32_PROVIDER_WINNT50,
                out var raw))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        using var token = new SafeAccessTokenHandle(raw.DangerousGetHandle());
        raw.SetHandleAsInvalid();
        return WindowsIdentity.RunImpersonated(token, action);
    }

    private static readonly SemaphoreSlim PromptGate = new(1, 1);

    private static Task<T> RunAsync<T>((string User, string Domain, string Password)? creds, Func<T> action)
        => creds is { } c
            ? Task.Run(() => RunWithCredentials(c.User, c.Domain, c.Password, action))
            : Task.Run(action);

    private static async Task<T> WithCredsAsync<T>(string? serverName, Func<T> action, T emptyFallback)
    {
        if (string.IsNullOrWhiteSpace(serverName))
            return await Task.Run(action);

        var used = CredentialsService.GetStoredCredentials(serverName);

        try
        {
            return await RunAsync(used, action);
        }
        catch (Exception ex) when (IsAccessDenied(ex))
        {
            Logger.Warning($"Access denied on '{serverName}' (user: {used?.User ?? "<none>"}): {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.Warning($"Spooler call failed for '{serverName}': {ex.GetType().Name}: {ex.Message}");
            return emptyFallback;
        }

        await PromptGate.WaitAsync();
        try
        {
            // Someone else already entered new credentials while we waited: use them, don't ask again.
            var current = CredentialsService.GetStoredCredentials(serverName);
            if (!Equals(current, used))
                return await RunAsync(current, action);

            // If stored credentials were just rejected, say so in the first dialog too.
            string? error = used is null ? null : WrongCredentialsMessage;

            while (true)
            {
                var fresh = await CredentialsService.PromptAndSaveCredentialsAsync(serverName, error);
                if (fresh is not { } c)
                    return emptyFallback;   // user cancelled

                try
                {
                    return await RunAsync(c, action);
                }
                catch (Exception ex) when (IsAccessDenied(ex))
                {
                    Logger.Warning($"Credentials rejected for '{serverName}' (user: '{c.User}', domain: '{c.Domain}'): {ex.GetType().Name}: {ex.Message}" +
                        (ex is Win32Exception w ? $" (Win32 {w.NativeErrorCode})" : ""));
                    error = WrongCredentialsMessage;   // loop: show the dialog again with the error
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warning($"Retry failed for '{serverName}': {ex.GetType().Name}: {ex.Message}" +
                (ex is Win32Exception w ? $" (Win32 {w.NativeErrorCode})" : ""));
            return emptyFallback;
        }
        finally
        {
            PromptGate.Release();
        }
    }

    private static bool IsAccessDenied(Exception ex)
    {
        if (ex is Win32Exception w32)
        {
            return w32.NativeErrorCode == 5 || w32.NativeErrorCode == 1312 || w32.NativeErrorCode == 1797;
        }
        return ex is UnauthorizedAccessException;
    }

    public static Task<IReadOnlyList<PrinterModel>> GetPrintersAsync(
        string? serverName = null, bool includeConnections = true,
        IReadOnlyList<DriverModel>? knownDrivers = null)
        => WithCredsAsync(serverName, () => GetPrinters(serverName, includeConnections, knownDrivers), []);

    public static Task<IReadOnlyList<DriverModel>> GetDriversAsync(string? serverName = null)
        => WithCredsAsync(serverName, () => GetDrivers(serverName), []);
}