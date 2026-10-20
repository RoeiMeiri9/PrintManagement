using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PrintManagement.Resources;
using PrintManagement.Services;
using Windows.ApplicationModel;

namespace PrintManagement.Pages.SettingsPage;

public sealed partial class SettingsPage : Page
{
    public string AppName => Package.Current.DisplayName;
    private static PackageVersion AppVersion => Package.Current.Id.Version;
    public string appVersion;

    public SettingsPage()
    {
        appVersion = $"Version {AppVersion.Major}.{AppVersion.Minor}.{AppVersion.Build}.{AppVersion.Revision}";
        InitializeComponent();
        LoadSettings();
    }
    private void LoadSettings()
    {
        ScrollIndicatorToggle.IsOn = SettingsService.ReadSetting(Settings<bool>.ScrollIndicatorSetting);
        StatusBadgeAppearanceToggle.IsOn = SettingsService.ReadSetting(Settings<bool>.StatusBadgeAppearanceSetting);
        UseISODateFormatToggle.IsOn = SettingsService.ReadSetting(Settings<bool>.UseISODateFormatSetting);
        WriteTrueFalseLowercaseToggle.IsOn = SettingsService.ReadSetting(Settings<bool>.WriteTrueFalseLowercaseSetting);
        ShowNotSharedToggle.IsOn = SettingsService.ReadSetting(Settings<bool>.ShowNotSharedSetting);
    }

    private void ScrollIndicatorToggle_Toggled(object sender, RoutedEventArgs e)
    {
        SettingsService.SaveSetting(Settings<bool>.ScrollIndicatorSetting, ScrollIndicatorToggle.IsOn);
    }

    private void StatusBadgeAppearanceToggle_Toggled(object sender, RoutedEventArgs e)
    {
        ViewDDSettingsService.IsColorfulBadgeView = StatusBadgeAppearanceToggle.IsOn;
    }

    private void UseISODateFormatToggle_Toggled(object sender, RoutedEventArgs e)
    {
        SettingsService.SaveSetting(Settings<bool>.UseISODateFormatSetting, UseISODateFormatToggle.IsOn);
    }

    private void WriteTrueFalseLowercaseToggle_Toggled(object sender, RoutedEventArgs e)
    {
        SettingsService.SaveSetting(Settings<bool>.WriteTrueFalseLowercaseSetting, WriteTrueFalseLowercaseToggle.IsOn);
    }

    private void ShowNotSharedToggle_Toggled(object sender, RoutedEventArgs e)
    {
        SettingsService.SaveSetting(Settings<bool>.ShowNotSharedSetting, ShowNotSharedToggle.IsOn);
    }
}
