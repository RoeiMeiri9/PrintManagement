namespace PrintManagement.Resources;

public record Settings(string Value, bool defaultValue)
{
    public static readonly Settings ScrollIndicatorSetting = new("ShowScrollDirectionIndicator", false);
    public static readonly Settings StatusBadgeAppearanceSetting = new("StatusBadgeAppearance", true);
    public static readonly Settings UseISODateFormatSetting = new("UseISODateFormat", true);
    public static readonly Settings WriteTrueFalseLowercaseSetting = new("WriteTrueFalseLowercase", true);
    public static readonly Settings ShowNotSharedSetting = new("ShowNotShared", false);

    // --- View Dropdown Settings ---
    public static readonly Settings CompactViewSetting = new("CompactView", false);
}