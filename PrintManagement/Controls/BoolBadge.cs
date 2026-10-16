using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PrintManagement.Resources;

namespace PrintManagement.Controls;

public sealed partial class BoolBadge : Control
{
    public BoolBadge()
    {
        DefaultStyleKey = typeof(BoolBadge);
    }

    #region Dependency Properties

    public bool Value
    {
        get => (bool)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(bool),
            typeof(BoolBadge),
            new PropertyMetadata(false, OnValueChanged));

    public string TrueText
    {
        get => (string)GetValue(TrueTextProperty);
        set => SetValue(TrueTextProperty, value);
    }

    public static readonly DependencyProperty TrueTextProperty =
        DependencyProperty.Register(nameof(TrueText), typeof(string), typeof(BoolBadge), new PropertyMetadata("Shared"));

    public string FalseText
    {
        get => (string)GetValue(FalseTextProperty);
        set => SetValue(FalseTextProperty, value);
    }

    public static readonly DependencyProperty FalseTextProperty =
        DependencyProperty.Register(nameof(FalseText), typeof(string), typeof(BoolBadge), new PropertyMetadata("Not Shared"));

    public string TrueGlyph
    {
        get => (string)GetValue(TrueGlyphProperty);
        set => SetValue(TrueGlyphProperty, value);
    }

    public static readonly DependencyProperty TrueGlyphProperty =
        DependencyProperty.Register(nameof(TrueGlyph), typeof(string), typeof(BoolBadge), new PropertyMetadata(""));

    public string FalseGlyph
    {
        get => (string)GetValue(FalseGlyphProperty);
        set => SetValue(FalseGlyphProperty, value);
    }

    public static readonly DependencyProperty FalseGlyphProperty =
        DependencyProperty.Register(nameof(FalseGlyph), typeof(string), typeof(BoolBadge), new PropertyMetadata(""));

    public string DisplayText
    {
        get => (string)GetValue(DisplayTextProperty);
        set => SetValue(DisplayTextProperty, value);
    }

    public static readonly DependencyProperty DisplayTextProperty =
        DependencyProperty.Register(nameof(DisplayText), typeof(string), typeof(BoolBadge), new PropertyMetadata(string.Empty));

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(BoolBadge), new PropertyMetadata(string.Empty));

    public FontFamily? GlyphFontFamily
    {
        get => (FontFamily?)GetValue(GlyphFontFamilyProperty);
        set => SetValue(GlyphFontFamilyProperty, value);
    }

    public static readonly DependencyProperty GlyphFontFamilyProperty =
        DependencyProperty.Register(nameof(GlyphFontFamily), typeof(FontFamily), typeof(BoolBadge), new PropertyMetadata(null));

    // הוספנו בחזרה את מאפייני הצבעים שה-C# מנהל לבד
    public Brush? BackgroundBrush
    {
        get => (Brush?)GetValue(BackgroundBrushProperty);
        set => SetValue(BackgroundBrushProperty, value);
    }

    public static readonly DependencyProperty BackgroundBrushProperty =
        DependencyProperty.Register(nameof(BackgroundBrush), typeof(Brush), typeof(BoolBadge), new PropertyMetadata(null));

    public Brush? ForegroundBrush
    {
        get => (Brush?)GetValue(ForegroundBrushProperty);
        set => SetValue(ForegroundBrushProperty, value);
    }

    public static readonly DependencyProperty ForegroundBrushProperty =
        DependencyProperty.Register(nameof(ForegroundBrush), typeof(Brush), typeof(BoolBadge), new PropertyMetadata(null));

    #endregion

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BoolBadge badge)
        {
            badge.UpdateVisualState();
        }
    }

    private void UpdateVisualState()
    {
        if (Value)
        {
            DisplayText = string.IsNullOrEmpty(TrueText) ? "True" : TrueText;
            Glyph = string.IsNullOrEmpty(TrueGlyph) ? (Icons.AcceptMedium.Glyph ?? string.Empty) : TrueGlyph;
            GlyphFontFamily = Icons.AcceptMedium.Font;

            Application.Current.Resources.TryGetValue("StatusGreenBrush", out var backgroundBrush);
            BackgroundBrush = backgroundBrush as Brush ?? new SolidColorBrush(Microsoft.UI.Colors.LightGreen);

            Application.Current.Resources.TryGetValue("SystemFillColorSuccessBrush", out var foregroundBrush);
            ForegroundBrush = foregroundBrush as Brush ?? new SolidColorBrush(Microsoft.UI.Colors.Green);
        }
        else
        {
            DisplayText = string.IsNullOrEmpty(FalseText) ? "False" : FalseText;
            Glyph = string.IsNullOrEmpty(FalseGlyph) ? (Icons.ErrorBadge12.Glyph ?? string.Empty) : FalseGlyph;
            GlyphFontFamily = Icons.ErrorBadge12.Font;

            Application.Current.Resources.TryGetValue("StatusDefaultBrush", out var backgroundBrush);
            BackgroundBrush = backgroundBrush as Brush ?? new SolidColorBrush(Microsoft.UI.Colors.MistyRose);

            Application.Current.Resources.TryGetValue("SystemFillColorSolidNeutralBrush", out var foregroundBrush);
            ForegroundBrush = foregroundBrush as Brush ?? new SolidColorBrush(Microsoft.UI.Colors.Red);
        }
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateVisualState();
    }
}