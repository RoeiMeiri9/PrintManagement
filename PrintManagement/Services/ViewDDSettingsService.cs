using PrintManagement.Resources;
using System;

namespace PrintManagement.Services;

/// <summary>
/// This service is NOT for the general settings.
/// This is used for the View dropdown menu only.
/// </summary>
internal class ViewDDSettingsService
{
    // --- Compact View ---
    private static bool _isCompactView = SettingsService.ReadSetting(
        Settings.CompactViewSetting.Value, Settings.CompactViewSetting.defaultValue);

    public static bool IsCompactView
    {
        get => _isCompactView;
        set
        {
            if (_isCompactView != value)
            {
                _isCompactView = value;
                SettingsService.SaveSetting(Settings.CompactViewSetting.Value, _isCompactView);
                CompactViewChanged?.Invoke(null, _isCompactView);
            }
        }
    }

    public static event EventHandler<bool>? CompactViewChanged;

    // --- Minimal Badge View ---
    private static bool _isColorfulBadgeView = SettingsService.ReadSetting(
        Settings.StatusBadgeAppearanceSetting.Value, Settings.StatusBadgeAppearanceSetting.defaultValue);
    public static bool IsColorfulBadgeView
    {
        get => _isColorfulBadgeView;
        set
        {
            if (_isColorfulBadgeView != value)
            {
                _isColorfulBadgeView = value;
                SettingsService.SaveSetting(Settings.StatusBadgeAppearanceSetting.Value, _isColorfulBadgeView);
                BadgeStyleChanged?.Invoke(null, _isColorfulBadgeView);
            }
        }
    }

    public static event EventHandler<bool>? BadgeStyleChanged;
}
