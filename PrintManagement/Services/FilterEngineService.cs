using PrintManagement.Models.Filter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace PrintManagement.Services;

/// <summary>
/// Applies a <see cref="GenericFilterDefinition"/> to a list of entities.
/// Every entry in <c>Criteria</c> must match (AND). An empty <c>Criteria</c> matches everything.
/// Column names are entity property names, case-insensitive.
///
/// A rule that cannot be evaluated (unknown column, a value that can't be parsed, a condition that
/// doesn't make sense for the column type) makes the whole filter match nothing, so a broken filter
/// is visible instead of silently showing everything. Use <see cref="Validate{T}"/> to get the reasons.
/// </summary>
public static class FilterEngine
{
    private const DynamicallyAccessedMemberTypes PublicProps = DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties;

    public static IEnumerable<T> Apply<[DynamicallyAccessedMembers(PublicProps)] T>(
        IEnumerable<T> source, GenericFilterDefinition filter)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Where(BuildPredicate<T>(filter));
    }

    public static int Count<[DynamicallyAccessedMembers(PublicProps)] T>(
        IEnumerable<T> source, GenericFilterDefinition filter)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Count(BuildPredicate<T>(filter));
    }


    /// <summary>
    /// Resolves columns and parses values once, then returns a cheap predicate.
    /// Build it once per filter and reuse it for many items.
    /// </summary>
    public static Func<T, bool> BuildPredicate<[DynamicallyAccessedMembers(PublicProps)] T>(
        GenericFilterDefinition filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var tests = new List<(PropertyInfo Property, Func<object?, bool> Match)>();
        foreach (var (column, rule) in filter.Criteria)
        {
            var resolved = Resolve<T>(column, rule); // <-- שינוי כאן
            if (resolved.Property is null || resolved.Matcher is null)
                return static _ => false;

            tests.Add((resolved.Property, resolved.Matcher));
        }

        if (tests.Count == 0)
            return static _ => true;

        return item =>
        {
            if (item is null) return false;

            foreach (var (property, match) in tests)
            {
                if (!match(property.GetValue(item)))
                    return false;
            }
            return true;
        };
    }

    /// <summary>Human-readable problems that stop the filter from working. Empty means the filter is valid.</summary>
    public static IReadOnlyList<string> Validate<[DynamicallyAccessedMembers(PublicProps)] T>(
        GenericFilterDefinition filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var errors = new List<string>();
        foreach (var (column, rule) in filter.Criteria)
        {
            var error = Resolve<T>(column, rule).Error; // <-- שינוי כאן
            if (error is not null)
                errors.Add(error);
        }
        return errors;
    }

    // ---- internals ----

    private readonly record struct ResolvedRule(PropertyInfo? Property, Func<object?, bool>? Matcher, string? Error);

    private static ResolvedRule Resolve<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] T>(
         string column, FilterRule rule)
    {
        if (rule is null)
            return new(null, null, $"'{column}': the rule is empty.");

        var entityType = typeof(T);
        var property = entityType.GetProperty(
            column, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (property is null || property.GetIndexParameters().Length > 0)
            return new(null, null, $"'{column}' is not a property of {entityType.Name}.");

        var matcher = CreateMatcher(property.PropertyType, rule, out var error);
        return new(property, matcher, error is null ? null : $"'{column}': {error}");
    }

    private static Func<object?, bool>? CreateMatcher(Type propertyType, FilterRule rule, out string? error)
    {
        error = null;
        var type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        var condition = rule.Condition;
        var text = rule.Value ?? string.Empty;

        // Text: case-insensitive comparisons.
        if (type == typeof(string))
            return TextMatcher(condition, text);

        // Lists (e.g. QueueStatus): matches when ANY element's text matches.
        // NotEquals means "no element matches". What "text" means for an element depends on its ToString().
        if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            if (condition is (FilterCondition.GreaterThan or FilterCondition.LessThan))
            {
                error = $"{condition} is not supported for a list.";
                return null;
            }

            var negate = condition == FilterCondition.NotEquals;
            var element = TextMatcher(negate ? FilterCondition.Equals : condition, text);
            return value =>
            {
                var any = value is IEnumerable items && items.Cast<object?>().Any(element);
                return negate ? !any : any;
            };
        }

        // Enums can also be searched by name: Contains / StartsWith.
        if (type.IsEnum && condition is (FilterCondition.Contains or FilterCondition.StartsWith))
            return TextMatcher(condition, text);

        // Typed values: parse the rule's value once, then compare.
        Func<object, int> compare;
        var orderable = true;

        if (type == typeof(bool))
        {
            if (!bool.TryParse(text, out var target))
            {
                error = $"'{text}' is not true or false.";
                return null;
            }
            compare = v => v is bool b && b == target ? 0 : 1;
            orderable = false;
        }
        else if (type.IsEnum)
        {
            if (!Enum.TryParse(type, text, ignoreCase: true, out var target))
            {
                error = $"'{text}' is not a valid {type.Name} value.";
                return null;
            }
            compare = v => object.Equals(v, target) ? 0 : 1;
            orderable = false;
        }
        else if (type == typeof(DateTime))
        {
            if (!DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var target))
            {
                error = $"'{text}' is not a valid date.";
                return null;
            }
            // "2024-05-01" with no time means the whole day.
            var wholeDay = target.TimeOfDay == TimeSpan.Zero;
            compare = v => wholeDay
                ? ((DateTime)v).Date.CompareTo(target)
                : ((DateTime)v).CompareTo(target);
        }
        else if (IsNumeric(type))
        {
            if (!decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var target))
            {
                error = $"'{text}' is not a valid number.";
                return null;
            }
            compare = v => Convert.ToDecimal(v, CultureInfo.InvariantCulture).CompareTo(target);
        }
        else
        {
            error = $"Properties of type {type.Name} can't be filtered.";
            return null;
        }

        var unsupported = condition switch
        {
            FilterCondition.Contains or FilterCondition.StartsWith => true,
            FilterCondition.GreaterThan or FilterCondition.LessThan => !orderable,
            _ => false
        };
        if (unsupported)
        {
            error = $"{condition} is not supported for {type.Name}.";
            return null;
        }

        return value =>
        {
            if (value is null)
                return condition == FilterCondition.NotEquals;

            var result = compare(value);
            return condition switch
            {
                FilterCondition.Equals => result == 0,
                FilterCondition.NotEquals => result != 0,
                FilterCondition.GreaterThan => result > 0,
                FilterCondition.LessThan => result < 0,
                _ => false
            };
        };
    }

    private static Func<object?, bool> TextMatcher(FilterCondition condition, string text) => condition switch
    {
        FilterCondition.Equals => v => string.Equals(AsText(v), text, StringComparison.OrdinalIgnoreCase),
        FilterCondition.NotEquals => v => !string.Equals(AsText(v), text, StringComparison.OrdinalIgnoreCase),
        FilterCondition.Contains => v => AsText(v).Contains(text, StringComparison.OrdinalIgnoreCase),
        FilterCondition.StartsWith => v => AsText(v).StartsWith(text, StringComparison.OrdinalIgnoreCase),
        FilterCondition.GreaterThan => v => string.Compare(AsText(v), text, StringComparison.OrdinalIgnoreCase) > 0,
        FilterCondition.LessThan => v => string.Compare(AsText(v), text, StringComparison.OrdinalIgnoreCase) < 0,
        _ => static _ => false
    };

    private static string AsText(object? value) => value?.ToString() ?? string.Empty;

    private static bool IsNumeric(Type type) => Type.GetTypeCode(type) switch
    {
        TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or
        TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or
        TypeCode.Single or TypeCode.Double or TypeCode.Decimal => true,
        _ => false
    };
}