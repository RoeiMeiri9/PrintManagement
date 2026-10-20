using Microsoft.UI.Xaml.Data;
using PrintManagement.Models.enums;
using System;

namespace PrintManagement.Controls.IconGridViewControl;

internal partial class ViewModeToSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ViewMode mode && mode == ViewMode.List)
        {
            return 18.0;
        }
        return 36.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}