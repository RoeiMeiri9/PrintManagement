using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PrintManagement.Pages.CustomFilters.Filters;
using PrintManagement.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PrintManagement.Pages.CustomFilters.SubPages;

public sealed partial class DriversFilter : Page, INotifyPropertyChanged
{
    public ObservableCollection<DriversFilterModel> DriverFiltersList { get; set; } = [];

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

    public DriversFilter()
    {
        InitializeComponent();
        Loaded += DriversFilter_Loaded;
        Unloaded += DriversFilter_Unloaded;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is ObservableCollection<DriversFilterModel> receivedDriverFiltersList)
        {
            DriverFiltersList = receivedDriverFiltersList;
        }
    }

    private void DriversFilter_Loaded(object sender, RoutedEventArgs e)
    {
        ViewDDSettingsService.CompactViewChanged += OnViewModeChanged;
        IsCompact = ViewDDSettingsService.IsCompactView;
    }

    private void DriversFilter_Unloaded(object sender, RoutedEventArgs e)
    {
        ViewDDSettingsService.CompactViewChanged -= OnViewModeChanged;
    }

    private void OnViewModeChanged(object? sender, bool isCompactView)
    {
        IsCompact = isCompactView;
    }
}
