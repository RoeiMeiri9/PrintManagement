using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
    SoftResetRequired = 0x0400,
    SandboxDisabled = 0x0800,
    Category3D = 0x1000,
    CategoryCloud = 0x2000,
}

public sealed record DriverDetails(
    string Name,
    string Environment,
    uint CVersion,
    string Type,
    string Version,
    string Isolation,
    string Provider,
    string ServerName,
    string PrintProcessor,
    bool IsPackageAware,
    DateTime? Date,
    string InfPath,
    bool IsXps,
    string ConfigFile,
    string DataFile,
    string DefaultDatatype,
    string DriverFile,
    string HardwareId,
    string HelpFile,
    string LanguageMonitor,
    string Manufacturer,
    string OemUrl,
    bool IsShareable,
    DriverAttributes Attributes);

// ───────────────────────── Drivers ─────────────────────────

public static unsafe partial class SpoolerPrinterService
{
    private static readonly string[] DriverEnvironments =
        { "Windows x64", "Windows NT x86", "Windows ARM64" };

    // ───────────── Drivers page: all environments ─────────────

    public static Task<IReadOnlyList<DriverDetails>> GetDriversAsync(string? serverName = null)
        => Task.Run(() => GetDrivers(serverName));

    /// <summary>Every driver on the server, across all supported architectures.</summary>
    public static IReadOnlyList<DriverDetails> GetDrivers(string? serverName = null)
    {
        string displayServer = serverName ?? Environment.MachineName;
        var all = new List<DriverDetails>();

        foreach (string env in DriverEnvironments)
        {
            try
            {
                all.AddRange(QueryBuffer(
                    (byte* buf, uint size, out uint needed, out uint count) =>
                        PInvoke.EnumPrinterDrivers(serverName, env, 8, buf, size, out needed, out count),
                    (buf, count) => ParseDrivers(buf, count, displayServer),
                    []));
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == ERROR_INVALID_ENVIRONMENT)
            {
                // This server doesn't support that architecture; skip it.
                Debug.WriteLine($"Environment '{env}' not supported on '{displayServer}'.");
            }
        }

        return all;
    }

    // ───────────── Printers page: native architecture only ─────────────

    /// <summary>Name → drivers map for the server's native architecture (used to resolve printers).</summary>
    public static Dictionary<string, List<DriverDetails>> FetchDrivers(string? serverName)
    {
        string displayServer = serverName ?? Environment.MachineName;

        // environment = null → the server's native architecture only.
        var list = QueryBuffer(
            (byte* buf, uint size, out uint needed, out uint count) =>
                PInvoke.EnumPrinterDrivers(serverName, null, 8, buf, size, out needed, out count),
            (buf, count) => ParseDrivers(buf, count, displayServer),
            []);

        var map = new Dictionary<string, List<DriverDetails>>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in list)
        {
            if (!map.TryGetValue(d.Name, out var bucket))
                map[d.Name] = bucket = new List<DriverDetails>();
            bucket.Add(d);
        }
        return map;
    }

    // ───────────── Parsing ─────────────

    private static List<DriverDetails> ParseDrivers(byte* buffer, uint count, string serverName)
    {
        var result = new List<DriverDetails>((int)count);
        var items = (DRIVER_INFO_8*)buffer;

        for (uint i = 0; i < count; i++)
        {
            var details = ToDetails(in items[i], serverName);
            if (details.Name.Length > 0)
                result.Add(details);
        }
        return result;
    }

    private static DriverDetails ToDetails(in DRIVER_INFO_8 d, string serverName)
    {
        var attrs = (DriverAttributes)d.dwPrinterDriverAttributes;
        bool packageAware = attrs.HasFlag(DriverAttributes.PackageAware);

        return new DriverDetails(
            Name: Str(d.pName),
            Environment: Str(d.pEnvironment),
            CVersion: d.cVersion,
            Type: FormatDriverType(d.cVersion, packageAware),
            Version: FormatVersion(d.dwlDriverVersion),
            Isolation: FormatIsolation(attrs),
            Provider: Str(d.pszProvider),
            ServerName: serverName,
            PrintProcessor: Str(d.pszPrintProcessor),
            IsPackageAware: packageAware,
            Date: ToDate(d.ftDriverDate),
            InfPath: Str(d.pszInfPath),
            IsXps: attrs.HasFlag(DriverAttributes.Xps),
            ConfigFile: Str(d.pConfigFile),
            DataFile: Str(d.pDataFile),
            DefaultDatatype: Str(d.pDefaultDataType),
            DriverFile: Str(d.pDriverPath),
            HardwareId: Str(d.pszHardwareID),
            HelpFile: Str(d.pHelpFile),
            LanguageMonitor: Str(d.pMonitorName),
            Manufacturer: Str(d.pszMfgName),
            OemUrl: Str(d.pszOEMUrl),
            IsShareable: !attrs.HasFlag(DriverAttributes.NotShareable),
            Attributes: attrs);
    }

    // ───────────── Printer → driver resolution ─────────────

    /// <summary>
    /// Fast path: the driver name is unique on the server → use it.
    /// Ambiguous (e.g. same name as both v3 and v4) → ask the spooler which
    /// driver this specific printer uses. Never guesses.
    /// </summary>
    private static DriverDetails? ResolveDriver(
        string printerName, string driverName,
        Dictionary<string, List<DriverDetails>> drivers)
    {
        if (driverName.Length == 0 || !drivers.TryGetValue(driverName, out var candidates))
            return null;

        if (candidates.Count == 1)
            return candidates[0];

        return GetExactDriver(printerName) ?? candidates.OrderByDescending(c => c.CVersion).First();
    }

    private static DriverDetails? GetExactDriver(string printerName)
    {
        if (!PInvoke.OpenPrinter(printerName, out var handle, null))
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

                return ToDetails(in *(DRIVER_INFO_8*)buffer, Environment.MachineName);
            }
            finally
            {
                NativeMemory.Free(buffer);
            }
        }
        finally
        {
            handle.Dispose();
        }
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

    /// <summary>
    /// UNVERIFIED proxy. Print Management shows None / Shared / Isolated. I derive it from
    /// the SANDBOX_ENABLED / SANDBOX_DISABLED attribute bits; compare against the MMC
    /// on your Server 2025 VM and adjust here if they differ.
    /// </summary>
    private static string FormatIsolation(DriverAttributes attrs)
    {
        if (attrs.HasFlag(DriverAttributes.SandboxDisabled)) return "None";
        if (attrs.HasFlag(DriverAttributes.SandboxEnabled)) return "Isolated";
        return "Shared";
    }

    private static string FormatVersion(ulong v) =>
        $"{(v >> 48) & 0xFFFF}.{(v >> 32) & 0xFFFF}.{(v >> 16) & 0xFFFF}.{v & 0xFFFF}";

    private static DateTime? ToDate(System.Runtime.InteropServices.ComTypes.FILETIME ft)
    {
        long ticks = ((long)(uint)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
        return ticks == 0 ? null : DateTime.FromFileTimeUtc(ticks);
    }
}