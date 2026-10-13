using Windows.Storage;

namespace PrinterManager.Services
{
    internal class SettingsService
    {
        private static readonly ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;

        public static void SaveSetting<T>(string key, T value)
        {
            LocalSettings.Values[key] = value;
        }

        public static T? ReadSetting<T>(string key, T? defaultValue = default)
        {
            if (LocalSettings.Values.TryGetValue(key, out object? value))
            {
                return (T)value;
            }
            return defaultValue;
        }
    }
}
