using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PrintManagement.Controls.IconGridViewControl;
using PrintManagement.Helpers.ControlHelpers;
using PrintManagement.Helpers.CustomCollections;
using PrintManagement.Models;
using PrintManagement.Models.enums;
using PrintManagement.Resources;
using PrintManagement.Services;
using System;
using System.Collections.Specialized;
using System.ComponentModel;

namespace PrintManagement.Pages.CustomFilters.SubPages;

public sealed partial class PrintersFilter : Page, INotifyPropertyChanged
{
    public BulkObservableCollection<PrinterModel> PrinterModelList { get; set; } = [];

    public ItemModelCollection PrinterItemModels { get; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool _isCompact = false;

    public bool IsCompact
    {
        get => _isCompact;
        set
        {
            if (_isCompact != value)
            {
                _isCompact = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCompact)));
            }
        }
    }

    private ViewMode _currentView;

    public ViewMode CurrentViewMode
    {
        get => _currentView;
        set
        {
            if (_currentView != value)
            {
                _currentView = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentViewMode)));
            }
        }
    }

    private bool _isLoading = true; // מתחיל טעינה כבר בעלייה
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoading)));
            }
        }
    }

    bool _hasTableEntered = false;

    public PrintersFilter()
    {
        IsCompact = ViewDDSettingsService.IsCompactView;
        CurrentViewMode = ViewDDSettingsService.CurrentViewMode_Filter;
        Loaded += PrintersFilter_Loaded;
        Unloaded += PrintersFilter_Unloaded;
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is BulkObservableCollection<PrinterModel> receivedPrinterModelList)
        {
            PrinterModelList = receivedPrinterModelList;
            PrinterModelList.CollectionChanged += PrinterModelList_CollectionChanged;
            PrintersTable.TableReadyForAnimation += CallAnimateCurrentView;

            RebuildItemModels();
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        PrinterModelList?.CollectionChanged -= PrinterModelList_CollectionChanged;
        PrintersTable.TableReadyForAnimation -= CallAnimateCurrentView;
    }

    private void CallAnimateCurrentView(object? sender, EventArgs e)
    {
        PrintersTable.Opacity = 1;
        PrintersTable.IsHitTestVisible = true;
        AnimateCurrentView();
    }

    private void PrinterModelList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildItemModels();
        if (!_hasTableEntered)
        {
            _hasTableEntered = true;
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                AnimateCurrentView();
            });
        }

    }

    private void RebuildItemModels()
    {
        PrinterItemModels.Clear();
        foreach (var printer in PrinterModelList)
        {
            var itemModel = new ItemModel(printer.Name ?? "Unknown Printer", Icons.Printer_Filled);
            PrinterItemModels.Add(itemModel);
        }
    }

    private void PrintersFilter_Loaded(object sender, RoutedEventArgs e)
    {
        ViewDDSettingsService.CompactViewChanged += OnCompactViewChanged;
        ViewDDSettingsService.ViewMode_FilterChanged += OnViewModeChanged;
    }

    private void PrintersFilter_Unloaded(object sender, RoutedEventArgs e)
    {
        ViewDDSettingsService.CompactViewChanged -= OnCompactViewChanged;
        ViewDDSettingsService.ViewMode_FilterChanged -= OnViewModeChanged;
    }

    private void OnCompactViewChanged(object? sender, bool isCompactView)
    {
        IsCompact = isCompactView;
    }

    private void OnViewModeChanged(object? sender, ViewMode viewMode)
    {
        CurrentViewMode = viewMode;
        if (_hasTableEntered)
            AnimateCurrentView();
    }

    private void AnimateCurrentView()
    {
        UIElement targetElement = CurrentViewMode == ViewMode.Table
            ? PrintersTable
            : PrintersIconView;

        AnimationHelpers.PlayEntranceAnimation(targetElement);
    }
}