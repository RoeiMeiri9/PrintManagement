using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrintManagement.Controls.Table
{
    public partial class DataTable
    {
        private bool _updatingSort;

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
        private static IComparer<object?> GetComparer(TableColumn col) =>
    col.Comparer is { } custom
        ? Comparer<object?>.Create((a, b) => custom.Compare(a, b))
        : DefaultValueComparer.Instance;
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