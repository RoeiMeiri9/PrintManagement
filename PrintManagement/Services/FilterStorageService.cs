using PrintManagement.Models.Entity;
using PrintManagement.Models.Filter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PrintManagement.Services;

/// <summary>
/// Reads and saves <see cref="GenericFilterDefinition"/>s as JSON, one file per entity type:<br />
/// <list type="bullet">
///     <item> %LocalAppData%\PrintManagement\Filters\Printer.json </item>
///     <item> %LocalAppData%\PrintManagement\Filters\Driver.json </item>
/// </list>
/// Each file holds a list of named filters for that entity.
/// </summary>
public sealed class FilterStorageService(string folder)
{
    private readonly string _folder = folder;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public FilterStorageService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PrintManagement", "Filters"))
    {
    }

    public async Task<IReadOnlyList<GenericFilterDefinition>> GetAllAsync<TEntity>(CancellationToken ct = default)
        where TEntity : EntityModel
    {
        await _lock.WaitAsync(ct);
        try
        {
            var store = await ReadStoreAsync(PathFor<TEntity>(), ct);
            return store.Filters;
        }
        finally { _lock.Release(); }
    }

    public async Task<GenericFilterDefinition?> GetAsync<TEntity>(Guid id, CancellationToken ct = default)
        where TEntity : EntityModel
    {
        var all = await GetAllAsync<TEntity>(ct);
        return all.FirstOrDefault(f => f.ID == id);
    }

    /// <summary>Adds the filter, or replaces the existing one with the same Id.</summary>
    public async Task SaveAsync<TEntity>(GenericFilterDefinition filter, CancellationToken ct = default)
        where TEntity : EntityModel
    {
        ArgumentNullException.ThrowIfNull(filter);

        await _lock.WaitAsync(ct);
        try
        {
            var path = PathFor<TEntity>();
            var store = await ReadStoreAsync(path, ct);

            var index = store.Filters.FindIndex(f => f.ID == filter.ID);
            if (index >= 0) store.Filters[index] = filter;
            else store.Filters.Add(filter);

            await WriteStoreAsync(path, store, ct);
        }
        finally { _lock.Release(); }
    }

    /// <returns>True if a filter was removed.</returns>
    public async Task<bool> DeleteAsync<TEntity>(Guid id, CancellationToken ct = default)
        where TEntity : EntityModel
    {
        await _lock.WaitAsync(ct);
        try
        {
            var path = PathFor<TEntity>();
            var store = await ReadStoreAsync(path, ct);

            if (store.Filters.RemoveAll(f => f.ID == id) == 0)
                return false;

            await WriteStoreAsync(path, store, ct);
            return true;
        }
        finally { _lock.Release(); }
    }

    // ---- internals ----

    private string PathFor<TEntity>() where TEntity : EntityModel
    {
        // PrinterModel -> Printer.json, DriverModel -> Driver.json
        var name = typeof(TEntity).Name;
        if (name.EndsWith("Model", StringComparison.Ordinal) && name.Length > "Model".Length)
            name = name[..^"Model".Length];

        return Path.Combine(_folder, name + ".json");
    }

    private static async Task<FilterStore> ReadStoreAsync(string path, CancellationToken ct)
    {
        if (!File.Exists(path))
            return new FilterStore();

        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync(stream, FilterJsonContext.Default.FilterStore, ct)
                   ?? new FilterStore();
        }
        catch (JsonException)
        {
            // Hand-edited or damaged file: keep it for inspection instead of crashing or silently overwriting it.
            File.Move(path, path + ".corrupt", overwrite: true);
            return new FilterStore();
        }
    }

    private async Task WriteStoreAsync(string path, FilterStore store, CancellationToken ct)
    {
        Directory.CreateDirectory(_folder);
        store.Version = FilterStore.CurrentVersion;

        // Write to a temp file first so a crash mid-write can't leave a half-written filters file.
        var tempPath = path + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, store, FilterJsonContext.Default.FilterStore, ct);
        }
        File.Move(tempPath, path, overwrite: true);
    }
}

/// <summary>On-disk shape. The Version field leaves room for migrations later.</summary>
internal sealed class FilterStore
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;
    public List<GenericFilterDefinition> Filters { get; set; } = [];
}

/// <summary>
/// Source-generated serializer metadata: no reflection, so it is safe for trimming / Native AOT.
/// Enums are written as names ("Contains") rather than numbers, so reordering FilterCondition can't break saved files.
/// GenericFilterDefinition, FilterRule and FilterCondition are picked up automatically as FilterStore's members.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(FilterStore))]
internal sealed partial class FilterJsonContext : JsonSerializerContext
{
}