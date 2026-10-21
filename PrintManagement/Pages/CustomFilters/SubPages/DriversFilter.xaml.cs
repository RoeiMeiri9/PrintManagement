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

public sealed partial class DriversFilter : Page, INotifyPropertyChanged
{
    public ObservableCollection<DriverModel> DriverModelList { get; set; } = [];

    public ItemModelCollection DriversItemModels { get; } = [];

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

    public DriversFilter()
    {
        CurrentViewMode = ViewDDSettingsService.CurrentViewMode_Filter;
        IsCompact = ViewDDSettingsService.IsCompactView;

        Loaded += DriversFilter_Loaded;
        Unloaded += DriversFilter_Unloaded;
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is ObservableCollection<DriverModel> receivedDriverModelList)
        {
            DriverModelList = receivedDriverModelList;

            DriverModelList.CollectionChanged += DriverModelList_CollectionChanged;

            RebuildItemModels();
        }
    }

    private void DriverModelList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildItemModels();
    }


    private void RebuildItemModels()
    {
        DriversItemModels.Clear();
        foreach (var driver in DriverModelList)
        {
            var itemModel = new ItemModel(driver.DriverName ?? "Unknown Driver", Icons.Driver);
            DriversItemModels.Add(itemModel);
        }
    }

    private void DriversFilter_Loaded(object sender, RoutedEventArgs e)
    {
        ViewDDSettingsService.CompactViewChanged += OnCompactViewChanged;
        ViewDDSettingsService.ViewMode_FilterChanged += OnViewModeChanged;
    }

    private void DriversFilter_Unloaded(object sender, RoutedEventArgs e)
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
        AnimateCurrentView();
    }

    private void AnimateCurrentView()
    {
        UIElement targetElement = CurrentViewMode == ViewMode.Table
            ? DriversTable
            : DriversIconView;

        AnimationHelpers.PlayEntranceAnimation(targetElement);
    }
}
