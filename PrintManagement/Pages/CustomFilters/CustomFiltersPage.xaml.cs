using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PrintManagement.Pages.CustomFilters.Components.TreeViewLeaf;
using PrintManagement.Pages.CustomFilters.Filters;
using PrintManagement.Pages.CustomFilters.Helpers;
using PrintManagement.Pages.CustomFilters.SubPages;
using PrintManagement.Resources;
using PrintManagement.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;


namespace PrintManagement.Pages.CustomFilters;

public sealed partial class CustomFiltersPage : Page
{
    public FilterLeaf? SelectedFilterLeaf;
    private bool _isLoadingPrinters;
    private bool _isLoadingDrivers;
    public ObservableCollection<FilterLeaf> FiltersList { get; } = [];
    public ObservableCollection<PrinterFilterModel> PrinterFiltersList { get; } = [];
    public ObservableCollection<DriversFilterModel> DriverFiltersList { get; } = [];

    public CustomFiltersPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadPrintersAsync();
        Loaded += async (_, _) => await LoadDriversAsync();
    }


    private async Task LoadPrintersAsync()
    {
        if (_isLoadingPrinters) return;
        _isLoadingPrinters = true;

        try
        {
            var printers = await SpoolerPrinterService.GetPrintersAsync(includeConnections: true);

            PrinterFiltersList.Clear();
            foreach (var p in printers)
                PrinterFiltersList.Add(p);

            LoadFilters();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load printers: {ex}");
        }
        finally
        {
            _isLoadingPrinters = false;
        }
    }

    private async Task LoadDriversAsync()
    {
        if (_isLoadingDrivers) return;
        _isLoadingDrivers = true;

        try
        {
            var drivers = await SpoolerPrinterService.GetDriversAsync();

            DriverFiltersList.Clear();
            foreach (var d in drivers)
                DriverFiltersList.Add(d);

            LoadFilters();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load drivers: {ex}");
        }
        finally
        {
            _isLoadingDrivers = false;
        }
    }

    private void LoadFilters()
    {
        // --- Printers Root Leaf ---
        FiltersList.Clear();
        var printersRoot = new FilterLeaf
        {
            Name = "Printers",
            DisplayName = "Printers",
            LeafIcon = Icons.Printer,
            FilterPage = FilterPages.PrinterPage
        };

        // Adding sub leafs

        printersRoot.Children.Add(new FilterLeaf
        {
            Name = "AllPrinters",
            DisplayName = "All Printers",
            LeafIcon = Icons.Filter,
            DisplayTotalItems = Visibility.Visible,
            TotalItems = PrinterFiltersList.Count,
            FilterPage = FilterPages.PrinterPage
        });
        printersRoot.Children.Add(new FilterLeaf
        {
            Name = "PrintersNorReady",
            DisplayName = "Printers Nor Ready",
            LeafIcon = Icons.Filter,
            FilterPage = FilterPages.PrinterPage

        });
        printersRoot.Children.Add(new FilterLeaf
        {
            Name = "PrintersWithJobs",
            DisplayName = "Printers With Jobs",
            LeafIcon = Icons.Filter,
            FilterPage = FilterPages.PrinterPage

        });

        FiltersList.Add(printersRoot);

        // --- Drivers Root Leaf ---
        var driversRoot = new FilterLeaf
        {
            Name = "Drivers",
            DisplayName = "Drivers",
            LeafIcon = Icons.Driver,
            FilterPage = FilterPages.DriverPage
        };

        // Adding sub leafs

        driversRoot.Children.Add(new FilterLeaf
        {
            Name = "AllDrivers",
            DisplayName = "All Drivers",
            LeafIcon = Icons.Filter,
            DisplayTotalItems = Visibility.Visible,
            TotalItems = DriverFiltersList.Count,
            FilterPage = FilterPages.DriverPage
        });

        FiltersList.Add(driversRoot);

        SetSelectedLeaf();
    }

    private void SetSelectedLeaf()
    {
        string targetName = NavigationStateService.LastSelectedFilterName ?? "AllPrinters";

        FilterLeaf? selectedLeaf = FindLeaf(FiltersList, targetName, FiltersTree);

        if (selectedLeaf != null)
        {
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                FiltersTree.SelectedItem = selectedLeaf;
            });
        }
    }
    public static FilterLeaf? FindLeaf(IEnumerable<FilterLeaf> nodes, string targetName, TreeView FiltersTree)
    {
        foreach (var node in nodes)
        {
            if (node.Name == targetName)
                return node;

            var foundInDescendants = FindLeaf(node.Children, targetName, FiltersTree);
            if (foundInDescendants != null)
            {
                node.IsExpanded = true;
                return foundInDescendants;
            }
        }
        return null;
    }

    private void FiltersTree_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is FilterLeaf leaf)
        {
            switch (leaf.FilterPage)
            {
                case FilterPages.PrinterPage:
                    FilterSubPage.Navigate(typeof(PrintersFilter), PrinterFiltersList);
                    break;
                case FilterPages.DriverPage:
                    FilterSubPage.Navigate(typeof(DriversFilter), DriverFiltersList);
                    break;
            }
        }
    }
}
