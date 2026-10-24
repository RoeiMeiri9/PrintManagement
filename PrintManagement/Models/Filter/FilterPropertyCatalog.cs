using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace PrintManagement.Models.Filter;

public static class FilterPropertyCatalog
{
    private static readonly FilterCondition[] TextSet =
        [FilterCondition.Equals, FilterCondition.NotEquals, FilterCondition.Contains, FilterCondition.StartsWith];
    private static readonly FilterCondition[] OrderedSet =
        [FilterCondition.Equals, FilterCondition.NotEquals, FilterCondition.GreaterThan, FilterCondition.LessThan];
    private static readonly FilterCondition[] ChoiceSet =
        [FilterCondition.Equals, FilterCondition.NotEquals];

    public static IReadOnlyList<FilterPropertyInfo> For(Type entityType) => [.. entityType
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
        .Select(Describe)
        .OrderBy(p => p.DisplayName)];

    private static FilterPropertyInfo Describe(PropertyInfo p)
    {
        var type = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
        string[] choices = [];
        FilterValueKind kind;

        if (type == typeof(bool))
        {
            kind = FilterValueKind.Choice;
            choices = ["True", "False"];
        }
        else if (type.IsEnum)
        {
            kind = FilterValueKind.Choice;
            choices = Enum.GetNames(type);
        }
        else if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            kind = FilterValueKind.Date;
        else if (Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.Decimal)
            kind = FilterValueKind.Number;
        else
            kind = FilterValueKind.Text;   // string, and collections like List<PrinterStatusInfo>

        var set = kind switch
        {
            FilterValueKind.Text => TextSet,
            FilterValueKind.Choice => ChoiceSet,
            _ => OrderedSet
        };

        return new FilterPropertyInfo
        {
            Name = p.Name,
            DisplayName = Regex.Replace(p.Name, "(?<=[a-z])(?=[A-Z])", " "),
            Kind = kind,
            Choices = choices,
            Conditions = set.Select(c => new ConditionOption(c, Label(c, kind))).ToList()
        };
    }

    private static string Label(FilterCondition c, FilterValueKind kind) => c switch
    {
        FilterCondition.Equals => "equals",
        FilterCondition.NotEquals => "does not equal",
        FilterCondition.Contains => "contains",
        FilterCondition.StartsWith => "starts with",
        FilterCondition.GreaterThan => kind == FilterValueKind.Date ? "is after" : "is greater than",
        FilterCondition.LessThan => kind == FilterValueKind.Date ? "is before" : "is less than",
        _ => c.ToString()
    };
}
