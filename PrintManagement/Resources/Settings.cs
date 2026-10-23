using PrintManagement.Models.Entity.enums;
using PrintManagement.Models.Entity.Statics;

namespace PrintManagement.Resources;

public record Settings<T>(string Key, T DefaultValue)
{
    public static readonly Settings<bool> ScrollIndicatorSetting = new("ShowScrollDirectionIndicator", false);
    public static readonly Settings<bool> StatusBadgeAppearanceSetting = new("StatusBadgeAppearance", true);
    public static readonly Settings<int> RefreshSpeedSetting = new("RefreshSpeed", 5);
    public static readonly Settings<bool> UseISODateFormatSetting = new("UseISODateFormat", true);
    public static readonly Settings<bool> WriteTrueFalseLowercaseSetting = new("WriteTrueFalseLowercase", true);
    public static readonly Settings<bool> ShowNotSharedSetting = new("ShowNotShared", false);

    // --- View Dropdown Settings ---
    public static readonly Settings<bool> CompactViewSetting = new("CompactView", false);
    public static readonly Settings<ViewMode> CurrentViewSetting_Filter = new("CurrentViewSetting_Filter", ViewMode.Table);
    public static readonly Settings<double> IconSizeSetting_Filter = new("IconSizeSetting_Filter", StaticValues.SmallIcons);
}