using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Linq;

namespace PrintManagement.Controls.Table
{
    public partial class DataTable
    {
        public static readonly DependencyProperty ThumbWithProperty =
    DependencyProperty.Register(nameof(ThumbWith), typeof(double), typeof(DataTable),
        new PropertyMetadata(8.0, OnThumbWidthChanged));

        public double ThumbWith
        {
            get => (double)GetValue(ThumbWithProperty);
            set => SetValue(ThumbWithProperty, value);
        }

        private static void OnThumbWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataTable table)
            {
                table.UpdateTotalWidth();
            }
        }

        private RoutedEventHandler OnTableLoaded_SetAutoWidth()
        {
            return (sender, e) => RecalculateAutoWidths();
        }

        public void RecalculateAutoWidths()
        {
            foreach (var column in Columns)
            {
                if (string.Equals(column.Width, "Auto", StringComparison.OrdinalIgnoreCase))
                {
                    double calculatedWidth = GetMaxWidth(column);

                    if (column.MaxAutoWidth.HasValue && calculatedWidth > column.MaxAutoWidth.Value)
                    {
                        calculatedWidth = column.MaxAutoWidth.Value;
                    }

                    column.ActualWidth = calculatedWidth;
                }
                else if (double.TryParse(column.Width, out double parsedWidth))
                {
                    column.ActualWidth = parsedWidth;
                }
                else
                {
                    column.ActualWidth = MinWidth;
                }
            }

            UpdateTotalWidth();
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

        private void UpdateTotalWidth() => TotalWidth = Columns.Sum(c => c.DisplayWidth) + ThumbWith;

        // ---------- Double Tapping Thumb ----------
        private void Thumb_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: TableColumn col })
            {
                col.ActualWidth = GetMaxWidth(col);
            }
        }

        private double GetMaxWidth(TableColumn col)
        {
            double rowPadding = GetCellContainerPadding();
            double width = GetHeaderCellWidth(col);

            foreach (var row in Rows)
            {
                width = Math.Max(width, GetCellWidth(col, row) + rowPadding);
            }

            return Math.Max(width, col.MinWidth + rowPadding);
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
                    cellContainer.Arrange(new Windows.Foundation.Rect(0, 0, cellContainer.DesiredSize.Width, cellContainer.DesiredSize.Height));
                    return cellContainer.DesiredSize.Width;
                }
            }
            return 0;
        }
    }
}
