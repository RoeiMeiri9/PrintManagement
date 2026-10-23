using PrintManagement.Models.Entity.enums;
using PrintManagement.Resources;
using System;
using System.Collections.Generic;

namespace PrintManagement.Services;

/// <summary>Holds one persisted value and raises Changed when it is modified.</summary>
internal sealed class ObservableSetting<T>(Settings<T> key)
{
    private readonly Settings<T> _key = key;
    private T _value = SettingsService.ReadSetting(key);

    public event EventHandler<T>? Changed;

    public T Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value)) return;

            _value = value;
            SettingsService.SaveSetting(_key, value);
            Changed?.Invoke(null, value);
        }
    }
}

/// <summary>
/// This service is NOT for the general Settings<bool>.
/// This is used for the View dropdown menu only.
/// </summary>
internal static class ViewDDSettingsService
{
    private static readonly ObservableSetting<bool> _compactView = new(Settings<bool>.CompactViewSetting);
    private static readonly ObservableSetting<bool> _colorfulBadge = new(Settings<bool>.StatusBadgeAppearanceSetting);
    private static readonly ObservableSetting<ViewMode> _viewMode_Filter = new(Settings<ViewMode>.CurrentViewSetting_Filter);
    private static readonly ObservableSetting<double> _iconSizeFilter = new(Settings<double>.IconSizeSetting_Filter);

    public static bool IsCompactView
    {
        get => _compactView.Value;
        set => _compactView.Value = value;
    }
    public static event EventHandler<bool>? CompactViewChanged
    {
        add => _compactView.Changed += value;
        remove => _compactView.Changed -= value;
    }

    public static bool IsColorfulBadgeView
    {
        get => _colorfulBadge.Value;
        set => _colorfulBadge.Value = value;
    }
    public static event EventHandler<bool>? BadgeStyleChanged
    {
        add => _colorfulBadge.Changed += value;
        remove => _colorfulBadge.Changed -= value;
    }

    public static ViewMode CurrentViewMode_Filter
    {
        get => _viewMode_Filter.Value;
        set => _viewMode_Filter.Value = value;
    }
    public static event EventHandler<ViewMode>? ViewMode_FilterChanged
    {
        add => _viewMode_Filter.Changed += value;
        remove => _viewMode_Filter.Changed -= value;
    }

    public static double CurrentIconSize_Filter
    {
        get => _iconSizeFilter.Value;
        set => _iconSizeFilter.Value = value;
    }
    public static event EventHandler<double>? IconSize_FilterChanged
    {
        add => _iconSizeFilter.Changed += value;
        remove => _iconSizeFilter.Changed -= value;
    }
}