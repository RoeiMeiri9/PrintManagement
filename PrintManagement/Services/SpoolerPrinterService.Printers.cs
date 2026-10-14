using Microsoft.Win32;
using PrintManagement.Pages.CustomFilters.Filters;
using PrintManagement.Pages.CustomFilters.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace PrintManagement.Services;

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

public static unsafe partial class SpoolerPrinterService
{
    private const uint PRINTER_ENUM_LOCAL = 0x00000002;
    private const uint PRINTER_ENUM_NAME = 0x00000008;
    private const uint PRINTER_ENUM_CONNECTIONS = 0x00000004;
    private const string PrintersRegistryPath = @"SYSTEM\CurrentControlSet\Control\Print\Printers";
    // UNVERIFIED: confirm this value name with Process Monitor while toggling
    // the branch office setting in Print Management.
    private const string BranchOfficeValueName = "BranchOfficeEnabled";

    // ───────────── Printers: public API ─────────────

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

    // ───────────── Printers: parsing ─────────────

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
                DriverType = drv?.Type ?? "Unknown",
                BranchOffice = IsBranchOffice(registryBase, printerName)
            });
        }

        return result;
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
}
