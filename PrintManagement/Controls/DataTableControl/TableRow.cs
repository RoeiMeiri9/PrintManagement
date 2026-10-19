using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace PrintManagement.Controls.DataTableControl;

// These are plain data holders. All the UI for a row/cell lives in DataTable.xaml.

/// <summary>
/// Represents a single row within the table.
/// Bridges the underlying raw domain object (e.g., PrinterModel) with the UI cells.
/// </summary>
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

    /// <summary>
    /// Gets the parent <see cref="DataTable"/> control containing this row.
    /// </summary>
    public DataTable Table { get; }

    /// <summary>
    /// Gets the original data model instance from <see cref="DataTable.ItemsSource"/> (e.g., a specific printer or driver instance).
    /// Used by column binding paths to extract display values.
    /// </summary>
    public object Item { get; }

    /// <summary>
    /// Gets the collection of cells in this row. Each cell corresponds to a specific table column.
    /// </summary>
    public IReadOnlyList<TableCell> Cells => _cells;

    internal void MoveCell(int oldIndex, int newIndex) => _cells.Move(oldIndex, newIndex);
}

/// <summary>
/// Represents a single cell in a row.
/// Pairs column metadata (<see cref="TableColumn"/>) with the row's data object (<see cref="TableRow.Item"/>).
/// </summary>
public sealed class TableCell
{
    internal TableCell(TableColumn column, object item)
    {
        Column = column;
        Item = item;
    }

    /// <summary>
    /// Gets the column definition for this cell (contains width, visibility, and layout templates).
    /// </summary
    public TableColumn Column { get; }

    /// <summary>
    /// Gets the raw data model instance belonging to the parent row.
    /// </summary>
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