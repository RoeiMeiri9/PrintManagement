using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using PrintManagement.Models.enums;
using System;

namespace PrintManagement.Converters;

internal partial class EnumToVisibilityConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        bool notTable = parameter?.ToString() == "NotTable";

        if (value.Equals(ViewMode.Table))
        {
            return notTable ? Visibility.Collapsed : Visibility.Visible;
        }
        return notTable ? Visibility.Visible : Visibility.Collapsed;
    }


    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
