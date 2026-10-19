using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using PrintManagement.Services;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace PrintManagement.Controls.DataTableControl;

[ContentProperty(Name = nameof(Columns))]
public sealed partial class DataTable : UserControl, IRecipient<ExportCsvRequestMessage>
{
    public TableColumnCollection Columns { get; } = [];

    /// <summary>What the ListView shows: one TableRow per item, already in sorted order.</summary>
    public TableRowCollection Rows { get; } = [];

    // every row in the order of the source collection (the unsorted truth)
    private readonly List<TableRow> _originalUnsortedRows = [];

    private INotifyCollectionChanged? _observedSource;

    public DataTable() : this(App.GetService<IExportService>())
    {
    }

    public DataTable(IExportService exportService)
    {
        InitializeComponent();
        Loaded += OnTableLoaded_SetAutoWidth();
        Loaded += OnTableLoaded_ApplyCompactViewStyle();
        Columns.CollectionChanged += Columns_CollectionChanged;
        _exportService = exportService;

        Loaded += (s, e) => WeakReferenceMessenger.Default.Register<ExportCsvRequestMessage>(this);
        Unloaded += (s, e) => WeakReferenceMessenger.Default.Unregister<ExportCsvRequestMessage>(this);
    }
}

public sealed partial class PanOverlay : Grid
{
    public void SetCursor(InputCursor? cursor) => ProtectedCursor = cursor;
}