using System;

namespace PrintManagement.Pages.CustomFilters.Helpers;

[Flags]
public enum IsolationFlag : int
{
    Unknown = -1,
    None = 0,
    Isolation = 1,
    Shared = 2
}

public record DriverIsolationInfo
{
    public string Isolation { get; set; } = string.Empty;
    public IsolationFlag Flag { get; set; }
}

internal class DriverIsolationHelper
{
    public static DriverIsolationInfo FormatDriverIsolation(IsolationFlag isolation)
    {
        return isolation switch
        {
            IsolationFlag.Unknown => new DriverIsolationInfo { Isolation = "Unknown", Flag = IsolationFlag.Unknown },
            IsolationFlag.None => new DriverIsolationInfo { Isolation = "None", Flag = IsolationFlag.None },
            IsolationFlag.Isolation => new DriverIsolationInfo { Isolation = "Isolation", Flag = IsolationFlag.Isolation },
            IsolationFlag.Shared => new DriverIsolationInfo { Isolation = "Shared", Flag = IsolationFlag.Shared },
            _ => new DriverIsolationInfo { Isolation = "Unknown", Flag = IsolationFlag.Unknown }
        };
    }
}
