using PrintManagement.Models.enums;
using PrintManagement.Resources;
using System;

namespace PrintManagement.Services;

/// <summary>
/// This service is NOT for the general Settings<bool>.
/// This is used for the View dropdown menu only.
/// </summary>
internal class ViewDDSettingsService
{
    // --- Compact View ---
    private static bool _isCompactView = SettingsService.ReadSetting(
        Settings<bool>.CompactViewSetting);

    public static bool IsCompactView
    {
        get => _isCompactView;
        set
        {
            if (_isCompactView != value)
            {
                _isCompactView = value;
                SettingsService.SaveSetting(Settings<bool>.CompactViewSetting, _isCompactView);
                CompactViewChanged?.Invoke(null, _isCompactView);
            }
        }
    }

    public static event EventHandler<bool>? CompactViewChanged;

    // --- Minimal Badge View ---
    private static bool _isColorfulBadgeView = SettingsService.ReadSetting(
        Settings<bool>.StatusBadgeAppearanceSetting);
    public static bool IsColorfulBadgeView
    {
        get => _isColorfulBadgeView;
        set
        {
            if (_isColorfulBadgeView != value)
            {
                _isColorfulBadgeView = value;
                SettingsService.SaveSetting(Settings<bool>.StatusBadgeAppearanceSetting, _isColorfulBadgeView);
                BadgeStyleChanged?.Invoke(null, _isColorfulBadgeView);
            }
        }
    }

    public static event EventHandler<bool>? BadgeStyleChanged;

    private static ViewMode _currentViewMode = SettingsService.ReadSetting(Settings<ViewMode>.CurrentViewSetting_Filter);

    public static ViewMode CurrentViewMode
    {
        get => _currentViewMode;
        set
        {
            if (_currentViewMode != value)
            {
                _currentViewMode = value;
                SettingsService.SaveSetting(Settings<ViewMode>.CurrentViewSetting_Filter, _currentViewMode);
                ViewModeChanged?.Invoke(null, _currentViewMode);
            }
        }
    }

    public static event EventHandler<ViewMode>? ViewModeChanged;
}
