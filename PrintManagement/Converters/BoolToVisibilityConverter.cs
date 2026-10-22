using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace PrintManagement.Converters
{
    internal partial class BoolToVisibilityConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            string? paramaterAsString = parameter?.ToString();

            bool hideOnNull = paramaterAsString == "hideOnNull" || paramaterAsString == "hideOnFalse";

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
