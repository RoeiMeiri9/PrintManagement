using Microsoft.UI.Xaml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace PrintManagement.Controls.Table
{
    public partial class DataTable
    {
        // ---------- Items ----------
        public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(DataTable),
            new PropertyMetadata(null, (d, e) => ((DataTable)d).OnItemsSourceChanged(e.NewValue)));

        public object? ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

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

        // ---------- Columns ----------
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

        // ---------- Rows ----------
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
    }
}
