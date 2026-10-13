using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PrinterManager.Resources;
using PrinterManager.Services;
using Windows.ApplicationModel;

namespace PrinterManager.Pages.SettingsPage;

public sealed partial class SettingsPage : Page
{
    public string AppName => Package.Current.DisplayName;
    private PackageVersion AppVersion => Package.Current.Id.Version;
    public string appVersion;

    public SettingsPage()
    {
        appVersion = $"Version {AppVersion.Major}.{AppVersion.Minor}.{AppVersion.Build}.{AppVersion.Revision}";
        InitializeComponent();
        LoadSettings();
    }
    private void LoadSettings()
    {
        bool isEnabled = SettingsService.ReadSetting(Settings.ScrollIndicatorSetting.Value, false);
        ScrollIndicatorToggle.IsOn = isEnabled;
    }

    private void ScrollIndicatorToggle_Toggled(object sender, RoutedEventArgs e)
    {
        SettingsService.SaveSetting(Settings.ScrollIndicatorSetting.Value, ScrollIndicatorToggle.IsOn);
    }
}
