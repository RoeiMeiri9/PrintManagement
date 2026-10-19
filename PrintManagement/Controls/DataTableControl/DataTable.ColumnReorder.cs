using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using Windows.ApplicationModel.DataTransfer;

namespace PrintManagement.Controls.DataTableControl
{
    public partial class DataTable
    {
        // ---------- Drag a header to reorder columns ----------
        //
        // Source: the header button (CanDrag). Target: every header cell (AllowDrop).
        // Dropping only calls Columns.Move(...) - the header and every row follow that
        // collection change (see Columns_CollectionChanged), nothing is rebuilt.

        private TableColumn? _draggedColumn;

        // ---------- hover / pressed look of a header cell ----------
        // (a ContentControl has no built-in pointer states, so the template's states are driven here)
        private static void SetHeaderState(object sender, string state)
        {
            if (sender is Control control)
                VisualStateManager.GoToState(control, state, true);
        }

        private void HeaderCell_PointerEntered(object sender, PointerRoutedEventArgs e) =>
            SetHeaderState(sender, "PointerOver");

        private void HeaderCell_PointerExited(object sender, PointerRoutedEventArgs e) =>
            SetHeaderState(sender, "Normal");

        private void HeaderCell_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is UIElement element && e.GetCurrentPoint(element).Properties.IsLeftButtonPressed)
                SetHeaderState(sender, "Pressed");
        }

        private void HeaderCell_PointerReleased(object sender, PointerRoutedEventArgs e) =>
            SetHeaderState(sender, "PointerOver");

        private void Header_DragStarting(UIElement sender, DragStartingEventArgs args)
        {
            SetHeaderState(sender, "Normal");   // no pointer events arrive during a drag, so don't stay "Pressed"

            if (sender is not FrameworkElement { DataContext: TableColumn col } || !col.CanReorder)
            {
                args.Cancel = true;
                return;
            }

            _draggedColumn = col;
            col.IsBeingDragged = true;

            args.AllowedOperations = DataPackageOperation.Move;
            args.Data.SetText(col.Header);   // a drag needs some payload; the real data is _draggedColumn

            StartDragAutoScroll();
        }

        private void Header_DragOver(object sender, DragEventArgs e)
        {
            if (_draggedColumn is not { } dragged ||
                sender is not FrameworkElement { DataContext: TableColumn target } cell) return;

            e.AcceptedOperation = DataPackageOperation.Move;
            e.DragUIOverride.IsCaptionVisible = false;
            e.DragUIOverride.IsGlyphVisible = false;

            // left half of the cell = insert before it, right half = insert after it
            bool after = e.GetPosition(cell).X >= cell.ActualWidth / 2;

            if (TryGetMoveIndex(dragged, target, after, out _))
                ShowDropIndicator(target, after ? ColumnDropPosition.After : ColumnDropPosition.Before);
            else
                ShowDropIndicator(null, ColumnDropPosition.None);   // would land where it already is
        }

        private void Header_DragLeave(object sender, DragEventArgs e) =>
            ShowDropIndicator(null, ColumnDropPosition.None);

        private void Header_Drop(object sender, DragEventArgs e)
        {
            if (_draggedColumn is { } dragged &&
                sender is FrameworkElement { DataContext: TableColumn target } cell)
            {
                bool after = e.GetPosition(cell).X >= cell.ActualWidth / 2;

                if (TryGetMoveIndex(dragged, target, after, out var newIndex))
                {
                    var oldIndex = Columns.IndexOf(dragged);

                    // let the drag operation finish first; the move re-creates the header items,
                    // including the one the drag started from
                    DispatcherQueue.TryEnqueue(() => Columns.Move(oldIndex, newIndex));
                }
            }

            ResetColumnDrag();
        }

        // fires on the drag source when the drag ends anywhere (dropped outside, cancelled with Esc, ...)
        private void Header_DropCompleted(UIElement sender, DropCompletedEventArgs args)
        {
            SetHeaderState(sender, "Normal");
            ResetColumnDrag();
        }

        // Index the dragged column ends up at, or false if the drop would change nothing.
        private bool TryGetMoveIndex(TableColumn dragged, TableColumn target, bool after, out int newIndex)
        {
            int from = Columns.IndexOf(dragged);
            int to = Columns.IndexOf(target);
            newIndex = from;
            if (from < 0 || to < 0) return false;

            int insertAt = after ? to + 1 : to;   // position in the list as it is now
            if (insertAt > from) insertAt--;      // taking the dragged column out shifts everything after it

            newIndex = insertAt;
            return newIndex != from;
        }

        private void ShowDropIndicator(TableColumn? target, ColumnDropPosition position)
        {
            foreach (var col in Columns)
                if (col != target && col.DropPosition != ColumnDropPosition.None)
                    col.DropPosition = ColumnDropPosition.None;

            if (target != null) target.DropPosition = position;
        }

        private void ResetColumnDrag()
        {
            _dragScrollTimer?.Stop();
            ShowDropIndicator(null, ColumnDropPosition.None);

            if (_draggedColumn != null)
            {
                _draggedColumn.IsBeingDragged = false;
                _draggedColumn = null;
            }
        }

        // ---------- Auto-scroll while dragging near the left/right edge ----------
        //
        // Same idea as the middle-click pan: scroll the rows list, the header follows
        // through _listScroller.ViewChanged.

        private const double DragEdgeZone = 48.0;       // px from the edge where scrolling starts
        private const double DragMaxStep = 24.0;        // px per tick at the very edge

        private DispatcherQueueTimer? _dragScrollTimer;
        private double _dragX;                           // pointer X relative to the header viewport
        private double _dragScrollTarget;

        private void HeaderScroller_DragOver(object sender, DragEventArgs e)
        {
            if (_draggedColumn == null) return;
            _dragX = e.GetPosition(HeaderScroller).X;
        }

        private void StartDragAutoScroll()
        {
            if (_listScroller == null) return;

            _dragX = HeaderScroller.ActualWidth / 2;     // neutral until the first DragOver
            _dragScrollTarget = _listScroller.HorizontalOffset;

            if (_dragScrollTimer == null)
            {
                _dragScrollTimer = DispatcherQueue.CreateTimer();
                _dragScrollTimer.Interval = TimeSpan.FromMilliseconds(16);
                _dragScrollTimer.Tick += (_, _) => DragAutoScrollStep();
            }
            _dragScrollTimer.Start();
        }

        private void DragAutoScrollStep()
        {
            var scroller = _listScroller;
            if (scroller == null || _draggedColumn == null) return;

            double width = HeaderScroller.ActualWidth;
            double step = 0;

            if (_dragX < DragEdgeZone)
                step = -DragMaxStep * (DragEdgeZone - Math.Max(_dragX, 0)) / DragEdgeZone;
            else if (_dragX > width - DragEdgeZone)
                step = DragMaxStep * (Math.Min(_dragX, width) - (width - DragEdgeZone)) / DragEdgeZone;

            if (step == 0) return;

            _dragScrollTarget = Math.Clamp(_dragScrollTarget + step, 0, scroller.ScrollableWidth);
            scroller.ChangeView(_dragScrollTarget, null, null, true);
        }
    }
}