// SpoolerService.Pulse.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Win32;

namespace PrintManagement.Services;

public readonly record struct PrinterPulse(string Name, uint Status, uint Jobs);

public static unsafe partial class SpoolerService
{
    /// <summary>Light status/job-count poll. Returns null on failure (never prompts for credentials).</summary>
    public static Task<IReadOnlyList<PrinterPulse>?> TryGetPrinterPulseAsync(
        string? serverName = null, bool includeConnections = true)
    {
        var creds = string.IsNullOrWhiteSpace(serverName)
            ? null
            : CredentialsService.GetStoredCredentials(serverName);

        return RunAsync<IReadOnlyList<PrinterPulse>?>(creds, () =>
        {
            try
            {
                return GetPrinterPulse(serverName, includeConnections);
            }
            catch (Exception ex)
            {
                Logger.Warning($"Pulse failed: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        });
    }

    private static IReadOnlyList<PrinterPulse> GetPrinterPulse(string? serverName, bool includeConnections)
    {
        uint flags = serverName is null
            ? PRINTER_ENUM_LOCAL | (includeConnections ? PRINTER_ENUM_CONNECTIONS : 0)
            : PRINTER_ENUM_NAME;

        return QueryBuffer<IReadOnlyList<PrinterPulse>>(
            (buf, size, out needed, out count) =>
                PInvoke.EnumPrinters(flags, serverName, 2, buf, size, out needed, out count),
            (buf, count) =>
            {
                var list = new List<PrinterPulse>((int)count);
                var items = (PRINTER_INFO_2*)buf;
                for (uint i = 0; i < count; i++)
                {
                    string raw = S(items[i].pPrinterName);
                    string server = S(items[i].pServerName);

                    // same name normalization as ParsePrinters, so names match your models
                    string name = serverName is not null && server.Length > 0 &&
                                  raw.StartsWith(server + "\\", StringComparison.OrdinalIgnoreCase)
                        ? raw[(server.Length + 1)..]
                        : raw;

                    list.Add(new PrinterPulse(name, items[i].Status, items[i].cJobs));
                }
                return list;
            },
            []);
    }
}