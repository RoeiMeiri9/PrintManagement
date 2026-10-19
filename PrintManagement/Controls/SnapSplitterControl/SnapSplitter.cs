using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;

namespace PrintManagement.Controls.SnapSplitterControl;

public sealed partial class SnapSplitter : Grid
{
    public static readonly DependencyProperty TargetColumnProperty =
        DependencyProperty.Register(
            nameof(TargetColumn),
            typeof(ColumnDefinition),
            typeof(SnapSplitter),
            new PropertyMetadata(null));

    public ColumnDefinition? TargetColumn
    {
        get => (ColumnDefinition?)GetValue(TargetColumnProperty);
        set => SetValue(TargetColumnProperty, value);
    }

    public double MinTreeWidth { get; set; } = 100;

    private bool _isDraggingSplitter;
    private bool _isPointerOverSplitter;
    private bool _isTreeCollapsed;
    private double _grabOffset;

    private bool _isAnimatingSnap;
    private double _snapTargetWidth;
    private double _snapStartWidth;
    private Stopwatch? _snapStopwatch;
    private const double SnapDurationMs = 180.0;

    private readonly Border _highlightBorder;

    private double CurrentTreeWidth =>
        TargetColumn != null && TargetColumn.Width.IsAbsolute ? TargetColumn.Width.Value : ActualWidth;

    public SnapSplitter()
    {
        _highlightBorder = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            CornerRadius = new CornerRadius(1),
            IsHitTestVisible = false,
            Opacity = 0
        };

        if (Application.Current.Resources.TryGetValue("ControlStrongFillColorDefaultBrush", out var brush))
        {
            _highlightBorder.Background = brush as Brush;
        }

        _highlightBorder.OpacityTransition = new ScalarTransition { Duration = TimeSpan.FromMilliseconds(150) };

        Children.Add(_highlightBorder);

        PointerEntered += SnapSplitter_PointerEntered;
        PointerExited += SnapSplitter_PointerExited;
        PointerPressed += SnapSplitter_PointerPressed;
        PointerMoved += SnapSplitter_PointerMoved;
        PointerReleased += SnapSplitter_PointerReleased;
        PointerCaptureLost += SnapSplitter_PointerCaptureLost;
    }

    private void SnapSplitter_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOverSplitter = true;
        UpdateHighlight();
    }

    private void SnapSplitter_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOverSplitter = false;
        UpdateHighlight();
    }

    private void SnapSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        StopSnapAnimation();

        if (!CapturePointer(e.Pointer)) return;
        _isDraggingSplitter = true;
        UpdateHighlight();

        var rootGrid = FindParent<Grid>(this);
        if (rootGrid != null)
        {
            _grabOffset = e.GetCurrentPoint(rootGrid).Position.X - CurrentTreeWidth;
        }

        e.Handled = true;
    }

    private void SnapSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDraggingSplitter || TargetColumn == null) return;

        var rootGrid = FindParent<Grid>(this);
        if (rootGrid == null) return;

        double desired = e.GetCurrentPoint(rootGrid).Position.X - _grabOffset;
        bool wantCollapsed = desired < MinTreeWidth;
        double target = wantCollapsed ? 0 : Math.Min(desired, TargetColumn.MaxWidth);

        if (wantCollapsed != _isTreeCollapsed)
        {
            _isTreeCollapsed = wantCollapsed;
            StartOrUpdateSnapAnimation(target);
        }
        else if (_isAnimatingSnap)
        {
            _snapTargetWidth = target;
        }
        else
        {
            ApplyWidth(target);
        }
        e.Handled = true;
    }

    private void SnapSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isDraggingSplitter = false;
        StopSnapAnimation();
        ReleasePointerCapture(e.Pointer);
        UpdateHighlight();
    }

    private void SnapSplitter_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _isDraggingSplitter = false;
        StopSnapAnimation();
        UpdateHighlight();
    }

    private void UpdateHighlight()
    {
        _highlightBorder.Opacity = _isPointerOverSplitter || _isDraggingSplitter ? 0.5 : 0;
    }

    private void ApplyWidth(double width)
    {
        if (TargetColumn == null) return;

        width = Math.Clamp(width, 0, TargetColumn.MaxWidth);
        TargetColumn.Width = new GridLength(width);
    }

    private void StartOrUpdateSnapAnimation(double targetWidth)
    {
        _snapStartWidth = CurrentTreeWidth;
        _snapTargetWidth = targetWidth;
        _snapStopwatch = Stopwatch.StartNew();

        if (!_isAnimatingSnap)
        {
            _isAnimatingSnap = true;
            CompositionTarget.Rendering += OnSnapStep;
        }
    }

    private void StopSnapAnimation()
    {
        if (_isAnimatingSnap)
        {
            _isAnimatingSnap = false;
            CompositionTarget.Rendering -= OnSnapStep;
        }
    }

    private void OnSnapStep(object? sender, object e)
    {
        if (!_isDraggingSplitter || _snapStopwatch == null)
        {
            StopSnapAnimation();
            return;
        }

        double elapsed = _snapStopwatch.ElapsedMilliseconds;
        double progress = Math.Min(elapsed / SnapDurationMs, 1.0);

        // Ease-out Cubic
        double eased = 1 - Math.Pow(1 - progress, 3);
        double current = _snapStartWidth + (_snapTargetWidth - _snapStartWidth) * eased;

        ApplyWidth(current);

        if (progress >= 1.0 || Math.Abs(current - _snapTargetWidth) < 0.5)
        {
            ApplyWidth(_snapTargetWidth);
            StopSnapAnimation();
        }
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject parentObject = VisualTreeHelper.GetParent(child);
        if (parentObject == null) return null;
        if (parentObject is T parent) return parent;
        return FindParent<T>(parentObject);
    }
}