using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace PrintManagement.Converters
{
    internal class JobsCounterConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            string ForegroundColor = "TextFillColorSecondaryBrush";

            if (value is uint jobs)
            {
                ForegroundColor = jobs == 0 ? "TemporaryTextFillColorDisabled" : ForegroundColor;
            }

            if (Application.Current.Resources.TryGetValue(ForegroundColor, out object resource) && resource is Brush brush)
            {
                return brush;
            }
            return new SolidColorBrush(Microsoft.UI.Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
