using PrintManagement.Resources;
using System;
using Windows.Storage;

namespace PrintManagement.Services
{
    internal class SettingsService
    {
        private static readonly ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;

        public static void SaveSetting<T>(Settings<T> setting, T value)
        {
            if (value is Enum enumValue)
            {
                LocalSettings.Values[setting.Key] = Convert.ToInt32(enumValue);
            }
            else
            {
                LocalSettings.Values[setting.Key] = value;
            }
        }

        public static T ReadSetting<T>(Settings<T> setting)
        {
            if (LocalSettings.Values.TryGetValue(setting.Key, out object? value))
            {
                if (typeof(T).IsEnum)
                {
                    return (T)Enum.ToObject(typeof(T), value);
                }
                return (T)value;
            }
            return setting.DefaultValue;
        }
    }
}
