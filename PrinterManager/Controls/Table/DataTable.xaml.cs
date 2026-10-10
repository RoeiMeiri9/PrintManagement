using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace PrinterManager.Controls.Table;

[ContentProperty(Name = nameof(Columns))]
public sealed partial class DataTable : UserControl
{
    public TableColumnCollection Columns { get; } = [];

    /// <summary>What the ListView shows: one TableRow per item, already in sorted order.</summary>
    public TableRowCollection Rows { get; } = [];

    // every row in the order of the source collection (the unsorted truth)
    private readonly List<TableRow> _all = [];

    private INotifyCollectionChanged? _observedSource;
    private bool _suppressSelection;
    private bool _updatingSort;

    public DataTable()
    {
        InitializeComponent();
        Columns.CollectionChanged += Columns_CollectionChanged;
    }

    // ---------- dependency properties ----------
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(DataTable),
            new PropertyMetadata(null, (d, e) => ((DataTable)d).OnItemsSourceChanged(e.NewValue)));

    public object? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
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

    // ---------- selection (works with your items, not with TableRow) ----------
    public object? SelectedItem
    {
        get => (RowsList.SelectedItem as TableRow)?.Item;
        set => RowsList.SelectedItem = value == null ? null : FindRow(value);
    }

    public event SelectionChangedEventHandler? SelectionChanged;

    private void RowsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelection) return;
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(Unwrap(e.RemovedItems), Unwrap(e.AddedItems)));
    }

    private static List<object> Unwrap(IList<object> rows) =>
        [.. rows.OfType<TableRow>().Select(r => r.Item)];

    private TableRow? FindRow(object item) => _all.FirstOrDefault(r => ReferenceEquals(r.Item, item));

    // ---------- columns ----------
    private void Columns_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
            foreach (TableColumn c in e.OldItems) c.PropertyChanged -= Column_PropertyChanged;
        if (e.NewItems != null)
            foreach (TableColumn c in e.NewItems) c.PropertyChanged += Column_PropertyChanged;

        UpdateTotalWidth();

        // every row's cells depend on the column list, so they have to be recreated
        RebuildRows(recreateRows: true);
    }

    private void Column_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(TableColumn.DisplayWidth):
                UpdateTotalWidth();
                break;

            case nameof(TableColumn.SortDirection) when sender is TableColumn col:
                OnSortDirectionChanged(col);
                break;
        }
    }

    private void UpdateTotalWidth() => TotalWidth = Columns.Sum(c => c.DisplayWidth);

    private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TableColumn col })
            col.Resize(e.HorizontalChange);
    }

    // ---------- items ----------
    private void OnItemsSourceChanged(object? newSource)
    {
        if (_observedSource != null)
            _observedSource.CollectionChanged -= Source_CollectionChanged;

        _observedSource = newSource as INotifyCollectionChanged;

        if (_observedSource != null)
            _observedSource.CollectionChanged += Source_CollectionChanged;

        RebuildRows(recreateRows: true);
    }

    private void Source_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems != null && e.NewStartingIndex >= 0:
                var index = e.NewStartingIndex;
                foreach (var item in e.NewItems)
                {
                    if (item == null) continue;
                    var row = new TableRow(this, item);
                    _all.Insert(Math.Min(index++, _all.Count), row);
                    InsertIntoView(row);
                }
                break;

            case NotifyCollectionChangedAction.Remove when e.OldItems != null:
                foreach (var item in e.OldItems)
                {
                    if (item == null || FindRow(item) is not { } row) continue;
                    _all.Remove(row);
                    Rows.Remove(row);
                }
                break;

            default: // replace / move / reset
                RebuildRows();
                break;
        }
    }

    /// <summary>Re-reads the source and re-applies the sort.</summary>
    public void Refresh() => RebuildRows();

    private void RebuildRows(bool recreateRows = false)
    {
        var selected = SelectedItem;

        // reuse existing TableRows (and so their realized cells) unless columns changed
        var pool = new Dictionary<object, Queue<TableRow>>(ReferenceEqualityComparer.Instance);
        if (!recreateRows)
        {
            foreach (var row in _all)
            {
                if (!pool.TryGetValue(row.Item, out var queue))
                    pool[row.Item] = queue = new Queue<TableRow>();
                queue.Enqueue(row);
            }
        }

        _all.Clear();
        if (ItemsSource is IEnumerable source)
        {
            foreach (var item in source)
            {
                if (item == null) continue;
                _all.Add(pool.TryGetValue(item, out var queue) && queue.Count > 0
                    ? queue.Dequeue()
                    : new TableRow(this, item));
            }
        }

        ApplySort(selected);
    }

    // ---------- sorting (works on the data, never on the visual tree) ----------

    /// <summary>The column that is currently sorted, if any.</summary>
    public TableColumn? SortColumn =>
        Columns.FirstOrDefault(c => c.SortDirection != TableSortDirection.None && c.CanSort);

    private void Header_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: TableColumn col }) return;

        col.SortDirection = col.SortDirection switch
        {
            TableSortDirection.None => TableSortDirection.Ascending,
            TableSortDirection.Ascending => TableSortDirection.Descending,
            _ => TableSortDirection.None
        };
    }

    private void OnSortDirectionChanged(TableColumn changed)
    {
        if (_updatingSort) return;

        _updatingSort = true;
        try
        {
            // single-column sort: clear every other column's arrow
            if (changed.SortDirection != TableSortDirection.None)
                foreach (var other in Columns.Where(c => c != changed))
                    other.SortDirection = TableSortDirection.None;
        }
        finally { _updatingSort = false; }

        ApplySort(SelectedItem);
    }

    private void ApplySort(object? selectedItem)
    {
        IEnumerable<TableRow> ordered = _all;

        if (SortColumn is { } col)
        {
            var comparer = GetComparer(col);
            var path = col.EffectiveSortPath;

            // read each row's key once, then let a stable LINQ sort do the work
            var keyed = _all.Select(r => (Row: r, Key: ValueReader.Read(r.Item, path)));
            ordered = (col.SortDirection == TableSortDirection.Ascending
                    ? keyed.OrderBy(k => k.Key, comparer)
                    : keyed.OrderByDescending(k => k.Key, comparer))
                .Select(k => k.Row);
        }

        _suppressSelection = true;
        try
        {
            Rows.ReplaceAll(ordered.ToList());
            if (selectedItem != null)
                RowsList.SelectedItem = FindRow(selectedItem);
        }
        finally { _suppressSelection = false; }
    }

    // a single new item goes straight to its sorted position instead of re-sorting everything
    private void InsertIntoView(TableRow row)
    {
        if (SortColumn is not { } col)
        {
            Rows.Insert(Math.Min(_all.IndexOf(row), Rows.Count), row);
            return;
        }

        var comparer = GetComparer(col);
        var path = col.EffectiveSortPath;
        var sign = col.SortDirection == TableSortDirection.Ascending ? 1 : -1;
        var key = ValueReader.Read(row.Item, path);

        int lo = 0, hi = Rows.Count;
        while (lo < hi)
        {
            var mid = (lo + hi) / 2;
            var midKey = ValueReader.Read(Rows[mid].Item, path);
            if (sign * comparer.Compare(midKey, key) <= 0) lo = mid + 1;
            else hi = mid;
        }
        Rows.Insert(lo, row);
    }

    private static IComparer<object?> GetComparer(TableColumn col) =>
        col.Comparer is { } custom
            ? Comparer<object?>.Create((a, b) => custom.Compare(a, b))
            : DefaultValueComparer.Instance;

    // ---------- keep the header scrolled with the rows ----------
    private ScrollViewer? _listScroller;

    private void RowsList_Loaded(object sender, RoutedEventArgs e)
    {
        if (_listScroller != null) return;
        _listScroller = FindDescendant<ScrollViewer>(RowsList);

        if (_listScroller != null)
        {
            _listScroller.ViewChanged += (_, _) =>
                HeaderScroller.ChangeView(_listScroller.HorizontalOffset, null, null, true);

            _listScroller.PointerWheelChanged += OnScrollPointerWheelChanged;
        }
    }


    private void HeaderList_Loaded(object sender, RoutedEventArgs e)
    {
        if (HeaderScroller != null)
        {
            HeaderScroller.PointerWheelChanged += OnScrollPointerWheelChanged;
        }
    }

    private void OnScrollPointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(null);
        var mouseWheelDelta = pointerPoint.Properties.MouseWheelDelta;

        if (mouseWheelDelta != 0 && sender is ScrollViewer scroller)
        {
            double scrollFactor = 0.4;
            double currentOffset = scroller.HorizontalOffset;

            // כיוון הגלגלת הפוך בין ציר Y לX, לכן מינוס
            double targetOffset = currentOffset - (mouseWheelDelta * scrollFactor);

            targetOffset = Math.Max(0, Math.Min(targetOffset, scroller.ScrollableWidth));

            // הפעלת ChangeView עם פרמטרים חלקים
            scroller.ChangeView(targetOffset, scroller.VerticalOffset, null, true);

            // סינכרון מיידי אבל רך לרכיב השני
            if (scroller == HeaderScroller && _listScroller != null)
            {
                _listScroller.ChangeView(targetOffset, _listScroller.VerticalOffset, null, true);
            }
            else if (scroller == _listScroller && HeaderScroller != null)
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

    private void Thumb_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TableColumn col })
        {
            col.Width = GetMaxWidth(col);
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

/// <summary>Reads a (dotted) property path from an item, caching the reflection lookups.</summary>
internal static class ValueReader
{
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> Cache = new();

    public static object? Read(object? obj, string path)
    {
        foreach (var part in path.Split('.'))
        {
            if (obj == null) return null;
            var prop = Cache.GetOrAdd((obj.GetType(), part), k => k.Item1.GetProperty(k.Item2));
            obj = prop?.GetValue(obj);
        }
        return obj;
    }
}

/// <summary>Nulls first, strings case-insensitive, IComparable values by value.</summary>
internal sealed class DefaultValueComparer : IComparer<object?>
{
    public static readonly DefaultValueComparer Instance = new();

    public int Compare(object? x, object? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        if (x is string sx && y is string sy)
            return string.Compare(sx, sy, StringComparison.CurrentCultureIgnoreCase);

        if (x is IComparable cx && x.GetType() == y.GetType())
            return cx.CompareTo(y);

        return string.Compare(x.ToString(), y.ToString(), StringComparison.CurrentCultureIgnoreCase);
    }
}