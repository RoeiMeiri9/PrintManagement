using System;

namespace PrintManagement.Services;

/// <summary>
/// This service is NOT for the general settings.
/// This is used for the View dropdown menu only.
/// </summary>
internal class ViewDDSettingsService
{
    private const string CompactViewKey = "IsCompactView";
    private static bool _isCompactView = SettingsService.ReadSetting<bool>(CompactViewKey);

    public static bool IsCompactView
    {
        get => _isCompactView;
        set
        {
            if (_isCompactView != value)
            {
                _isCompactView = value;
                SettingsService.SaveSetting(CompactViewKey, _isCompactView);
                CompactViewChanged?.Invoke(null, _isCompactView);
            }
        }
    }

    public static event EventHandler<bool>? CompactViewChanged;
}
