using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PrintManagement.Controls.IconGridViewControl;
using PrintManagement.Helpers.ControlHelpers;
using PrintManagement.Models;
using PrintManagement.Models.enums;
using PrintManagement.Resources;
using PrintManagement.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace PrintManagement.Pages.CustomFilters.SubPages;

public sealed partial class PrintersFilter : Page, INotifyPropertyChanged
{
    public ObservableCollection<PrinterModel> PrinterModelList { get; set; } = [];

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

    public PrintersFilter()
    {
        IsCompact = ViewDDSettingsService.IsCompactView;
        CurrentViewMode = ViewDDSettingsService.CurrentViewMode;
        Loaded += PrintersFilter_Loaded;
        Unloaded += PrintersFilter_Unloaded;
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is ObservableCollection<PrinterModel> receivedPrinterModelList)
        {
            PrinterModelList = receivedPrinterModelList;

            PrinterModelList.CollectionChanged += PrinterModelList_CollectionChanged;

            RebuildItemModels();
        }
    }

    private void PrinterModelList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildItemModels();
    }


    private void RebuildItemModels()
    {
        PrinterItemModels.Clear();
        foreach (var printer in PrinterModelList)
        {
            var itemModel = new ItemModel(printer.Name ?? "Unknown Printer", Icons.Printer);
            PrinterItemModels.Add(itemModel);
        }
    }

    private void PrintersFilter_Loaded(object sender, RoutedEventArgs e)
    {
        ViewDDSettingsService.CompactViewChanged += OnCompactViewChanged;
        ViewDDSettingsService.ViewModeChanged += OnViewModeChanged;
    }

    private void PrintersFilter_Unloaded(object sender, RoutedEventArgs e)
    {
        ViewDDSettingsService.CompactViewChanged -= OnCompactViewChanged;
        ViewDDSettingsService.ViewModeChanged += OnViewModeChanged;
    }

    private void OnCompactViewChanged(object? sender, bool isCompactView)
    {
        IsCompact = isCompactView;
    }

    private void OnViewModeChanged(object? sender, ViewMode viewMode)
    {
        CurrentViewMode = viewMode;
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