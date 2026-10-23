using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using PrintManagement.Models.Entity.enums;
using System;

namespace PrintManagement.Controls.IconGridViewControl;

internal partial class ViewModeToAlignmentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ViewMode mode && mode == ViewMode.List)
        {
            return HorizontalAlignment.Left;
        }
        return HorizontalAlignment.Center;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}