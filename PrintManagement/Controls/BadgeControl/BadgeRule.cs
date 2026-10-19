using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace PrintManagement.Controls.BadgeControl;

public sealed class BadgeRule : DependencyObject
{
    public object? Key { get; set; }
    public string DisplayText { get; set; } = string.Empty;
    public string Glyph { get; set; } = string.Empty;
    public FontFamily? GlyphFontFamily { get; set; }
    public Brush? BackgroundBrush { get; set; }
    public Brush? ForegroundBrush { get; set; }
    public string? BackgroundResourceKey { get; set; }
    public string? ForegroundResourceKey { get; set; }
}

