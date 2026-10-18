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
        ScrollIndicatorToggle.IsOn = SettingsService.ReadSetting(Settings.ScrollIndicatorSetting.Value, Settings.ScrollIndicatorSetting.defaultValue);
        UseISODateFormatToggle.IsOn = SettingsService.ReadSetting(Settings.UseISODateFormatSetting.Value, Settings.UseISODateFormatSetting.defaultValue);
        WriteTrueFalseLowercaseToggle.IsOn = SettingsService.ReadSetting(Settings.WriteTrueFalseLowercaseSetting.Value, Settings.WriteTrueFalseLowercaseSetting.defaultValue);
        ShowNotSharedToggle.IsOn = SettingsService.ReadSetting(Settings.ShowNotSharedSetting.Value, Settings.ShowNotSharedSetting.defaultValue);
    }

    private void ScrollIndicatorToggle_Toggled(object sender, RoutedEventArgs e)
    {
        SettingsService.SaveSetting(Settings.ScrollIndicatorSetting.Value, ScrollIndicatorToggle.IsOn);
    }

    private void UseISODateFormatToggle_Toggled(object sender, RoutedEventArgs e)
    {
        SettingsService.SaveSetting(Settings.UseISODateFormatSetting.Value, UseISODateFormatToggle.IsOn);
    }

    private void WriteTrueFalseLowercaseToggle_Toggled(object sender, RoutedEventArgs e)
    {
        SettingsService.SaveSetting(Settings.WriteTrueFalseLowercaseSetting.Value, WriteTrueFalseLowercaseToggle.IsOn);
    }

    private void ShowNotSharedToggle_Toggled(object sender, RoutedEventArgs e)
    {
        SettingsService.SaveSetting(Settings.ShowNotSharedSetting.Value, ShowNotSharedToggle.IsOn);
    }
}
