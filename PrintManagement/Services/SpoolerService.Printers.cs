using Microsoft.Win32;
using PrintManagement.Models.Entity;
using PrintManagement.Pages.CustomFilters.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

[Flags]
public enum PrinterAttributesMask : uint
{
    Queued = 0x00000001,
    Direct = 0x00000002,
    Default = 0x00000004,
    Shared = 0x00000008,
    Network = 0x00000010,
    Hidden = 0x00000020,
    Local = 0x00000040,
    Enabled = 0x00000080,
    KeepPrintedJobs = 0x00000100,
    DoCompleteFirst = 0x00000200,
    WorkOffline = 0x00000400,
    EnableBidi = 0x00000800,
    RawOnly = 0x00001000,
    Published = 0x00002000,
    Fax = 0x00004000,
    TerminalServer = 0x00008000,
    PushDeploy = 0x00010000,
    PerUser = 0x00020000,
    EnterpriseCloud = 0x00040000
}

public static unsafe partial class SpoolerService
{
    private const uint PRINTER_ENUM_LOCAL = 0x00000002;
    private const uint PRINTER_ENUM_NAME = 0x00000008;
    private const uint PRINTER_ENUM_CONNECTIONS = 0x00000004;
    private const string PrintersRegistryPath = @"SYSTEM\CurrentControlSet\Control\Print\Printers";
    private const string BranchOfficeValueName = "EnableBranchOfficePrinting";

    // ───────────── Printers: public API ─────────────

    /// <param name="serverName">null = local machine, otherwise e.g. "\\\\PRINTSRV".</param>
    public static IReadOnlyList<PrinterModel> GetPrinters(
        string? serverName = null, bool includeConnections = true,
        IReadOnlyList<DriverModel>? knownDrivers = null)
    {
        Dictionary<string, List<DriverModel>> drivers;
        if (knownDrivers is not null)
        {
            drivers = ToDriverMap(knownDrivers);
            Logger.Log($"drivers reused: {drivers.Count}");
        }
        else
        {
            try
            {
                drivers = FetchDrivers(serverName);
            }
            catch (Win32Exception)
            {
                drivers = [with(StringComparer.OrdinalIgnoreCase)];
            }
        }

        uint flags = serverName is null
            ? PRINTER_ENUM_LOCAL | (includeConnections ? PRINTER_ENUM_CONNECTIONS : 0)
            : PRINTER_ENUM_NAME;
        Logger.Log($"GetPrinters server='{serverName ?? "<null>"}' flags=0x{flags:X}");


        using var registryBase = OpenPrintersRegistryKey(serverName);
        if (registryBase is null)
            Logger.Warning($"Printers registry key unavailable for '{serverName ?? "<local>"}'; Branch Office will show false.");
        var ports = FetchPorts(serverName);

        try
        {
            var result = QueryBuffer<IReadOnlyList<PrinterModel>>(
            (buf, size, out needed, out count) =>
                PInvoke.EnumPrinters(flags, serverName, 2, buf, size, out needed, out count),
            (buf, count) => ParsePrinters(buf, count, drivers, ports, registryBase, remote: serverName is not null),
            []);
            Logger.Log($"printers returned: {result.Count}");
            return result;
        }
        catch (Exception ex)
        {
            Logger.Log($"EXCEPTION {ex.GetType().Name}: {ex.Message}" +
                (ex is Win32Exception w ? $" (Win32 {w.NativeErrorCode})" : ""));
            throw;
        }
    }

    // ───────────── Printers: parsing ─────────────

    private static List<PrinterModel> ParsePrinters(
     byte* buffer, uint count,
     Dictionary<string, List<DriverModel>> drivers,
     Dictionary<string, PortDetails> ports,
     RegistryKey? registryBase, bool remote)
    {

        var result = new List<PrinterModel>((int)count);
        var items = (PRINTER_INFO_2*)buffer;

        for (uint i = 0; i < count; i++)
        {
            ref readonly PRINTER_INFO_2 p = ref items[i];
            string rawName = S(p.pPrinterName);
            string serverRawName = S(p.pServerName);

            // Enumerating a remote server returns "\\SRV\Printer"; show it as the MMC does.
            string printerName =
                remote && serverRawName.Length > 0 &&
                rawName.StartsWith(serverRawName + "\\", StringComparison.OrdinalIgnoreCase)
                ? rawName[(serverRawName.Length + 1)..]
                : rawName;

            string driverName = S(p.pDriverName);
            string serverDisplayName = FormatServerName(serverRawName);
            string portName = S(p.pPortName);
            DriverModel? drv = ResolveDriver(rawName, serverRawName, driverName, drivers);  // raw name: it needs the qualified form
            bool isBranchOffice = IsBranchOffice(registryBase, printerName);

            var (kind, summary) = DetermineConnection(p.Attributes, printerName, serverRawName, portName, driverName, isBranchOffice, ports, remote);

            Logger.Log($"printerName: {printerName}, serverName: {serverRawName}, formattedServerName: {serverDisplayName}, attributes: {p.Attributes}");


            result.Add(new PrinterModel
            {
                Name = printerName,
                ServerName = serverDisplayName,
                Provider = drv?.Provider ?? "Unknown",
                ShareName = S(p.pShareName),
                IsShared = (p.Attributes & (uint)PrinterAttributesMask.Shared) != 0,
                PortName = portName,
                DriverName = driverName,
                Comments = S(p.pComment),
                Location = S(p.pLocation),
                PrintProcessor = S(p.pPrintProcessor),
                DataType = S(p.pDatatype),
                Attributes = p.Attributes,
                QueueStatus = PrinterStatusHelper.FormatQueueStatus(p.Status),
                JobsInQueue = p.cJobs,
                DriverVersion = drv?.DriverVersion ?? "Unknown",
                DriverType = drv?.Type ?? "Unknown",
                BranchOffice = isBranchOffice,
                ConnectionSummary = summary,
                ConnectionKind = kind.ToString(),
            });
        }

        return result;
    }

    // ───────────── Port info (new) ─────────────

    [StructLayout(LayoutKind.Sequential)]
    internal struct PORT_INFO_2
    {
        public PWSTR pPortName;
        public PWSTR pMonitorName;
        public PWSTR pDescription;
        public uint fPortType;
        public uint Reserved;
    }

    internal readonly record struct PortDetails(string Name, string Monitor, uint PortType);

    public enum ConnectionKind
    {
        Unknown, Fax, Cloud, RemoteDesktop,
        SharedServer, BranchOffice,
        NetworkTcpIp, NetworkWsd, NetworkIpp, NetworkOther,
        Usb, Bluetooth, Serial, Parallel,
        Virtual, File, Other
    }

    private const uint PRINTER_ATTRIBUTE_NETWORK = 0x00000010;
    private const uint PRINTER_ATTRIBUTE_FAX = 0x00004000;
    private const uint PRINTER_ATTRIBUTE_TS = 0x00008000;
    private const uint PRINTER_ATTRIBUTE_CLOUD = 0x00040000;
    private const uint PORT_TYPE_NET_ATTACHED = 0x00000008;

    private static Dictionary<string, PortDetails> FetchPorts(string? serverName)
    {
        var map = new Dictionary<string, PortDetails>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var ports = QueryBuffer<List<PortDetails>>(
                (buf, size, out needed, out count) =>
                {
                    // fixed on a null string yields a null pointer, which means "local machine"
                    fixed (char* pServer = serverName)
                    {
                        return PInvoke.EnumPorts(new PWSTR(pServer), 2, buf, size, out needed, out count);
                    }
                },
                (buf, count) =>
                {
                    var list = new List<PortDetails>((int)count);
                    var items = (PORT_INFO_2*)buf;
                    for (uint i = 0; i < count; i++)
                        list.Add(new PortDetails(S(items[i].pPortName), S(items[i].pMonitorName), items[i].fPortType));
                    return list;
                },
                []);

            foreach (var p in ports)
                map[p.Name] = p;
        }
        catch (Win32Exception ex)
        {
            Logger.Warning($"Port enumeration failed: {ex.Message} (Win32 {ex.NativeErrorCode})");
        }
        return map;
    }

    // ───────────── Connection classification ─────────────

    private static (ConnectionKind Kind, string Summary) DetermineConnection(
        uint attributes, string printerName, string serverName, string portName, string driverName,
        bool branchOffice, Dictionary<string, PortDetails> ports, bool remoteServer)
    {
        // 1. Unambiguous attribute flags
        if ((attributes & PRINTER_ATTRIBUTE_FAX) != 0) return (ConnectionKind.Fax, "Fax");
        if ((attributes & PRINTER_ATTRIBUTE_CLOUD) != 0) return (ConnectionKind.Cloud, "Cloud (Universal Print)");
        if ((attributes & PRINTER_ATTRIBUTE_TS) != 0) return (ConnectionKind.RemoteDesktop, "Remote Desktop (redirected)");

        // 2. Connection to a print server (\\server\printer)
        bool isConnection = !remoteServer &&
    (printerName.StartsWith(@"\\", StringComparison.Ordinal)
     || ((attributes & PRINTER_ATTRIBUTE_NETWORK) != 0 && serverName.Length > 0));
        if (isConnection)
        {
            return branchOffice
                ? (ConnectionKind.BranchOffice, "Print server (Branch Office, cached locally)")
                : (ConnectionKind.SharedServer, "Print server (shared printer)");
        }

        // 3. Local queue: classify by the port's monitor (pooled printers: first port wins)
        string firstPort = portName.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                   .FirstOrDefault() ?? "";

        ports.TryGetValue(firstPort, out var port);
        string monitor = port.Monitor ?? "";

        bool Mon(string s) => monitor.Contains(s, StringComparison.OrdinalIgnoreCase);
        bool Port(string s) => firstPort.StartsWith(s, StringComparison.OrdinalIgnoreCase);

        if (driverName.Contains("Virtual Print Class", StringComparison.OrdinalIgnoreCase) ||
            firstPort.StartsWith("Microsoft.Office.", StringComparison.OrdinalIgnoreCase))
            return (ConnectionKind.Virtual, "Virtual (no physical device)");

        if (Mon("Fax")) return (ConnectionKind.Fax, "Fax");
        if (Mon("TCP/IP") || Mon("TCPMON")) return (ConnectionKind.NetworkTcpIp, "Network (TCP/IP)");
        if (Mon("WSD")) return (ConnectionKind.NetworkWsd, "Network (WSD)");
        if (Mon("USB")) return (ConnectionKind.Usb, "USB");
        if (Mon("Bluetooth") || Port("BTH")) return (ConnectionKind.Bluetooth, "Bluetooth");
        if (Mon("IPP") || Mon("Internet") ||
            Port("http://") || Port("https://") || Port("ipp://"))
            return (ConnectionKind.NetworkIpp, "Network (IPP)");

        // The built-in "Local Monitor" hosts many unrelated port types: look at the name
        if (Port("LPT")) return (ConnectionKind.Parallel, "Parallel (LPT)");
        if (Port("COM")) return (ConnectionKind.Serial, "Serial (COM)");
        if (Port("USB") || Port("DOT4")) return (ConnectionKind.Usb, "USB");
        if (Port("PORTPROMPT:") || Port("XPSPort:") || Port("nul:") || Port("SHRFAX:"))
            return (ConnectionKind.Virtual, "Virtual (no physical device)");
        if (Port("FILE:")) return (ConnectionKind.File, "Print to file");
        if (firstPort.StartsWith(@"\\", StringComparison.Ordinal))
            return (ConnectionKind.SharedServer, "Network (UNC port)");
        if (firstPort.Contains('\\') || firstPort.Contains('/'))
            return (ConnectionKind.File, "Print to file");

        // 4. Third-party monitor: trust the port-type flag if the spooler set it
        if (port.PortType != 0 && (port.PortType & PORT_TYPE_NET_ATTACHED) != 0)
            return (ConnectionKind.NetworkOther, $"Network ({monitor})");

        return monitor.Length > 0
            ? (ConnectionKind.Other, $"Other ({monitor})")
            : (ConnectionKind.Unknown, "Unknown");
    }


    // ───────────── Branch office ─────────────

    private static RegistryKey? OpenPrintersRegistryKey(string? serverName)
    {
        try
        {
            using var hklm = string.IsNullOrWhiteSpace(serverName)
                ? RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                : RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine,
                      serverName.TrimStart('\\'), RegistryView.Registry64);
            return hklm.OpenSubKey(PrintersRegistryPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                       or System.Security.SecurityException)
        {
            Logger.Warning($"Cannot open printers registry key on '{serverName ?? "<local>"}': {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private static bool IsBranchOffice(RegistryKey? printersKey, string printerName)
    {
        if (printersKey is null || printerName.Length == 0 || printerName.StartsWith(@"\\"))
            return false;

        try
        {
            using var key = printersKey.OpenSubKey($@"{printerName}\PrinterDriverData");
            return key?.GetValue(BranchOfficeValueName) is int v && v == 1;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Branch office check failed for '{printerName}': {ex.Message}");
            return false;
        }
    }
}