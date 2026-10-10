using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace PrinterManager.Pages.CustomFilterPage
{
    public class StatusColorsNames
    {
        public required string Background { get; set; }
        public required string Foreground { get; set; }
    }

    public partial class StatusConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {

            var status = value?.ToString();

            var names = status switch
            {
                "Ready" => new StatusColorsNames
                {
                    Background = "StatusGreenBrush",
                    Foreground = "SystemFillColorSuccessBrush"
                },
                "Pending" => new StatusColorsNames
                {
                    Background = "StatusOrangeBrush",
                    Foreground = "SystemFillColorCautionBrush"
                },
                "Offline" => new StatusColorsNames
                {
                    Background = "StatusRedBrush",
                    Foreground = "SystemFillColorCriticalBrush"
                },
                _ => new StatusColorsNames
                {
                    Background = "StatusDefaultBrush",
                    Foreground = "SystemFillColorSolidNeutralBrush"
                }
            };

            string resourceKey = parameter?.ToString() == "Foreground"
                            ? names.Foreground
                            : names.Background;



            if (Application.Current.Resources.TryGetValue(resourceKey, out object resource) && resource is Brush brush)
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
