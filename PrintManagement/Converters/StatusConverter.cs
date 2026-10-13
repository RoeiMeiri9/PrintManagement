using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using PrintManagement.Pages.CustomFilterPage.Helpers;
using System;

namespace PrintManagement.Converters;

public class StatusColorsNames
{
    public required string Background { get; set; }
    public required string Foreground { get; set; }
}
internal partial class StatusConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {

        PrinterStatusFlags flag = (PrinterStatusFlags)value;

        StatusColorsNames names = flag switch
        {
            PrinterStatusFlags.Ready => new StatusColorsNames
            {
                Background = "StatusGreenBrush",
                Foreground = "SystemFillColorSuccessBrush"
            },
            PrinterStatusFlags.Printing or
            PrinterStatusFlags.Busy or
            PrinterStatusFlags.Processing => new StatusColorsNames
            {
                Background = "StatusOrangeBrush",
                Foreground = "SystemFillColorCautionBrush"
            },
            PrinterStatusFlags.Error or
            PrinterStatusFlags.Offline or
            PrinterStatusFlags.PaperJam or
            PrinterStatusFlags.PaperOut => new StatusColorsNames
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
