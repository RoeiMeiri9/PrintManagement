using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using PrintManagement.Models.enums;
using System;

namespace PrintManagement.Controls.IconGridViewControl;

internal partial class ViewModeToOrientationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ViewMode mode && mode == ViewMode.List)
        {
            return Orientation.Horizontal; // רשימה: אייקון משמאל, טקסט מימין
        }
        return Orientation.Vertical; // אריחים: אייקון למעלה, טקסט מתחת
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}