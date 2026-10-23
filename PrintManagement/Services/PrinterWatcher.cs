using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PrintManagement.Services;

public sealed class PrinterWatcher(string? serverName, TimeSpan interval)
{
    private TimeSpan _interval = interval;
    private PeriodicTimer? _timer;

    public TimeSpan Interval
    {
        get => _interval;
        set
        {
            _interval = value;
            try { _timer?.Period = value; }
            catch (ObjectDisposedException) { }   // watcher is shutting down
        }
    }

    private Dictionary<string, PrinterPulse> _last = [with(StringComparer.OrdinalIgnoreCase)];
    private bool _hasBaseline;

    public event Action<IReadOnlyList<PrinterPulse>>? Changed;   // raised on a background thread

    public async Task RunAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_interval);
        _timer = timer;
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                var now = await SpoolerService.TryGetPrinterPulseAsync(serverName);
                if (now is null) continue;   // failed poll: keep the last state

                var changed = now
                    .Where(p => !_last.TryGetValue(p.Name, out var old) || old != p)
                    .ToList();

                _last = now.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

                // The first successful poll only fills the baseline.
                if (!_hasBaseline)
                {
                    _hasBaseline = true;
                    continue;
                }

                if (changed.Count > 0 && !ct.IsCancellationRequested)
                    Changed?.Invoke(changed);
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown when the page cancels the token
        }
        finally { _timer = null; }
    }
}