using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PrintManagement.Models;
using PrintManagement.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PrintManagement.Pages.CustomFilters.SubPages;

public sealed partial class PrintersFilter : Page, INotifyPropertyChanged
{
    public ObservableCollection<PrinterModel> PrinterModelList { get; set; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;
    public bool _isCompact = false;
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

    public PrintersFilter()
    {
        InitializeComponent();
        Loaded += PrintersFilter_Loaded;
        Unloaded += PrintersFilter_Unloaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is ObservableCollection<PrinterModel> receivedPrinterModelList)
        {
            PrinterModelList = receivedPrinterModelList;
        }
    }

    private void PrintersFilter_Loaded(object sender, RoutedEventArgs e)
    {
        ViewDDSettingsService.CompactViewChanged += OnViewModeChanged;
        IsCompact = ViewDDSettingsService.IsCompactView;
    }

    private void PrintersFilter_Unloaded(object sender, RoutedEventArgs e)
    {
        ViewDDSettingsService.CompactViewChanged -= OnViewModeChanged;
    }

    private void OnViewModeChanged(object? sender, bool isCompactView)
    {
        IsCompact = isCompactView;
    }
}