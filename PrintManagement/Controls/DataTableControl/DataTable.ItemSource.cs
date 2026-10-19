using Microsoft.UI.Xaml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace PrintManagement.Controls.DataTableControl
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
                    var startingIndex = e.NewStartingIndex;
                    foreach (var item in e.NewItems)
                    {
                        if (item == null) continue;
                        var row = new TableRow(this, item);
                        _originalUnsortedRows.Insert(Math.Min(startingIndex++, _originalUnsortedRows.Count), row);
                        InsertIntoView(row);
                    }
                    DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        RecalculateAutoWidths();
                    });
                    break;

                case NotifyCollectionChangedAction.Remove when e.OldItems != null:
                    foreach (var item in e.OldItems)
                    {
                        if (item == null || FindRow(item) is not { } row) continue;
                        _originalUnsortedRows.Remove(row);
                        Rows.Remove(row);
                    }
                    break;


                default: // replace / move / reset
                    RebuildRows();
                    break;
            }
        }

        /// <summary>
        /// Inserts a newly added row directly into its sorted position within <see cref="Rows"/>.
        /// Uses binary search to pinpoint the target index in O(log N) time, avoiding a full table re-sort.
        /// </summary>
        /// <param name="row">The new <see cref="TableRow"/> instance to insert.</param>
        private void InsertIntoView(TableRow row)
        {
            // If no column is currently sorted, append relative to the unsorted source order.
            if (SortColumn is not { } col)
            {
                Rows.Insert(Math.Min(_originalUnsortedRows.IndexOf(row), Rows.Count), row);
                return;
            }

            var comparer = GetComparer(col);
            var path = col.EffectiveSortPath;
            var sign = col.SortDirection == TableSortDirection.Ascending ? 1 : -1;

            // Extract the sort key from the new row's data model.
            var key = ValueReader.Read(row.Item, path);

            // Binary search to find the correct sorted insertion index.
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
            // a reorder: same columns, new order. Just move each row's cell; no rebuild, same total width.
            if (e.Action == NotifyCollectionChangedAction.Move)
            {
                foreach (var row in _originalUnsortedRows)
                    row.MoveCell(e.OldStartingIndex, e.NewStartingIndex);
                return;
            }

            if (e.OldItems != null)
            {
                foreach (TableColumn c in e.OldItems)
                {
                    c.PropertyChanged -= Column_PropertyChanged;
                    c.Owner = null;
                }
            }
            if (e.NewItems != null)
            {
                foreach (TableColumn c in e.NewItems)
                {
                    c.PropertyChanged += Column_PropertyChanged;
                    c.Owner = this;
                }
            }

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
            var _reusableRowCache = new Dictionary<object, Queue<TableRow>>(ReferenceEqualityComparer.Instance);
            if (!recreateRows)
            {
                foreach (var row in _originalUnsortedRows)
                {
                    if (!_reusableRowCache.TryGetValue(row.Item, out var queue))
                        _reusableRowCache[row.Item] = queue = new Queue<TableRow>();
                    queue.Enqueue(row);
                }
            }

            _originalUnsortedRows.Clear();
            if (ItemsSource is IEnumerable rawItemsSource)
            {
                foreach (var item in rawItemsSource)
                {
                    if (item == null) continue;
                    _originalUnsortedRows.Add(_reusableRowCache.TryGetValue(item, out var queue) && queue.Count > 0
                        ? queue.Dequeue()
                        : new TableRow(this, item));
                }
            }

            ApplySort(selected);
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                RecalculateAutoWidths();
            });
        }
    }
}