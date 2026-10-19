using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace PrintManagement.Controls.Table;

/// <summary>
/// Reflection helper that extracts property values from data models dynamically at runtime.
/// Caches lookup results for efficient sorting and data export operations.
/// </summary>
internal static class ValueReader
{
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> Cache = new();

    /// <summary>
    /// Reads a property value from a target object using a property path (supports nested paths, e.g., "Driver.Name").
    /// </summary>
    /// <param name="obj">The source object instance (typically <see cref="TableRow.Item"/>).</param>
    /// <param name="path">The property name or dotted property path to evaluate.</param>
    /// <returns>The property value if found; otherwise, <c>null</c>.</returns>
    public static object? Read(object? obj, string path)
    {
        foreach (var part in path.Split('.'))
        {
            if (obj == null) return null;
            var prop = Cache.GetOrAdd((obj.GetType(), part), k => k.Item1.GetProperty(k.Item2));
            obj = prop?.GetValue(obj);
        }
        return obj;
    }
}

