using System;
using System.Collections.Generic;

namespace PrintManagement.Models.Filter;

public class GenericFilterDefinition
{
    private Dictionary<string, FilterRule> _criteria = [with(StringComparer.OrdinalIgnoreCase)];

    public Guid ID { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool? DisplayTotalItems { get; set; }


    // The setter re-wraps whatever it receives in a case-insensitive dictionary.
    // Without this, System.Text.Json replaces the property with a plain
    // (case-sensitive) Dictionary on deserialization and the comparer is lost.
    public Dictionary<string, FilterRule> Criteria
    {
        get => _criteria;
        set => _criteria = value is null
            ? new(StringComparer.OrdinalIgnoreCase)
            : new(value, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Property names (same keys as <see cref="Criteria"/>) of columns that should be hidden
    /// when this filter is applied. Columns not listed here are shown, so properties added
    /// to the entity later appear automatically.
    /// </summary>
    public List<string> HiddenColumns { get; set; } = [];
}