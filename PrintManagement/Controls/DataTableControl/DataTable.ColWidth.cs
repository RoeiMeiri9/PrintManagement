using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Globalization;
using System.Linq;

namespace PrintManagement.Controls.DataTableControl
{
    public partial class DataTable
    {
        public static readonly DependencyProperty ThumbWidthProperty =
            DependencyProperty.Register(nameof(ThumbWidth), typeof(double), typeof(DataTable),
                new PropertyMetadata(8.0, OnThumbWidthChanged));

        public double ThumbWidth
        {
            get => (double)GetValue(ThumbWidthProperty);
            set => SetValue(ThumbWidthProperty, value);
        }

        private bool _autoWidthQueued;

        private const int AutoWidthSampleRows = 200;
        private static readonly Windows.Foundation.Size Unbounded =
            new(double.PositiveInfinity, double.PositiveInfinity);

        private void ScheduleAutoWidth()
        {
            if (_autoWidthQueued) return;
            _autoWidthQueued = true;
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                _autoWidthQueued = false;
                RecalculateAutoWidths();
            });
        }

        private static void OnThumbWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataTable table)
            {
                table.UpdateTotalWidth();
            }
        }

        public void RecalculateAutoWidths()
        {
            double rowPadding = GetCellContainerPadding();   // identical for every column

            foreach (var column in Columns)
            {
                if (string.Equals(column.Width, "Auto", StringComparison.OrdinalIgnoreCase) && !column.IsUserSized)
                {
                    double calculatedWidth = GetMaxWidth(column, rowPadding, AutoWidthSampleRows);

                    if (column.MaxAutoWidth.HasValue && calculatedWidth > column.MaxAutoWidth.Value)
                        calculatedWidth = column.MaxAutoWidth.Value;

                    column.ActualWidth = calculatedWidth;
                }
                else if (double.TryParse(column.Width, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedWidth))
                {
                    column.ActualWidth = parsedWidth;
                }
                else
                {
                    column.ActualWidth = MinWidth;
                }
            }

            UpdateTotalWidth();
            TaskCompleted(TableInitialTasks.AutoWidthCalculated);
        }

        // ---------- Dragging Thumb ----------
        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: TableColumn col })
                col.Resize(e.HorizontalChange);
        }


        // rows bind their Width to this
        public static readonly DependencyProperty TotalWidthProperty =
            DependencyProperty.Register(nameof(TotalWidth), typeof(double), typeof(DataTable),
                new PropertyMetadata(0.0));

        public double TotalWidth
        {
            get => (double)GetValue(TotalWidthProperty);
            private set => SetValue(TotalWidthProperty, value);
        }

        private ItemsControl? _headerItemsControl;

        private void UpdateTotalWidth()
        {
            // Used to make sure the line will not surpuss the hearder.
            // The thing is that WinUI is very strange and the header is not truely aligned well with the body,
            // so hand crafted calculation must be used for making sure the header and the rows are aligned well.
            // As far as I know: the calculations must be: Thumb width + the difference between the right and left margin.
            double offsetFromRightSide = ThumbWidth;

            _headerItemsControl ??= FindDescendant<ItemsControl>(HeaderScroller);
            if (_headerItemsControl is { } ic)
                offsetFromRightSide += Math.Abs(ic.Margin.Left - ic.Margin.Right);

            TotalWidth = Columns.Sum(c => c.DisplayWidth) - offsetFromRightSide;
        }
        // ---------- Double Tapping Thumb ----------
        private void Thumb_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: TableColumn col })
            {
                // explicit user action: measure every row, no cap
                col.ActualWidth = GetMaxWidth(col, GetCellContainerPadding());
            }
        }

        private double GetMaxWidth(TableColumn col, double rowPadding, int maxRows = int.MaxValue)
        {
            double width = GetHeaderCellWidth(col);

            // One template instance per column. Each row just swaps the DataContext and re-measures.
            if (col.EffectiveTemplate?.LoadContent() is FrameworkElement probe)
            {
                int colIndex = Columns.IndexOf(col);

                foreach (var row in Rows.Take(maxRows))
                {
                    if (FindCell(row, col, colIndex) is not { } cell) continue;

                    probe.DataContext = cell.Item;
                    probe.Measure(Unbounded);
                    width = Math.Max(width, probe.DesiredSize.Width + rowPadding);
                }
            }

            return Math.Max(width, col.MinWidth + rowPadding);
        }

        private static TableCell? FindCell(TableRow row, TableColumn col, int colIndex)
        {
            // Cells mirror the column order (MoveCell relies on that), so the index is the fast path.
            var cell = row.Cells.ElementAtOrDefault(colIndex);
            if (!(cell is not null && cell.Column.Equals(col))) System.Diagnostics.Debug.WriteLine("Safty net for FindCell was used!");
            return cell is not null && cell.Column.Equals(col)
                ? cell
                : row.Cells.FirstOrDefault(c => c.Column.Equals(col));   // safety net
        }

        private static double GetHeaderCellWidth(TableColumn col)
        {
            if (col.Header == null) return 0;

            var tb = new TextBlock
            {
                Text = col.Header.ToString(),
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.Normal
            };

            tb.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
            double totalWidth = tb.DesiredSize.Width;

            if (col.SortIndicatorVisibility == Visibility.Visible)
            {
                totalWidth += 10;
            }

            const double headerButtonPaddingAndDecorations = 32.0;
            return totalWidth + headerButtonPaddingAndDecorations;
        }

        private static double GetCellWidth(TableColumn col, TableRow row)
        {
            TableCell? cell = row.Cells.FirstOrDefault(c => c.Column.Equals(col));
            if (cell != null && col.EffectiveTemplate != null)
            {
                DependencyObject contentObject = col.EffectiveTemplate.LoadContent();
                if (contentObject is FrameworkElement element)
                {
                    element.DataContext = cell.Item;
                    element.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
                    element.Arrange(new Windows.Foundation.Rect(0, 0, element.DesiredSize.Width, element.DesiredSize.Height));
                    return element.DesiredSize.Width;
                }
            }
            return 0;
        }

        private double GetCellContainerPadding()
        {
            if (Resources["CellTemplate"] is DataTemplate cellTemplate)
            {
                DependencyObject cellObject = cellTemplate.LoadContent();
                if (cellObject is FrameworkElement cellContainer)
                {
                    cellContainer.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
                    return cellContainer.DesiredSize.Width;
                }
            }
            return 0;
        }
    }
}
