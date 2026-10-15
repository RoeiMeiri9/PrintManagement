using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace PrintManagement.Controls.Table;

// These are plain data holders. All the UI for a row/cell lives in DataTable.xaml.

/// <summary>One row of the table: the original item plus one cell per column.</summary>
public sealed class TableRow
{
    // observable at runtime, so the row's ItemsControl follows a column being moved
    private readonly ObservableCollection<TableCell> _cells;

    internal TableRow(DataTable table, object item)
    {
        Table = table;
        Item = item;
        _cells = [.. table.Columns.Select(c => new TableCell(c, item))];
    }

    public DataTable Table { get; }
    public object Item { get; }
    public IReadOnlyList<TableCell> Cells => _cells;

    internal void MoveCell(int oldIndex, int newIndex) => _cells.Move(oldIndex, newIndex);
}

/// <summary>Pairs a column with the row item, so a cell template can see both.</summary>
public sealed class TableCell
{
    internal TableCell(TableColumn column, object item)
    {
        Column = column;
        Item = item;
    }

    public TableColumn Column { get; }
    public object Item { get; }
}

/// <summary>Rows as shown by the ListView. Can be replaced in one go (single Reset event).</summary>
public sealed partial class TableRowCollection : ObservableCollection<TableRow>
{
    internal void ReplaceAll(IEnumerable<TableRow> rows)
    {
        CheckReentrancy();
        Items.Clear();
        foreach (var row in rows) Items.Add(row);

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}