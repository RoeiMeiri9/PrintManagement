using Microsoft.UI.Xaml.Data;
using System;

namespace PrintManagement.Converters;

public partial class FormatStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string text && parameter is string format)
        {
            return string.Format(format, text);
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}