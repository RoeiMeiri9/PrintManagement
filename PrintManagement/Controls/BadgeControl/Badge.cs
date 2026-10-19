using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using PrintManagement.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Text;

namespace PrintManagement.Controls.BadgeControl;

public sealed partial class BadgeRuleCollection : ObservableCollection<BadgeRule> { }

public sealed partial class Badge : Control
{
    public Badge()
    {
        DefaultStyleKey = typeof(Badge);
        Loaded += (_, _) =>
        {
            ViewDDSettingsService.BadgeStyleChanged -= OnMinimalBadgeChanged;
            ViewDDSettingsService.BadgeStyleChanged += OnMinimalBadgeChanged;
            IsColorful = ViewDDSettingsService.IsColorfulBadgeView;
        };
        Unloaded += (_, _) => ViewDDSettingsService.BadgeStyleChanged -= OnMinimalBadgeChanged;
        IsColorful = ViewDDSettingsService.IsColorfulBadgeView;
    }

    #region Dependency Properties

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(object),
            typeof(Badge),
            new PropertyMetadata(null, OnVisualStatePropertyChanged));

    public IList<BadgeRule> Rules
    {
        get => (IList<BadgeRule>)GetValue(RulesProperty);
        set => SetValue(RulesProperty, value);
    }

    public static readonly DependencyProperty RulesProperty =
        DependencyProperty.Register(
            nameof(Rules),
            typeof(IList<BadgeRule>),
            typeof(Badge),
            new PropertyMetadata(null, OnVisualStatePropertyChanged));

    public string DisplayText
    {
        get => (string)GetValue(DisplayTextProperty);
        set => SetValue(DisplayTextProperty, value); // 🎯 שונה מ-private ל-public
    }

    public static readonly DependencyProperty DisplayTextProperty =
        DependencyProperty.Register(nameof(DisplayText), typeof(string), typeof(Badge), new PropertyMetadata(string.Empty));

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        private set => SetValue(GlyphProperty, value);
    }

    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(nameof(Glyph), typeof(string), typeof(Badge), new PropertyMetadata(string.Empty));

    public FontFamily? GlyphFontFamily
    {
        get => (FontFamily?)GetValue(GlyphFontFamilyProperty);
        private set => SetValue(GlyphFontFamilyProperty, value);
    }

    public static readonly DependencyProperty GlyphFontFamilyProperty =
        DependencyProperty.Register(nameof(GlyphFontFamily), typeof(FontFamily), typeof(Badge), new PropertyMetadata(null));

    public Visibility GlyphVisibility
    {
        get => (Visibility)GetValue(GlyphVisibilityProperty);
        private set => SetValue(GlyphVisibilityProperty, value);
    }

    public static readonly DependencyProperty GlyphVisibilityProperty =
        DependencyProperty.Register(nameof(GlyphVisibility), typeof(Visibility), typeof(Badge), new PropertyMetadata(Visibility.Collapsed));

    public Thickness BadgePadding
    {
        get => (Thickness)GetValue(BadgePaddingProperty);
        private set => SetValue(BadgePaddingProperty, value);
    }

    public static readonly DependencyProperty BadgePaddingProperty =
        DependencyProperty.Register(nameof(BadgePadding), typeof(Thickness), typeof(Badge), new PropertyMetadata(new Thickness(10, 3, 10, 3)));

    public Brush? BackgroundBrush
    {
        get => (Brush?)GetValue(BackgroundBrushProperty);
        private set => SetValue(BackgroundBrushProperty, value);
    }

    public static readonly DependencyProperty BackgroundBrushProperty =
        DependencyProperty.Register(nameof(BackgroundBrush), typeof(Brush), typeof(Badge), new PropertyMetadata(null));

    public Brush? ForegroundBrush
    {
        get => (Brush?)GetValue(ForegroundBrushProperty);
        private set => SetValue(ForegroundBrushProperty, value);
    }

    public static readonly DependencyProperty ForegroundBrushProperty =
        DependencyProperty.Register(nameof(ForegroundBrush), typeof(Brush), typeof(Badge), new PropertyMetadata(null));

    public double BadgeFontSize
    {
        get => (double)GetValue(BadgeFontSizeProperty);
        private set => SetValue(BadgeFontSizeProperty, value);
    }

    public static readonly DependencyProperty BadgeFontSizeProperty =
        DependencyProperty.Register(nameof(BadgeFontSize), typeof(double), typeof(Badge), new PropertyMetadata(12.0));

    public FontWeight BadgeFontWeight
    {
        get => (FontWeight)GetValue(BadgeFontWeightProperty);
        private set => SetValue(BadgeFontWeightProperty, value);
    }

    public static readonly DependencyProperty BadgeFontWeightProperty =
    DependencyProperty.Register(nameof(BadgeFontWeight), typeof(double), typeof(Badge), new PropertyMetadata(FontWeights.SemiBold));


    public bool IsColorful
    {
        get => (bool)GetValue(IsMinimalProperty);
        set => SetValue(IsMinimalProperty, value);
    }

    public static readonly DependencyProperty IsMinimalProperty =
        DependencyProperty.Register(
            nameof(IsColorful),
            typeof(bool),
            typeof(Badge),
            new PropertyMetadata(false, OnVisualStatePropertyChanged));

    #endregion

    private void OnMinimalBadgeChanged(object? sender, bool isMinimal) =>
    DispatcherQueue?.TryEnqueue(() => IsColorful = isMinimal);

    private static void OnVisualStatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Badge badge)
        {
            badge.UpdateVisualState();
        }
    }

    private void UpdateVisualState()
    {
        if (Value == null || Rules == null || Rules.Count == 0)
        {
            ResetVisuals();
            return;
        }

        var matchedRule = Rules.FirstOrDefault(r => Equals(r.Key, Value) || r.Key?.ToString() == Value.ToString());

        if (matchedRule != null)
        {
            if (!IsColorful)
            {
                ApplyMinimalStyle(matchedRule);
                return;
            }
            ApplyRule(matchedRule);
        }
        else
        {
            ResetVisuals();
        }
    }

    private void ApplyMinimalStyle(BadgeRule rule)
    {
        if (string.IsNullOrEmpty(DisplayText))
        {
            DisplayText = rule.DisplayText;
        }
        Glyph = string.Empty;
        GlyphVisibility = Visibility.Collapsed;
        BadgePadding = new Thickness(0, 2, 0, 2);
        BadgeFontSize = 14.0;
        BackgroundBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        BadgeFontWeight = FontWeights.Normal;

        if (Application.Current.Resources.TryGetValue("TextFillColorSecondaryBrush", out var defaultFg) && defaultFg is Brush minimalFgBrush)
            ForegroundBrush = minimalFgBrush;
        else
            ForegroundBrush = new SolidColorBrush(Microsoft.UI.Colors.Black);
    }

    private void ApplyRule(BadgeRule rule)
    {
        if (string.IsNullOrEmpty(DisplayText))
        {
            DisplayText = rule.DisplayText;
        }

        Glyph = rule.Glyph;
        GlyphFontFamily = rule.GlyphFontFamily;

        bool hasGlyph = !string.IsNullOrEmpty(Glyph);
        GlyphVisibility = hasGlyph ? Visibility.Visible : Visibility.Collapsed;
        BadgePadding = hasGlyph ? new Thickness(8, 3, 10, 3) : new Thickness(10, 3, 10, 3);
        BadgeFontSize = 12.0;
        BadgeFontWeight = FontWeights.SemiBold;

        if (!string.IsNullOrEmpty(rule.BackgroundResourceKey) && Application.Current.Resources.TryGetValue(rule.BackgroundResourceKey, out var bg) && bg is Brush bgBrush)
            BackgroundBrush = bgBrush;
        else
            BackgroundBrush = rule.BackgroundBrush;

        if (!string.IsNullOrEmpty(rule.ForegroundResourceKey) && Application.Current.Resources.TryGetValue(rule.ForegroundResourceKey, out var fg) && fg is Brush fgBrush)
            ForegroundBrush = fgBrush;
        else
            ForegroundBrush = rule.ForegroundBrush;
    }

    private void ResetVisuals()
    {
        DisplayText = Value?.ToString() ?? string.Empty;
        Glyph = string.Empty;
        GlyphVisibility = Visibility.Collapsed;
        BadgePadding = new Thickness(10, 3, 10, 3);
        BackgroundBrush = null;
        ForegroundBrush = null;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateVisualState();
    }
}