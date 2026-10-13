using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace PrintManagement.Controls.Table;

[ContentProperty(Name = nameof(Columns))]
public sealed partial class DataTable : UserControl
{
    public TableColumnCollection Columns { get; } = [];

    /// <summary>What the ListView shows: one TableRow per item, already in sorted order.</summary>
    public TableRowCollection Rows { get; } = [];

    // every row in the order of the source collection (the unsorted truth)
    private readonly List<TableRow> _all = [];

    private INotifyCollectionChanged? _observedSource;

    public DataTable()
    {
        InitializeComponent();
        Columns.CollectionChanged += Columns_CollectionChanged;
    }
}

public sealed partial class PanOverlay : Grid
{
    public void SetCursor(InputCursor? cursor) => ProtectedCursor = cursor;
}