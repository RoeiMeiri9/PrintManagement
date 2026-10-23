namespace PrintManagement.Models.Filter;

public class FilterRule
{
    public FilterCondition Condition { get; set; } = FilterCondition.Contains;
    public string Value { get; set; } = string.Empty;
}
