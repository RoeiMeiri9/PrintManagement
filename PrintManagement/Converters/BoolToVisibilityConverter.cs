using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace PrintManagement.Converters
{
    internal class BoolToVisibilityConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            bool hideOnNull = parameter?.ToString() == "hideOnNull";

            if (value is false)
            {
                return hideOnNull ? Visibility.Collapsed : Visibility.Visible;
            }
            return hideOnNull ? Visibility.Visible : Visibility.Collapsed;
        }


        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
