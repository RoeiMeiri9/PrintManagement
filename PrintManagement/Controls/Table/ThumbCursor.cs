using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using System.Reflection;

namespace PrintManagement.Controls.Table;

// Gives any UIElement a resize cursor via an attached property
public static class ThumbCursor
{
    public static readonly DependencyProperty ResizeCursorProperty =
        DependencyProperty.RegisterAttached(
            "ResizeCursor", typeof(bool), typeof(ThumbCursor),
            new PropertyMetadata(false, OnResizeCursorChanged));

    public static bool GetResizeCursor(DependencyObject d) => (bool)d.GetValue(ResizeCursorProperty);
    public static void SetResizeCursor(DependencyObject d, bool v) => d.SetValue(ResizeCursorProperty, v);

    private static readonly PropertyInfo? CursorProp =
        typeof(UIElement).GetProperty("ProtectedCursor", BindingFlags.Instance | BindingFlags.NonPublic);

    private static void OnResizeCursorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element && (bool)e.NewValue)
        {
            CursorProp?.SetValue(element, InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
        }
    }
}