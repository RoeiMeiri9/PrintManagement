using System.Collections.Generic;

namespace PrintManagement.Models.Filter;

public sealed class FilterPropertyInfo
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required FilterValueKind Kind { get; init; }
    public IReadOnlyList<string> Choices { get; init; } = [];
    public required IReadOnlyList<ConditionOption> Conditions { get; init; }
}