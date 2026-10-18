using Microsoft.Win32;
using PrintManagement.Pages.CustomFilters.Filters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.Win32;

namespace PrintManagement.Services;

// ───────────────────────── Native struct ─────────────────────────

// Matches winspool.h DRIVER_INFO_8 (200 bytes on x64).
[StructLayout(LayoutKind.Sequential, Pack = 8)]
internal struct DRIVER_INFO_8
{
    public uint cVersion;
    public IntPtr pName;
    public IntPtr pEnvironment;
    public IntPtr pDriverPath;
    public IntPtr pDataFile;
    public IntPtr pConfigFile;
    public IntPtr pHelpFile;
    public IntPtr pDependentFiles;
    public IntPtr pMonitorName;
    public IntPtr pDefaultDataType;
    public IntPtr pszzPreviousNames;
    public System.Runtime.InteropServices.ComTypes.FILETIME ftDriverDate;
    public ulong dwlDriverVersion;
    public IntPtr pszMfgName;
    public IntPtr pszOEMUrl;
    public IntPtr pszHardwareID;
    public IntPtr pszProvider;
    public IntPtr pszPrintProcessor;
    public IntPtr pszVendorSetup;
    public IntPtr pszzColorProfiles;
    public IntPtr pszInfPath;
    public uint dwPrinterDriverAttributes;
    public IntPtr pszzCoreDriverDependencies;
    public System.Runtime.InteropServices.ComTypes.FILETIME ftMinInboxDriverVerDate;
    public ulong dwlMinInboxDriverVerVersion;
}

[Flags]
public enum DriverAttributes : uint
{
    PackageAware = 0x0001,
    Xps = 0x0002,
    SandboxEnabled = 0x0004,
    Class = 0x0008,
    Derived = 0x0010,
    NotShareable = 0x0020,
    CategoryFax = 0x0040,
    CategoryFile = 0x0080,
    CategoryVirtual = 0x0100,
    CategoryService = 0x0200,
    SoftResetRequired = 0x0400
}

// ───────────────────────── Drivers ─────────────────────────

public static unsafe partial class SpoolerService
{
    private static readonly string[] DriverEnvironments =
        { "Windows x64", "Windows NT x86", "Windows ARM64" };

    // ───────────── Drivers page: all environments ─────────────

    /// <summary>Every driver on the server, across all supported architectures.</summary>
    public static IReadOnlyList<DriversFilterModel> GetDrivers(string? serverName = null)
    {
        var all = new List<DriversFilterModel>();
        IsolationConfig? cfg = ReadIsolationConfig(serverName);
        foreach (string env in DriverEnvironments)
        {
            try
            {
                // If serverName (injectd into the pName variable) is NULL, the function enumerates the local printer drivers.
                all.AddRange(QueryBuffer(
                    (byte* buf, uint size, out uint needed, out uint count) =>
                        PInvoke.EnumPrinterDrivers(serverName, env, 8, buf, size, out needed, out count),
                    (buf, count) => ParseDrivers(buf, count, serverName, cfg),
                    []));
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == ERROR_INVALID_ENVIRONMENT)
            {
                // This server doesn't support that architecture; skip it.
                Logger.Log($"Environment '{env}' not supported on '{serverName ?? "<local>"}'.");
            }
        }

        return all;
    }

    // ───────────── Printers page: native architecture only ─────────────

    /// <summary>Name → drivers map for the server's native architecture (used to resolve printers).</summary>
    public static Dictionary<string, List<DriversFilterModel>> FetchDrivers(string? serverName)
    {
        IsolationConfig? cfg = ReadIsolationConfig(serverName);

        // If serverName (injectd into the pName variable) is NULL, the function enumerates the local printer drivers.
        var list = QueryBuffer(
            (byte* buf, uint size, out uint needed, out uint count) =>
                PInvoke.EnumPrinterDrivers(serverName, null, 8, buf, size, out needed, out count),
            (buf, count) => ParseDrivers(buf, count, serverName, cfg),
            []);

        return ToDriverMap(list);
    }

    private static Dictionary<string, List<DriversFilterModel>> ToDriverMap(IEnumerable<DriversFilterModel> list)
    {
        var map = new Dictionary<string, List<DriversFilterModel>>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in list)
        {
            if (!map.TryGetValue(d.DriverName, out var bucket))
                map[d.DriverName] = bucket = [];
            bucket.Add(d);
        }
        return map;
    }

    // ───────────── Parsing ─────────────

    private static List<DriversFilterModel> ParseDrivers(byte* buffer, uint count, string? serverName, IsolationConfig? cfg)
    {
        var result = new List<DriversFilterModel>((int)count);
        var items = (DRIVER_INFO_8*)buffer;

        for (uint i = 0; i < count; i++)
        {
            var details = ToDetails(in items[i], serverName, cfg);
            if (details.DriverName.Length > 0)
                result.Add(details);
        }
        return result;
    }

    private static DriversFilterModel ToDetails(in DRIVER_INFO_8 d, string? serverName, IsolationConfig? cfg)
    {
        var attrs = (DriverAttributes)d.dwPrinterDriverAttributes;
        string driverName = Str(d.pName);
        bool packageAware = attrs.HasFlag(DriverAttributes.PackageAware);

        return new DriversFilterModel
        {
            DriverName = driverName,
            Environment = Str(d.pEnvironment),
            CVersion = d.cVersion,
            Type = FormatDriverType(d.cVersion, packageAware),
            DriverVersion = FormatVersion(d.dwlDriverVersion),
            DriverIsolation = FormatIsolation(driverName, attrs, cfg),
            Provider = Str(d.pszProvider),
            ServerName = FormatServerName(serverName),
            PrintProcessor = Str(d.pszPrintProcessor),
            Packaged = packageAware,
            DriverDate = ToDate(d.ftDriverDate),
            InfPath = Str(d.pszInfPath),
            XPSDriver = attrs.HasFlag(DriverAttributes.Xps),
            ConfigFilePath = Str(d.pConfigFile),
            DataFilePath = Str(d.pDataFile),
            DefaultDatatype = Str(d.pDefaultDataType),
            DriverFilePath = Str(d.pDriverPath),
            HardwareID = Str(d.pszHardwareID),
            HelpFilePath = Str(d.pHelpFile),
            LanguageMonitor = Str(d.pMonitorName),
            Manufacturer = Str(d.pszMfgName),
            OEMURL = Str(d.pszOEMUrl),
            Shareable = !attrs.HasFlag(DriverAttributes.NotShareable)
        };
    }

    // ───────────── Printer → driver resolution ─────────────

    /// <summary>
    /// Fast path: the driver name is unique on the server → use it.
    /// Ambiguous (e.g. same name as both v3 and v4) → ask the spooler which
    /// driver this specific printer uses. Never guesses.
    /// </summary>
    private static DriversFilterModel? ResolveDriver(
    string printerName, string rawServer, string driverName,
    Dictionary<string, List<DriversFilterModel>> drivers)
    {
        if (driverName.Length == 0 || !drivers.TryGetValue(driverName, out var candidates))
            return null;
        if (candidates.Count == 1)
            return candidates[0];

        return GetExactDriver(printerName, rawServer, candidates)
               ?? candidates.OrderByDescending(c => c.CVersion).First();
    }

    private static DriversFilterModel? GetExactDriver(
        string printerName, string rawServer, List<DriversFilterModel> candidates)
    {
        // Qualify the name so a remote printer isn't opened on the local spooler.
        string target = rawServer.Length > 0 && !printerName.StartsWith(@"\\")
            ? $@"{rawServer}\{printerName}" : printerName;

        if (!PInvoke.OpenPrinter(target, out var handle, null))
            return null;
        try
        {
            PInvoke.GetPrinterDriver(handle, null, 8, null, 0, out uint needed);
            if (Marshal.GetLastWin32Error() != ERROR_INSUFFICIENT_BUFFER || needed == 0)
                return null;

            byte* buffer = (byte*)NativeMemory.Alloc(needed);
            try
            {
                if (!PInvoke.GetPrinterDriver(handle, null, 8, buffer, needed, out _))
                    return null;

                ref readonly DRIVER_INFO_8 d = ref *(DRIVER_INFO_8*)buffer;
                string env = Str(d.pEnvironment);
                uint ver = d.cVersion;
                return candidates.FirstOrDefault(c => c.CVersion == ver &&
                    string.Equals(c.Environment, env, StringComparison.OrdinalIgnoreCase));
            }
            finally { NativeMemory.Free(buffer); }
        }
        finally { handle.Dispose(); }
    }

    // ───────────── Formatting rules (one place each) ─────────────

    /// <summary>
    /// v4 drivers and package-aware drivers are treated as user mode, the rest as kernel mode.
    /// This is the one place to change if it disagrees with Print Management.
    /// </summary>
    private static string FormatDriverType(uint cVersion, bool packageAware)
    {
        bool userMode = cVersion >= 4 || packageAware;
        return $"Type {cVersion} - {(userMode ? "User Mode" : "Kernel Mode")}";
    }

    private const string PrintKey = @"SYSTEM\CurrentControlSet\Control\Print";
    private const string GroupsValue = "PrintDriverIsolationGroups";
    private const string PolicyKey = @"SOFTWARE\Policies\Microsoft\Windows NT\Printers";
    private const string PolicyExec = "PrintDriverIsolationExecutionPolicy";      // TODO: VERIFY
    private const string PolicyOverride = "PrintDriverIsolationOverrideCompat";   // TODO: VERIFY

    internal sealed record IsolationConfig(
        List<HashSet<string>> Groups, bool IsolationEnabled, bool OverrideCompat);

    private static IsolationConfig? ReadIsolationConfig(string? serverName)
    {
        try
        {
            using var hklm = string.IsNullOrWhiteSpace(serverName)
                ? RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                : RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine,
                      serverName.TrimStart('\\'), RegistryView.Registry64);

            var groups = new List<HashSet<string>>();
            using (var key = hklm.OpenSubKey(PrintKey))
            {
                if (key?.GetValue(GroupsValue) is string raw && raw.Length > 0)
                    foreach (string g in raw.Split(@"\\"))
                        groups.Add(new HashSet<string>(
                            g.Split('\\', StringSplitOptions.RemoveEmptyEntries),
                            StringComparer.OrdinalIgnoreCase));
            }

            bool enabled = true, overrideCompat = false;
            using (var pol = hklm.OpenSubKey(PolicyKey))
            {
                if (pol?.GetValue(PolicyExec) is int e) enabled = e != 0;
                if (pol?.GetValue(PolicyOverride) is int o) overrideCompat = o != 0;
            }
            Logger.Log($"isolation config '{serverName ?? "<local>"}': groups={groups.Count}, enabled={enabled}, overrideCompat={overrideCompat}");
            return new IsolationConfig(groups, enabled, overrideCompat);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                       or System.Security.SecurityException)
        {
            Logger.Log($"Isolation config unavailable for '{serverName}': {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private static string FormatIsolation(string driverName, DriverAttributes attrs, IsolationConfig? cfg)
    {
        if (cfg is null) return "Unknown";
        if (!cfg.IsolationEnabled) return "None";                    // GP: machine disabled

        for (int i = 0; i < cfg.Groups.Count; i++)                   // per-driver mode set
            if (cfg.Groups[i].Contains(driverName))
                return i == 0 ? "None" : i == 1 ? "Shared" : "Isolated";

        // Not set: INF compat (0x4 as proxy) → Shared; else the override policy decides.
        // NOTE: the 0x4 = "INF DriverIsolation=2" mapping is untested. No driver with this flag was
        // available on the test servers (all isolatable drivers there were v3 without 0x4).
        return attrs.HasFlag(DriverAttributes.SandboxEnabled) || cfg.OverrideCompat ? "Shared" : "None";
    }


    private static string FormatVersion(ulong v) =>
        $"{(v >> 48) & 0xFFFF}.{(v >> 32) & 0xFFFF}.{(v >> 16) & 0xFFFF}.{v & 0xFFFF}";

    private static DateTime? ToDate(System.Runtime.InteropServices.ComTypes.FILETIME ft)
    {
        long ticks = ((long)(uint)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
        return ticks == 0 ? null : DateTime.FromFileTimeUtc(ticks);
    }
}