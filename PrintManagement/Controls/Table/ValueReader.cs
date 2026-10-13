using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace PrintManagement.Controls.Table;

/// <summary>Reads a (dotted) property path from an item, caching the reflection lookups.</summary>
internal static class ValueReader
{
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> Cache = new();

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

