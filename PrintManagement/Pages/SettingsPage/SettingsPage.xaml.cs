using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using PrintManagement.Converters;
using PrintManagement.Resources;
using PrintManagement.Services;
using System;
using Windows.ApplicationModel;

namespace PrintManagement.Pages.SettingsPage;

public sealed partial class SettingsPage : Page
{
    public string AppName => Package.Current.DisplayName;
    private static PackageVersion AppVersion => Package.Current.Id.Version;
    public string appVersion;
    private readonly (string Text, int Seconds)[] _intervalOptions = [("2s", 2), ("5s", 5), ("10s", 10), ("30s", 10), ("1 min", 60), ("Off", -1)];

    public SettingsPage()
    {
        appVersion = $"Version {AppVersion.Major}.{AppVersion.Minor}.{AppVersion.Build}.{AppVersion.Revision}";
        InitializeComponent();
        RefreshSpeedSlider.ThumbToolTipValueConverter = new IntervalTooltipConverter(_intervalOptions);
        LoadSettings();
    }
    private void LoadSettings()
    {
        ScrollIndicatorToggle.IsOn = SettingsService.ReadSetting(Settings<bool>.ScrollIndicatorSetting);
        StatusBadgeAppearanceToggle.IsOn = SettingsService.ReadSetting(Settings<bool>.StatusBadgeAppearanceSetting);
        UseISODateFormatToggle.IsOn = SettingsService.ReadSetting(Settings<bool>.UseISODateFormatSetting);
        WriteTrueFalseLowercaseToggle.IsOn = SettingsService.ReadSetting(Settings<bool>.WriteTrueFalseLowercaseSetting);
        ShowNotSharedToggle.IsOn = SettingsService.ReadSetting(Settings<bool>.ShowNotSharedSetting);

        int speedIndex = RawSpeedValueToDisplay(SettingsService.ReadSetting(Settings<int>.RefreshSpeedSetting));
        RefreshSpeedSlider.Value = speedIndex;
        UpdateSpeedText(speedIndex);
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

    private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        int index = (int)Math.Round(e.NewValue);

        if (index >= 0 && index < _intervalOptions.Length)
        {
            UpdateSpeedText(index);
            SettingsService.SaveSetting(Settings<int>.RefreshSpeedSetting, _intervalOptions[index].Seconds);
        }
    }
    private void UpdateSpeedText(int index)
    {
        if (index >= 0 && index < _intervalOptions.Length)
            RefreshSpeedText.Text = _intervalOptions[index].Text;
    }
    private int RawSpeedValueToDisplay(int raw)
    {
        int i = Array.FindIndex(_intervalOptions, o => o.Seconds == raw);
        return i >= 0 ? i : 1;   // default: 5s
    }
}
