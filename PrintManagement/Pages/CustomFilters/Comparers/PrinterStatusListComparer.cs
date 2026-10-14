using PrintManagement.Pages.CustomFilters.Helpers;
using System.Collections;
using System.Collections.Generic;

namespace PrintManagement.Pages.CustomFilters.Comparers;

public class PrinterStatusListComparer : IComparer
{
    public int Compare(object? x, object? y)
    {
        // המרה בטוחה לטיפוס הרשימה שקיים אצלנו במודל
        var listX = x as List<PrinterStatusInfo>;
        var listY = y as List<PrinterStatusInfo>;

        if (listX == null && listY == null) return 0;
        if (listX == null) return -1;
        if (listY == null) return 1;

        string xStatus = GetPrintingStatusAsIntegers(listX);
        string yStatus = GetPrintingStatusAsIntegers(listY);

        return string.Compare(xStatus, yStatus, System.StringComparison.Ordinal);
    }

    private static string GetPrintingStatusAsIntegers(List<PrinterStatusInfo> infos)
    {
        string result = string.Empty;
        foreach (var info in infos)
        {
            result += info.Flags switch
            {
                PrinterStatusFlags.Ready => "0",
                PrinterStatusFlags.Error => "1",
                PrinterStatusFlags.Paused => "2",
                PrinterStatusFlags.PaperJam => "3",
                PrinterStatusFlags.NoToner => "4",
                PrinterStatusFlags.DoorOpen => "5",
                PrinterStatusFlags.OutOfMemory => "6",
                PrinterStatusFlags.UserIntervention => "7",
                PrinterStatusFlags.PaperProblem => "8",
                PrinterStatusFlags.NotAvailable => "9",
                PrinterStatusFlags.ServerUnknown => "A",
                PrinterStatusFlags.PaperOut => "B",
                PrinterStatusFlags.TonerLow => "C",
                PrinterStatusFlags.ManualFeed => "D",
                PrinterStatusFlags.OutputBinFull => "E",
                PrinterStatusFlags.PendingDeletion => "F",
                PrinterStatusFlags.Processing => "G",
                PrinterStatusFlags.Busy => "H",
                PrinterStatusFlags.Printing => "I",
                PrinterStatusFlags.IOActive => "J",
                PrinterStatusFlags.WarmingUp => "K",
                PrinterStatusFlags.Initializing => "L",
                PrinterStatusFlags.PagePert => "M",
                PrinterStatusFlags.Waiting => "N",
                PrinterStatusFlags.PowerSave => "O",
                PrinterStatusFlags.Offline => "P",
                _ => ""
            };
        }
        return result;
    }
}