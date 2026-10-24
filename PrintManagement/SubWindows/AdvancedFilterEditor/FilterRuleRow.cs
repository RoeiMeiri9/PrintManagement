using PrintManagement.Models.Entity;
using PrintManagement.Models.Filter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PrintManagement.SubWindows.AdvancedFilterEditor;

public sealed partial class FilterRuleRow(IReadOnlyList<FilterPropertyInfo> availableProperties) : EntityModel
{
    private FilterPropertyInfo? _property;
    private ConditionOption? _selectedCondition;
    private string _value = string.Empty;

    public IReadOnlyList<FilterPropertyInfo> AvailableProperties { get; } = availableProperties;

    private bool _isResetting;

    public FilterPropertyInfo? Property
    {
        get => _property;
        set
        {
            if (ReferenceEquals(_property, value)) return;
            _property = value;
            OnPropertyChanged();

            _isResetting = true;   // ignore the ComboBoxes writing null back while their lists change
            try
            {
                OnPropertyChanged(nameof(AvailableConditions));
                OnPropertyChanged(nameof(Choices));
                OnPropertyChanged(nameof(HasProperty));
                OnPropertyChanged(nameof(IsText));
                OnPropertyChanged(nameof(IsNumber));
                OnPropertyChanged(nameof(IsDate));
                OnPropertyChanged(nameof(IsChoice));
            }
            finally { _isResetting = false; }

            SelectedCondition = AvailableConditions.FirstOrDefault();
            Value = Choices.FirstOrDefault() ?? string.Empty;
        }
    }

    public IReadOnlyList<ConditionOption> AvailableConditions => _property?.Conditions ?? [];
    public IReadOnlyList<string> Choices => _property?.Choices ?? [];

    public ConditionOption? SelectedCondition
    {
        get => _selectedCondition;
        set
        {
            if (_isResetting || value is null || Equals(_selectedCondition, value)) return;
            _selectedCondition = value;
            OnPropertyChanged();
        }
    }

    public string Value
    {
        get => _value;
        set
        {
            if (_isResetting || value is null) return;
            if (_value == value) return;
            _value = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(NumberValue));
            OnPropertyChanged(nameof(DateValue));
        }
    }

    // Editor visibility
    public bool HasProperty => _property is not null;
    public bool IsText => _property is null || _property.Kind == FilterValueKind.Text;
    public bool IsNumber => _property?.Kind == FilterValueKind.Number;
    public bool IsDate => _property?.Kind == FilterValueKind.Date;
    public bool IsChoice => _property?.Kind == FilterValueKind.Choice;

    // Typed proxies over the string Value
    public double NumberValue
    {
        get => double.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : double.NaN;
        set => Value = double.IsNaN(value) ? string.Empty : value.ToString(CultureInfo.InvariantCulture);
    }

    public DateTimeOffset? DateValue
    {
        get => DateTime.TryParse(Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? new DateTimeOffset(d) : null;
        set => Value = value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    public bool IsComplete => _property is not null && _selectedCondition is not null && _value.Length > 0;
}