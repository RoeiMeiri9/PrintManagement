using System.Diagnostics;

namespace PrintManagement.Helpers.PerformanceMonitorHelpers;

public class StepTimer
{
    private readonly Stopwatch _sw = Stopwatch.StartNew();
    private long _lastTicks = 0;

    public void LogStep(string stepName)
    {
        long current = _sw.ElapsedMilliseconds;
        long diff = current - _lastTicks;

        Debug.WriteLine($"[Step] {stepName}: Took {diff} ms (Total: {current} ms)");
        _lastTicks = current;
    }
    public void Mark(string label) =>
        Debug.WriteLine($"[{_sw.ElapsedMilliseconds,5} ms] {label}");
}
