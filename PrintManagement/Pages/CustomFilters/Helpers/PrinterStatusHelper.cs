using System;
using System.Collections.Generic;

namespace PrintManagement.Pages.CustomFilters.Helpers;

[Flags]
public enum PrinterStatusFlags : uint
{
    Ready = 0x00000000,
    Paused = 0x00000001,
    Error = 0x00000002,
    PendingDeletion = 0x00000004,
    PaperJam = 0x00000008,
    PaperOut = 0x00000010,
    ManualFeed = 0x00000020,
    PaperProblem = 0x00000040,
    Offline = 0x00000080,
    IOActive = 0x00000100,
    Busy = 0x00000200,
    Printing = 0x00000400,
    OutputBinFull = 0x00000800,
    NotAvailable = 0x00001000,
    Waiting = 0x00002000,
    Processing = 0x00004000,
    Initializing = 0x00008000,
    WarmingUp = 0x00010000,
    TonerLow = 0x00020000,
    NoToner = 0x00040000,
    PagePert = 0x00080000,
    UserIntervention = 0x00100000,
    OutOfMemory = 0x00200000,
    DoorOpen = 0x00400000,
    ServerUnknown = 0x00800000,
    PowerSave = 0x01000000
}

public record PrinterStatusInfo
{
    public string Status { get; set; } = string.Empty;
    public PrinterStatusFlags Flags { get; set; }

    public override string ToString() => Status;
}

public partial class PrinterStatusHelper
{

    public static List<PrinterStatusInfo> FormatQueueStatus(uint status)
    {
        List<PrinterStatusInfo> defaultStatusList = [new PrinterStatusInfo { Status = "Ready", Flags = PrinterStatusFlags.Ready }];

        if (status == 0)
            return defaultStatusList;

        var statuses = new List<PrinterStatusInfo>();
        var statusEnum = (PrinterStatusFlags)status;

        // בדיקת דגלים שונים מתוך המספר הנומרי
        if (statusEnum.HasFlag(PrinterStatusFlags.Paused)) statuses.Add(new PrinterStatusInfo { Status = "Paused", Flags = PrinterStatusFlags.Paused });
        if (statusEnum.HasFlag(PrinterStatusFlags.Error)) statuses.Add(new PrinterStatusInfo { Status = "Error", Flags = PrinterStatusFlags.Error });
        if (statusEnum.HasFlag(PrinterStatusFlags.Offline)) statuses.Add(new PrinterStatusInfo { Status = "Offline", Flags = PrinterStatusFlags.Offline });
        if (statusEnum.HasFlag(PrinterStatusFlags.Printing)) statuses.Add(new PrinterStatusInfo { Status = "Printing", Flags = PrinterStatusFlags.Printing });
        if (statusEnum.HasFlag(PrinterStatusFlags.PaperOut)) statuses.Add(new PrinterStatusInfo { Status = "Out of Paper", Flags = PrinterStatusFlags.PaperOut });
        if (statusEnum.HasFlag(PrinterStatusFlags.PaperJam)) statuses.Add(new PrinterStatusInfo { Status = "Paper Jam", Flags = PrinterStatusFlags.PaperJam });
        if (statusEnum.HasFlag(PrinterStatusFlags.Busy)) statuses.Add(new PrinterStatusInfo { Status = "Busy", Flags = PrinterStatusFlags.Busy });
        if (statusEnum.HasFlag(PrinterStatusFlags.TonerLow)) statuses.Add(new PrinterStatusInfo { Status = "Toner Low", Flags = PrinterStatusFlags.TonerLow });
        if (statusEnum.HasFlag(PrinterStatusFlags.DoorOpen)) statuses.Add(new PrinterStatusInfo { Status = "Door Open", Flags = PrinterStatusFlags.DoorOpen });

        // אם זוהו מצבי דגלים, נחבר אותם; אחרת נחזיר את הקוד המספרי כגיבוי
        return statuses.Count > 0 ? statuses : defaultStatusList;
    }
}
