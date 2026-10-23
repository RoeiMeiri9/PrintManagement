using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
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

    private readonly List<TableColumn> _attachedColumns = [];

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
        RowsList.ContainerContentChanging += (_, args) =>
            args.ItemContainer.Padding = CompactView ? lowPadding : highPadding;
        _exportService = exportService;
        Columns.CollectionChanged += Columns_CollectionChanged;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

        RowsList.Loaded += RowsList_Loaded_Initiate_Scroll;
    }
    private bool _unloadedOnce;
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Register<ExportCsvRequestMessage>(this);

        ScheduleAutoWidth();

        if (_observedSource == null && ItemsSource is INotifyCollectionChanged)
        {
            AttachSource();
            if (_unloadedOnce) RebuildRows();
        }
        if (ItemsSource is null) TaskCompleted(TableInitialTasks.DataLoaded);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _unloadedOnce = true;
        WeakReferenceMessenger.Default.Unregister<ExportCsvRequestMessage>(this);
        DetachSource();
        StopPanning();                        // already exists in DataTable.Scroll.cs
        _headerItemsControl = null;
    }
}

public sealed partial class PanOverlay : Grid
{
    public void SetCursor(InputCursor? cursor) => ProtectedCursor = cursor;
}