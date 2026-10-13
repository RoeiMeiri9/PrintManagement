using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using PrintManagement.Resources;
using PrintManagement.Services;
using System;
using System.Diagnostics;
using Windows.Foundation;

namespace PrintManagement.Controls.Table
{
    public partial class DataTable
    {
        // ---------- Horizontal Scroll ----------
        private ScrollViewer? _listScroller;

        private bool ShowMarker;

        private void RowsList_Loaded(object sender, RoutedEventArgs e)
        {
            if (_listScroller != null) return;
            _listScroller = FindDescendant<ScrollViewer>(RowsList);

            if (_listScroller != null)
            {
                // The header follows the list horizontally (this also covers panning).
                _listScroller.ViewChanged += (_, _) =>
                    HeaderScroller.ChangeView(_listScroller.HorizontalOffset, null, null, true);

                _listScroller.PointerWheelChanged += OnScrollPointerWheelChanged;
            }

            InitPanning();
            HookPanStart(RowsList);
        }

        private bool _headerHooked;

        private void HeaderList_Loaded(object sender, RoutedEventArgs e)
        {
            if (_headerHooked) return;
            _headerHooked = true;

            HeaderScroller.PointerWheelChanged += OnScrollPointerWheelChanged;

            InitPanning();
            HookPanStart(HeaderScroller);
        }

        private void OnScrollPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var pointerPoint = e.GetCurrentPoint(null);
            var mouseWheelDelta = pointerPoint.Properties.MouseWheelDelta;

            if (mouseWheelDelta != 0 && sender is ScrollViewer scroller)
            {
                var modifiers = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
                bool isCtrlPressed = (modifiers & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;

                if (!isCtrlPressed) return;

                double scrollFactor = 0.4;
                double targetOffset = scroller.HorizontalOffset - (mouseWheelDelta * scrollFactor);
                targetOffset = Math.Max(0, Math.Min(targetOffset, scroller.ScrollableWidth));

                scroller.ChangeView(targetOffset, scroller.VerticalOffset, null, true);

                if (scroller == HeaderScroller && _listScroller != null)
                {
                    _listScroller.ChangeView(targetOffset, _listScroller.VerticalOffset, null, true);
                }
                else if (scroller == _listScroller)
                {
                    HeaderScroller.ChangeView(targetOffset, HeaderScroller.VerticalOffset, null, true);
                }

                e.Handled = true;
            }
        }

        private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match) return match;
                var result = FindDescendant<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        // ---------- Middle-click autoscroll ----------
        //
        // PanCursorOverlay (in DataTable.xaml) is a transparent overlay spanning both rows.
        // It is always "visible" but not hit-testable. While panning it becomes hit-testable and
        // captures the pointer (so the release always reaches us), and it hosts the origin marker.
        //
        // The press can start on the rows or on the header; either way we scroll the rows list,
        // and the header follows through _listScroller.ViewChanged.

        private const double DeadZone = 10.0;
        private const double SpeedMultiplier = 0.001;   // px/frame per px^2 beyond the dead zone (at 60 fps)
        private const double MaxStepPerFrame = 30.0;    // at 60 fps

        private bool _panInitialized;
        private bool _isPanning;
        private Point _panOrigin;
        private Point _panCurrent;
        private double _targetX;
        private double _targetY;
        private readonly Stopwatch _panClock = new();
        private TimeSpan _lastFrame;

        private void InitPanning()
        {
            if (_panInitialized) return;
            _panInitialized = true;

            var overlay = PanCursorOverlay;
            overlay.Background = new SolidColorBrush(Colors.Transparent); // null brush isn't hit-testable
            overlay.Visibility = Visibility.Visible;
            overlay.IsHitTestVisible = false;


            BuildPanMarker(overlay);


            overlay.PointerMoved += OnPanMoved;
            overlay.PointerReleased += OnPanReleased;
            overlay.PointerCanceled += (_, _) => StopPanning();
            overlay.PointerCaptureLost += (_, _) => StopPanning();
        }

        private void HookPanStart(UIElement element)
        {
            // handledEventsToo: ListViewItem may mark the press as handled before we see it.
            element.AddHandler(PointerPressedEvent, new PointerEventHandler(OnPanPressed), true);
        }

        private void OnPanPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_isPanning || _listScroller == null) return;

            var point = e.GetCurrentPoint(PanCursorOverlay);
            if (!point.Properties.IsMiddleButtonPressed) return;

            var overlay = PanCursorOverlay;
            overlay.IsHitTestVisible = true;
            if (!overlay.CapturePointer(e.Pointer))
            {
                overlay.IsHitTestVisible = false;
                return;
            }

            ShowMarker = SettingsService.ReadSetting(Settings.ScrollIndicatorSetting.Value, false);

            _isPanning = true;
            _panOrigin = _panCurrent = point.Position;
            _targetX = _listScroller.HorizontalOffset;
            _targetY = _listScroller.VerticalOffset;

            if (ShowMarker)
            {
                ShowPanMarker(_panOrigin);
            }
            _panClock.Restart();
            _lastFrame = TimeSpan.Zero;
            CompositionTarget.Rendering += OnPanRendering;

            e.Handled = true;
        }

        private void OnPanMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isPanning) return;

            _panCurrent = e.GetCurrentPoint(PanCursorOverlay).Position;
            UpdatePanMarker(_panCurrent, _markerLine);
            UpdatePanMarker(_panCurrent, _markerOuterLine);
            e.Handled = true;
        }

        private void OnPanReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!_isPanning) return;

            // Only end the pan when the middle button is no longer held.
            if (e.GetCurrentPoint(PanCursorOverlay).Properties.IsMiddleButtonPressed) return;

            StopPanning();
            e.Handled = true;
        }

        private void StopPanning()
        {
            if (!_isPanning) return;
            _isPanning = false;

            CompositionTarget.Rendering -= OnPanRendering;
            _panClock.Stop();

            var overlay = PanCursorOverlay;
            overlay.ReleasePointerCaptures();
            overlay.IsHitTestVisible = false;
            HidePanMarker();
        }

        private void OnPanRendering(object? sender, object e)
        {
            var scroller = _listScroller;
            if (!_isPanning || scroller == null) return;

            // Scale by frame time so speed doesn't depend on the display's refresh rate.
            var now = _panClock.Elapsed;
            double frames = Math.Min((now - _lastFrame).TotalSeconds, 0.1) * 60.0;
            _lastFrame = now;

            double moveX = StepFor(_panCurrent.X - _panOrigin.X) * frames;
            double moveY = StepFor(_panCurrent.Y - _panOrigin.Y) * frames;
            if (moveX == 0 && moveY == 0) return;

            // Accumulate our own target instead of re-reading the offset, which may lag behind ChangeView.
            _targetX = Math.Clamp(_targetX + moveX, 0, scroller.ScrollableWidth);
            _targetY = Math.Clamp(_targetY + moveY, 0, scroller.ScrollableHeight);

            scroller.ChangeView(_targetX, _targetY, null, true);
        }

        private static double StepFor(double offset)
        {
            if (Math.Abs(offset) <= DeadZone) return 0;

            double effective = offset > 0 ? offset - DeadZone : offset + DeadZone;
            double step = effective * Math.Abs(effective) * SpeedMultiplier; // quadratic acceleration
            return Math.Sign(step) * Math.Min(Math.Abs(step), MaxStepPerFrame);
        }

        // ---------- Origin marker (circle at the press point + a line toward the mouse) ----------
        private const double MarkerRadius = 14.0;
        private const double MaxLineLength = 90.0;

        private Canvas? _markerCanvas;
        private Ellipse? _markerRingWrapper;
        private Ellipse? _markerRing;
        private Ellipse? _markerDot;
        private Line? _markerLine;
        private Line? _markerOuterLine;

        private void BuildPanMarker(Panel host)
        {
            var accent = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
            var outerLineBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(100, 0, 0, 0));
            double opacity = 0.9;
            _markerRingWrapper = new Ellipse
            {
                Width = MarkerRadius * 2 + 2,
                Height = MarkerRadius * 2 + 2,
                Stroke = outerLineBrush,
                Opacity = opacity,
                StrokeThickness = 4,
            };
            _markerRing = new Ellipse
            {
                Width = MarkerRadius * 2,
                Height = MarkerRadius * 2,
                Stroke = accent,
                Opacity = opacity,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(70, 128, 128, 128)),
            };
            _markerDot = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = accent,
                Stroke = outerLineBrush,
                StrokeThickness = 1,
                Opacity = opacity
            };
            _markerLine = new Line
            {
                Stroke = accent,
                StrokeThickness = 3,
                Opacity = opacity,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Triangle,
                Visibility = Visibility.Collapsed,
            };
            _markerOuterLine = new Line
            {
                Stroke = outerLineBrush,
                StrokeThickness = 5,
                Opacity = opacity,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Triangle,
                Visibility = Visibility.Collapsed,
            };

            _markerCanvas = new Canvas { IsHitTestVisible = false, Visibility = Visibility.Collapsed };
            _markerCanvas.Children.Add(_markerRingWrapper);
            _markerCanvas.Children.Add(_markerOuterLine);
            _markerCanvas.Children.Add(_markerLine);
            _markerCanvas.Children.Add(_markerRing);
            _markerCanvas.Children.Add(_markerDot);

            host.Children.Add(_markerCanvas);
        }

        private void ShowPanMarker(Point origin)
        {
            if (_markerCanvas == null || _markerRing == null || _markerDot == null) return;

            Canvas.SetLeft(_markerRingWrapper, origin.X - MarkerRadius - 1);
            Canvas.SetTop(_markerRingWrapper, origin.Y - MarkerRadius - 1);
            Canvas.SetLeft(_markerRing, origin.X - MarkerRadius);
            Canvas.SetTop(_markerRing, origin.Y - MarkerRadius);
            Canvas.SetLeft(_markerDot, origin.X - 3);
            Canvas.SetTop(_markerDot, origin.Y - 3);

            UpdatePanMarker(origin, _markerLine);
            UpdatePanMarker(origin, _markerOuterLine);
            _markerCanvas.Visibility = Visibility.Visible;
        }

        private void UpdatePanMarker(Point current, Line? markerLine)
        {
            if (markerLine == null) return;

            double dx = current.X - _panOrigin.X;
            double dy = current.Y - _panOrigin.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            // Only draw once the mouse is outside the ring (this also avoids a reversed line).
            if (distance <= MarkerRadius)
            {
                markerLine.Visibility = Visibility.Collapsed;
                return;
            }

            double ux = dx / distance;
            double uy = dy / distance;

            // Ends exactly at the mouse, capped at the ring's edge + MaxLineLength.
            double endDistance = Math.Min(distance, MarkerRadius + MaxLineLength);

            markerLine.X1 = _panOrigin.X + ux * MarkerRadius;
            markerLine.Y1 = _panOrigin.Y + uy * MarkerRadius;
            markerLine.X2 = _panOrigin.X + ux * endDistance;
            markerLine.Y2 = _panOrigin.Y + uy * endDistance;
            markerLine.Visibility = Visibility.Visible;
        }

        private void HidePanMarker()
        {
            if (_markerCanvas != null) _markerCanvas.Visibility = Visibility.Collapsed;
            if (_markerLine != null) _markerLine.Visibility = Visibility.Collapsed;
        }
    }
}