using PrintManagement.Models.Filter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PrintManagement.SubWindows.AdvancedFilterEditor;

public sealed class FilterDraft
{
    public FilterDraft(Type entityType, GenericFilterDefinition? existing = null)
    {
        EntityType = entityType;
        Properties = FilterPropertyCatalog.For(entityType);
        Definition = existing ?? new GenericFilterDefinition();

        // Editing an existing filter: turn its stored rules back into rows
        foreach (var (key, rule) in Definition.Criteria)
        {
            var prop = Properties.FirstOrDefault(p =>
                string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase));
            if (prop is null) continue;   // property was removed from the entity

            // Order matters: setting Property resets the condition and value
            var row = new FilterRuleRow(Properties)
            {
                Property = prop,
                SelectedCondition = prop.Conditions.FirstOrDefault(c => c.Value == rule.Condition),
                Value = rule.Value
            };
            Rules.Add(row);
        }
    }

    public Type EntityType { get; }
    public IReadOnlyList<FilterPropertyInfo> Properties { get; }
    public GenericFilterDefinition Definition { get; }
    public ObservableCollection<FilterRuleRow> Rules { get; } = [];

    // Pass-throughs so stage 1 can bind to the draft
    public string Name
    {
        get => Definition.Name;
        set => Definition.Name = value;
    }

    public string? Description
    {
        get => Definition.Description;
        set => Definition.Description = value;
    }

    public bool DisplayTotalItems
    {
        get => Definition.DisplayTotalItems ?? false;
        set => Definition.DisplayTotalItems = value;
    }

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return "Give the filter a name.";
        if (Rules.Count == 0 || Rules.Any(r => !r.IsComplete))
            return "Complete or remove the empty conditions.";

        var dup = Rules.GroupBy(r => r.Property!.Name).FirstOrDefault(g => g.Count() > 1);
        return dup is null
            ? null
            : $"“{dup.First().Property!.DisplayName}” is used twice. Each property can have one condition.";
    }

    /// <summary>Writes the rows into the definition and returns it, ready for SaveAsync.</summary>
    public GenericFilterDefinition Build()
    {
        Definition.Name = Definition.Name.Trim();
        Definition.Criteria = Rules.ToDictionary(
            r => r.Property!.Name,
            r => new FilterRule { Condition = r.SelectedCondition!.Value, Value = r.Value });
        return Definition;
    }
}