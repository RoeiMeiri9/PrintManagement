using Microsoft.Win32;
using PrintManagement.Pages.CustomFilterPage.Filters;
using PrintManagement.Pages.CustomFilterPage.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace PrintManagement.Services;

// ───────────────────────── Native structs ─────────────────────────

[StructLayout(LayoutKind.Sequential)]
internal struct PRINTER_INFO_2
{
    public PWSTR pServerName;
    public PWSTR pPrinterName;
    public PWSTR pShareName;
    public PWSTR pPortName;
    public PWSTR pDriverName;
    public PWSTR pComment;
    public PWSTR pLocation;
    public IntPtr pDevMode;
    public PWSTR pSepFile;
    public PWSTR pPrintProcessor;
    public PWSTR pDatatype;
    public PWSTR pParameters;
    public IntPtr pSecurityDescriptor;
    public uint Attributes;
    public uint Priority;
    public uint DefaultPriority;
    public uint StartTime;
    public uint UntilTime;
    public uint Status;
    public uint cJobs;
    public uint AveragePPM;
}

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
internal enum DriverAttributes : uint
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

internal sealed record DriverDetails(
    string Name,
    uint CVersion,
    string Version,
    DateTime? Date,
    DriverAttributes Attributes,
    string Provider,
    string Manufacturer,
    string InfPath,
    string Environment)
{
    public bool IsPackageAware => Attributes.HasFlag(DriverAttributes.PackageAware);
}

// ───────────────────────── Service ─────────────────────────

public static unsafe class SpoolerPrinterService
{
    private const int ERROR_INSUFFICIENT_BUFFER = 122;

    private const uint PRINTER_ENUM_LOCAL = 0x00000002;
    private const uint PRINTER_ENUM_NAME = 0x00000008;
    private const uint PRINTER_ENUM_CONNECTIONS = 0x00000004;

    private const string PrintersRegistryPath = @"SYSTEM\CurrentControlSet\Control\Print\Printers";
    // UNVERIFIED: confirm this value name with Process Monitor while toggling
    // the branch office setting in Print Management.
    private const string BranchOfficeValueName = "BranchOfficeEnabled";

    private delegate bool SpoolerCall(byte* buffer, uint size, out uint needed, out uint count);
    private delegate T BufferParser<T>(byte* buffer, uint count);

    // ───────────── Public API ─────────────

    public static Task<IReadOnlyList<PrinterFilterModel>> GetPrintersAsync(
        string? serverName = null, bool includeConnections = true)
        => Task.Run(() => GetPrinters(serverName, includeConnections));

    /// <param name="serverName">null = local machine, otherwise e.g. "\\\\PRINTSRV".</param>
    public static IReadOnlyList<PrinterFilterModel> GetPrinters(
        string? serverName = null, bool includeConnections = true)
    {
        // A driver lookup failure should not prevent listing printers.
        Dictionary<string, List<DriverDetails>> drivers;
        try
        {
            drivers = FetchDrivers(serverName);
        }
        catch (Win32Exception ex)
        {
            Debug.WriteLine($"Driver enumeration failed: {ex.Message}");
            drivers = new(StringComparer.OrdinalIgnoreCase);
        }

        uint flags = serverName is null
            ? PRINTER_ENUM_LOCAL | (includeConnections ? PRINTER_ENUM_CONNECTIONS : 0)
            : PRINTER_ENUM_NAME;

        // Opened once for the whole enumeration (local only), not once per printer.
        using var registryBase = serverName is null ? OpenPrintersRegistryKey() : null;

        return QueryBuffer<IReadOnlyList<PrinterFilterModel>>(
            (byte* buf, uint size, out uint needed, out uint count) =>
                PInvoke.EnumPrinters(flags, serverName, 2, buf, size, out needed, out count),
            (buf, count) => ParsePrinters(buf, count, drivers, registryBase),
            []);
    }

    // ───────────── Printers ─────────────

    private static List<PrinterFilterModel> ParsePrinters(
        byte* buffer, uint count,
        Dictionary<string, List<DriverDetails>> drivers,
        RegistryKey? registryBase)
    {
        var result = new List<PrinterFilterModel>((int)count);
        var items = (PRINTER_INFO_2*)buffer;

        for (uint i = 0; i < count; i++)
        {
            ref readonly PRINTER_INFO_2 p = ref items[i];

            string printerName = S(p.pPrinterName);
            string driverName = S(p.pDriverName);
            DriverDetails? drv = ResolveDriver(printerName, driverName, drivers);

            result.Add(new PrinterFilterModel
            {
                Name = printerName,
                ServerName = S(p.pServerName),
                ShareName = S(p.pShareName),
                PortName = S(p.pPortName),
                DriverName = driverName,
                Comments = S(p.pComment),
                Location = S(p.pLocation),
                PrintProcessor = S(p.pPrintProcessor),
                DataType = S(p.pDatatype),
                Attributes = p.Attributes,
                QueueStatus = PrinterStatusHelper.FormatQueueStatus(p.Status),
                JobsInQueue = p.cJobs,
                DriverVersion = drv?.Version ?? "Unknown",
                DriverType = FormatDriverType(drv),
                BranchOffice = IsBranchOffice(registryBase, printerName)
            });
        }

        return result;
    }

    // ───────────── Drivers ─────────────

    private static Dictionary<string, List<DriverDetails>> FetchDrivers(string? serverName)
    {
        // environment = null → the server's native architecture only.
        var list = QueryBuffer(
            (byte* buf, uint size, out uint needed, out uint count) =>
                PInvoke.EnumPrinterDrivers(serverName, null, 8, buf, size, out needed, out count),
            ParseDrivers,
            new List<DriverDetails>());

        var map = new Dictionary<string, List<DriverDetails>>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in list)
        {
            if (!map.TryGetValue(d.Name, out var bucket))
                map[d.Name] = bucket = new List<DriverDetails>();
            bucket.Add(d);
        }
        return map;
    }

    private static List<DriverDetails> ParseDrivers(byte* buffer, uint count)
    {
        var result = new List<DriverDetails>((int)count);
        var items = (DRIVER_INFO_8*)buffer;

        for (uint i = 0; i < count; i++)
        {
            var details = ToDetails(in items[i]);
            if (details.Name.Length > 0)
                result.Add(details);
        }
        return result;
    }

    private static DriverDetails ToDetails(in DRIVER_INFO_8 d) => new(
        Name: Str(d.pName),
        CVersion: d.cVersion,
        Version: FormatVersion(d.dwlDriverVersion),
        Date: ToDate(d.ftDriverDate),
        Attributes: (DriverAttributes)d.dwPrinterDriverAttributes,
        Provider: Str(d.pszProvider),
        Manufacturer: Str(d.pszMfgName),
        InfPath: Str(d.pszInfPath),
        Environment: Str(d.pEnvironment));

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

        return GetExactDriver(printerName);
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

                return ToDetails(in *(DRIVER_INFO_8*)buffer);
            }
            finally
            {
                NativeMemory.Free(buffer);
            }
        }
        finally
        {
            // handle is a SafeHandle/ClosePrinterSafeHandle; dispose it
            handle.Dispose();
        }
    }

    // ───────────── Driver type / user mode ─────────────

    /// <summary>
    /// true = user mode, false = kernel mode, null = unknown.
    /// Rule: v4 drivers and package-aware drivers (DRIVER_INFO_8
    /// dwPrinterDriverAttributes &amp; PRINTER_DRIVER_PACKAGE_AWARE) are user mode;
    /// everything else is treated as kernel mode.
    /// This is the one place to change if it disagrees with Print Management.
    /// </summary>
    private static bool? IsUserMode(DriverDetails? drv)
    {
        if (drv is null) return null;
        return drv.CVersion >= 4 || drv.IsPackageAware;
    }

    private static string FormatDriverType(DriverDetails? drv)
    {
        if (drv is null) return "Unknown";

        string mode = IsUserMode(drv) switch
        {
            true => "User Mode",
            false => "Kernel Mode",
            null => "Unknown"
        };
        return $"Type {drv.CVersion} - {mode}";
    }

    // ───────────── Branch office ─────────────

    private static RegistryKey? OpenPrintersRegistryKey()
    {
        try
        {
            using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            return hklm.OpenSubKey(PrintersRegistryPath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Cannot open printers registry key: {ex.Message}");
            return null;
        }
    }

    private static bool IsBranchOffice(RegistryKey? printersKey, string printerName)
    {
        // Connections (\\server\printer) are not stored under this key.
        if (printersKey is null || printerName.Length == 0 || printerName.StartsWith(@"\\"))
            return false;

        try
        {
            using var key = printersKey.OpenSubKey(printerName);
            return key?.GetValue(BranchOfficeValueName) is int v && v == 1;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Branch office check failed for '{printerName}': {ex.Message}");
            return false;
        }
    }

    // ───────────── Shared helpers ─────────────

    /// <summary>
    /// The standard "ask for size, allocate, fetch" spooler pattern.
    /// Throws on real errors instead of silently returning empty.
    /// </summary>
    private static T QueryBuffer<T>(SpoolerCall call, BufferParser<T> parse, T empty)
    {
        // Succeeding with no buffer means there is nothing to return.
        if (call(null, 0, out uint needed, out _))
            return empty;

        int err = Marshal.GetLastWin32Error();
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

                needed = newNeeded; // list changed between calls, retry
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

    private static string FormatVersion(ulong v) =>
        $"{(v >> 48) & 0xFFFF}.{(v >> 32) & 0xFFFF}.{(v >> 16) & 0xFFFF}.{v & 0xFFFF}";

    private static DateTime? ToDate(System.Runtime.InteropServices.ComTypes.FILETIME ft)
    {
        long ticks = ((long)(uint)ft.dwHighDateTime << 32) | (uint)ft.dwLowDateTime;
        return ticks == 0 ? null : DateTime.FromFileTimeUtc(ticks);
    }
}