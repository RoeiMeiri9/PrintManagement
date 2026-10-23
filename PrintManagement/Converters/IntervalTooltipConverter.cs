using Microsoft.UI.Xaml.Data;
using System;

namespace PrintManagement.Converters;

public sealed partial class IntervalTooltipConverter((string Text, int Seconds)[] options) : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        int index = (int)Math.Round((double)value);
        return index >= 0 && index < options.Length ? options[index].Text : value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
