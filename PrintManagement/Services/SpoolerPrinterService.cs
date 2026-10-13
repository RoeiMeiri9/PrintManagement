using Microsoft.Win32;
using PrintManagement.Pages.CustomFilterPage.Filters;
using PrintManagement.Pages.CustomFilterPage.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace PrintManagement.Services;



[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
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

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct DRIVER_INFO_2
{
    public uint cVersion;
    public IntPtr pName;
    public IntPtr pEnvironment;
    public IntPtr pDriverPath;
    public IntPtr pDataFile;
    public IntPtr pConfigFile;
}

[Flags]
internal enum PRINTER_ENUM_FLAGS : uint
{
    PRINTER_ENUM_DEFAULT = 0x00000001,
    PRINTER_ENUM_LOCAL = 0x00000002,
    PRINTER_ENUM_CONNECTIONS = 0x00000004,
    PRINTER_ENUM_FAVORITE = 0x00000004 //YES IT SHOULD BE THE SAME VALUE AS CONNECTIONS, IT'S A BUG IN THE API
}


public static unsafe class SpoolerPrinterService
{
    private const int ERROR_INSUFFICIENT_BUFFER = 122;

    public static IReadOnlyList<PrinterFilterModel> GetPrinters(bool includeConnections = true)
    {
        var driversMap = FetchDriversInfo();

        var flags = PRINTER_ENUM_FLAGS.PRINTER_ENUM_LOCAL;
        if (includeConnections)
            flags |= PRINTER_ENUM_FLAGS.PRINTER_ENUM_CONNECTIONS;

        const uint level = 2;

        PInvoke.EnumPrinters((uint)flags, null, level, null, 0, out uint needed, out uint returned);

        int err = Marshal.GetLastWin32Error();
        if (needed == 0)
        {
            if (err != 0 && err != ERROR_INSUFFICIENT_BUFFER)
                throw new Win32Exception(err);
            return [];
        }

        for (int attempt = 0; attempt < 3; attempt++)
        {
            void* buffer = NativeMemory.Alloc(needed);
            try
            {
                var span = new Span<byte>(buffer, (int)needed);

                fixed (byte* p = &MemoryMarshal.GetReference(span))
                {
                    if (PInvoke.EnumPrinters((uint)flags, null, level, p, (uint)span.Length, out uint newNeeded, out uint count))
                    {
                        var result = new List<PrinterFilterModel>((int)count);
                        int structSize = Marshal.SizeOf<PRINTER_INFO_2>();
                        IntPtr currentPtr = (IntPtr)buffer;

                        for (uint i = 0; i < count; i++)
                        {
                            // שימוש ב-Marshal הבטוח מונع פגיעה בזיכרון לא מנוהל וב-Padding
                            var printer = Marshal.PtrToStructure<PRINTER_INFO_2>(currentPtr);

                            string printerName = printer.pPrinterName.Value != null ? new string(printer.pPrinterName.Value) : string.Empty;
                            string driverName = printer.pDriverName.Value != null ? new string(printer.pDriverName.Value) : string.Empty;

                            string driverVersion = "Unknown";
                            string driverType = "v3";
                            if (driversMap.TryGetValue(driverName, out var drvInfo))
                            {
                                driverVersion = drvInfo.Version;
                                driverType = drvInfo.Type;
                            }

                            bool branchOffice = CheckBranchOfficeRegistry(printerName);

                            result.Add(new PrinterFilterModel
                            {
                                Name = printerName,
                                ServerName = printer.pServerName.Value != null ? new string(printer.pServerName.Value) : string.Empty,
                                ShareName = printer.pShareName.Value != null ? new string(printer.pShareName.Value) : string.Empty,
                                PortName = printer.pPortName.Value != null ? new string(printer.pPortName.Value) : string.Empty,
                                DriverName = driverName,
                                Comments = printer.pComment.Value != null ? new string(printer.pComment.Value) : string.Empty,
                                Location = printer.pLocation.Value != null ? new string(printer.pLocation.Value) : string.Empty,
                                PrintProcessor = printer.pPrintProcessor.Value != null ? new string(printer.pPrintProcessor.Value) : string.Empty,
                                DataType = printer.pDatatype.Value != null ? new string(printer.pDatatype.Value) : string.Empty,
                                Attributes = printer.Attributes,
                                QueueStatus = PrinterStatusHelper.FormatQueueStatus(printer.Status),
                                JobsInQueue = printer.cJobs,
                                DriverVersion = driverVersion,
                                DriverType = driverType,
                                BranchOffice = branchOffice
                            });

                            // מעבר מדויק למבנה הבא בזיכרון לפי הגודל האמיתי של ה-Struct
                            currentPtr = IntPtr.Add(currentPtr, structSize);
                        }
                        return result;
                    }

                    err = Marshal.GetLastWin32Error();
                    if (err != ERROR_INSUFFICIENT_BUFFER)
                        throw new Win32Exception(err);

                    needed = newNeeded;
                }
            }
            finally
            {
                NativeMemory.Free(buffer);
            }
        }

        throw new InvalidOperationException("Printer list kept changing while enumerating.");
    }

    private static Dictionary<string, (string Version, string Type)> FetchDriversInfo()
    {
        var map = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);
        uint level = 2; // רמה 2 יציבה לחלוטין וללא בעיות Padding

        PInvoke.EnumPrinterDrivers(null, null, level, null, 0, out uint needed, out uint returned);
        if (needed == 0) return map;

        void* buffer = NativeMemory.Alloc(needed);
        try
        {
            var span = new Span<byte>(buffer, (int)needed);
            fixed (byte* p = &MemoryMarshal.GetReference(span))
            {
                if (PInvoke.EnumPrinterDrivers(null, null, level, p, (uint)span.Length, out _, out uint count))
                {
                    int structSize = Marshal.SizeOf<DRIVER_INFO_2>();
                    IntPtr currentPtr = (IntPtr)buffer;

                    for (uint i = 0; i < count; i++)
                    {
                        // שימוש ב-PtrToStructure מונע לחלוטין בעיות אופסטים ומצביעים
                        var drv = Marshal.PtrToStructure<DRIVER_INFO_2>(currentPtr);

                        string name = drv.pName != IntPtr.Zero ? Marshal.PtrToStringUni(drv.pName) ?? string.Empty : string.Empty;

                        if (!string.IsNullOrEmpty(name))
                        {
                            uint cVersion = drv.cVersion;
                            string type = cVersion >= 4 ? "v4" : "v3";
                            string version = $"{cVersion}.0.0.0";

                            if (!map.ContainsKey(name))
                            {
                                map.Add(name, (version, type));
                            }
                        }

                        currentPtr = IntPtr.Add(currentPtr, structSize);
                    }
                }
            }
        }
        catch
        {
            // מונע קריסה כללית במקרה של חריגה בודדת בשליפת דרייברים
        }
        finally
        {
            NativeMemory.Free(buffer);
        }

        return map;
    }

    private static bool CheckBranchOfficeRegistry(string printerName)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@$"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Print\Printers\{printerName}");
            if (key != null)
            {
                object? val = key.GetValue("BranchOfficeEnabled");
                if (val is int intVal && intVal == 1) return true;
            }
        }
        catch { }
        return false;
    }


}