using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PrintManagement.Helpers.PerformanceMonitorHelpers;

internal static class LeakCheck
{
    private static readonly List<(string Name, WeakReference Ref)> Tracked = [];

    [Conditional("DEBUG")]
    public static void Track(object o)
    {
        lock (Tracked) Tracked.Add((o.GetType().Name, new WeakReference(o)));
    }

    [Conditional("DEBUG")]
    public static void Report()
    {
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        lock (Tracked)
        {
            foreach (var g in Tracked.GroupBy(t => t.Name))
                Debug.WriteLine($"[Leak] {g.Key}: created {g.Count()}, still alive {g.Count(t => t.Ref.IsAlive)}");
        }
    }
}