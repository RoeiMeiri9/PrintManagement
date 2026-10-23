using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using PrintManagement.Helpers.CustomCollections;
using PrintManagement.Interfaces;
using PrintManagement.Models.Entity;
using PrintManagement.Models.Filter;
using PrintManagement.Pages.CustomFilters.Components.TreeViewLeaf;
using PrintManagement.Pages.CustomFilters.Helpers;
using PrintManagement.Pages.CustomFilters.SubPages;
using PrintManagement.Resources;
using PrintManagement.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PrintManagement.Pages.CustomFilters;

public sealed partial class CustomFiltersPage : Page, INavigationCommands
{
    private bool _isLoadingPrinters;
    private bool _isLoadingDrivers;
    public ObservableCollection<FilterLeaf> FiltersList { get; } = [];
    public BulkObservableCollection<PrinterModel> PrinterModelList { get; } = [];
    public BulkObservableCollection<DriverModel> DriverModelList { get; } = [];

    private readonly string _serverName = "";
    //private readonly string _serverName = @"\\WIN-HO9KT6LB37V";
    public string SelectedFilterName
    {
        get
        {
            if (FiltersTree.SelectedItem is FilterLeaf selectedLeaf)
            {
                return selectedLeaf.Name;
            }
            return "UnknownFilter";
        }
    }

    public string ActiveSubPageName => FilterSubPage.Content?.GetType().Name ?? string.Empty;

    private bool _isLoading = true;

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

    private readonly FilterNavigationHistory _navHistory = new();

    public bool _canRefreshan = true;
    public bool CanRefresh
    {
        get => _canRefreshan;
        set
        {
            if (_canRefreshan != value)
            {
                _canRefreshan = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanRefresh)));
            }
        }
    }

    public bool _canGoUp = false;
    public bool CanGoUp
    {
        get => _canGoUp;
        set
        {
            if (_canGoUp != value)
            {
                _canGoUp = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanGoUp)));
            }
        }
    }

    public bool CanGoBack => _navHistory.CanGoBack;
    public bool CanGoForward => _navHistory.CanGoForward;

    public string BackTargetName => _navHistory.BackTargetName;
    public string ForwardTargetName => _navHistory.ForwardTargetName;

    private string _upTargetName = string.Empty;
    public string UpTargetName
    {
        get => _upTargetName;
        set { if (_upTargetName != value) { _upTargetName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UpTargetName))); } }
    }

    private string _refreshTargetName = string.Empty;
    public string RefereshTargetName
    {
        get => _refreshTargetName;
        set { if (_refreshTargetName != value) { _refreshTargetName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RefereshTargetName))); } }
    }

    private static readonly string PrintersRootName = "Printers";
    private static readonly string DriversRootName = "Drivers";

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool _isInitialized = false;


    public CustomFiltersPage()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            if (_isInitialized) return;
            _isInitialized = true;

            _ = LoadAllAsync();
            await LoadFiltersAsync();
        };
    }

    // ---- Loading Drivers and Printers ----

    /// <summary>
    /// Drivers first, because the printers load reuses that list instead of asking the server again.
    /// The filter tree is built once, at the end, so it never shows a half-loaded count.
    /// </summary>
    private bool _isLoadingAll;

    private async Task LoadAllAsync()
    {
        if (_isLoadingAll) return;
        _isLoadingAll = true;
        IsLoading = true;
        CanRefresh = false;
        try
        {
            var sw = Stopwatch.StartNew();
            await LoadDriversAsync();
            await LoadPrintersAsync();
            Logger.Log($"load: {sw.ElapsedMilliseconds} ms");
        }
        finally
        {
            IsLoading = false;
            CanRefresh = true;
            _isLoadingAll = false;
        }

        UpdateFilterCountsAfterDataLoaded();
    }

    private async Task LoadPrintersAsync()
    {
        if (_isLoadingPrinters) return;
        _isLoadingPrinters = true;

        try
        {
            // Reuse the drivers page's list so the server isn't asked for the same drivers twice.
            // If that load failed (empty list), pass null and let the service fetch them itself.
            IReadOnlyList<DriverModel>? knownDrivers =
                DriverModelList.Count > 0 ? DriverModelList.ToList() : null;

            var printers = await SpoolerService.GetPrintersAsync(
                serverName: _serverName, includeConnections: true, knownDrivers: knownDrivers);

            PrinterModelList.ReplaceAll(printers);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load printers: {ex}");
            Logger.Warning($"Failed to load printers: {ex}");
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
            var drivers = await SpoolerService.GetDriversAsync(serverName: _serverName);

            DriverModelList.ReplaceAll(drivers);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load drivers: {ex}");
            Logger.Warning($"Failed to load drivers: {ex}");
        }
        finally
        {
            _isLoadingDrivers = false;
        }
    }

    // ---- Filters ----

    private readonly Guid AllPrintersLeafID = new("b83c27e3-5482-4136-9815-568285521b40");
    private readonly Guid AllDriversLeafID = new("b83c27e3-5482-4136-9815-568285521b41");

    private async Task LoadFiltersAsync()
    {
        FiltersList.Clear();

        var printerDefaults = new[]
        {
            new GenericFilterDefinition
            {
                Name = "Printers Not Ready",
                Criteria = new Dictionary<string, FilterRule>(StringComparer.OrdinalIgnoreCase)
                {
                    {
                        "QueueStatus",
                        new FilterRule
                        {
                            Condition = FilterCondition.NotEquals,
                            Value = "Ready"
                        }
                    }
                }
            },
            new GenericFilterDefinition
            {
                Name = "Printers With Jobs",
                Criteria = new Dictionary<string, FilterRule>(StringComparer.OrdinalIgnoreCase)
                {
                    {
                        "JobsInQueue",
                        new FilterRule
                        {
                            Condition = FilterCondition.GreaterThan,
                            Value = "0"
                        }
                    }
                }
            }
        };

        var printersRoot = await FilterLeafHelpers.BuildEntityRootLeafAsync<PrinterModel>(
            rootName: PrintersRootName,
            rootIcon: Icons.Printer,
            pageType: FilterPages.PrinterPage,
            totalItems: PrinterModelList.Count,
            countCalculator: filter => FilterEngine.Count(PrinterModelList, filter), // <--- כאן הקסם קורה!
            specialAllId: AllPrintersLeafID,
            defaultSeedFilters: printerDefaults
        );
        FiltersList.Add(printersRoot);

        var driversRoot = await FilterLeafHelpers.BuildEntityRootLeafAsync<DriverModel>(
            rootName: DriversRootName,
            rootIcon: Icons.Driver,
            pageType: FilterPages.DriverPage,
            totalItems: DriverModelList.Count,
            countCalculator: filter => FilterEngine.Count(DriverModelList, filter), // <--- כאן הקסם קורה!
            specialAllId: AllDriversLeafID
        );
        FiltersList.Add(driversRoot);

        SetSelectedLeaf();
    }

    private void SetSelectedLeaf()
    {
        Guid targetID = NavigationStateService.LastSelectedFilterID ?? FiltersList[0].Children.First().ID;

        FilterLeaf? selectedLeaf = FindLeaf(FiltersList, targetID);

        if (selectedLeaf != null)
        {
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                FiltersTree.SelectedItem = selectedLeaf;
            });
        }
    }

    public static FilterLeaf? FindLeaf(IEnumerable<FilterLeaf> nodes, Guid targetID)
    {
        foreach (var node in nodes)
        {
            if (node.ID == targetID)
                return node;

            var foundInDescendants = FindLeaf(node.Children, targetID);
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
            var oldLeaf = e.RemovedItems.FirstOrDefault() as FilterLeaf;

            // מעדכנים את ההיסטוריה הנקייה
            _navHistory.OnSelectionChanged(oldLeaf, leaf);

            switch (leaf.FilterPage)
            {
                case FilterPages.PrinterPage:
                    FilterSubPage.Navigate(typeof(PrintersFilter), (list: PrinterModelList, filter: leaf.Filter));
                    break;
                case FilterPages.DriverPage:
                    FilterSubPage.Navigate(typeof(DriversFilter), (list: DriverModelList, filter: leaf.Filter));
                    break;
            }

            NavigationStateService.LastSelectedFilterID = leaf.ID;
            UpdateNavigationStates(leaf);
        }
    }

    private void UpdateFilterCountsAfterDataLoaded()
    {
        foreach (var rootNode in FiltersList)
        {
            if (rootNode.FilterPage == FilterPages.PrinterPage)
            {
                if (PrinterModelList != null && PrinterModelList.Any())
                {
                    rootNode.TotalItems = PrinterModelList.Count;

                    RecalculateNodeCounts(rootNode, PrinterModelList);
                }
            }
            else if (rootNode.FilterPage == FilterPages.DriverPage)
            {
                if (DriverModelList != null && DriverModelList.Any())
                {
                    rootNode.TotalItems = DriverModelList.Count;

                    RecalculateNodeCounts(rootNode, DriverModelList);
                }
            }
        }
    }

    private void RecalculateNodeCounts<T>(FilterLeaf node, BulkObservableCollection<T> dataList)
    {
        if (node == null) return;

        if (node.Filter.Criteria != null)
        {
            node.TotalItems = FilterEngine.Count(dataList, node.Filter);
        }

        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                RecalculateNodeCounts(child, dataList);
            }
        }
    }

    // ---- Navigation ----

    private void UpdateNavigationStates(FilterLeaf? current)
    {
        if (current is not null)
        {
            CanGoUp = !current.IsRoot;
            RefereshTargetName = current.Name;
            UpTargetName = current.IsRoot ? string.Empty : (current.FilterPage == FilterPages.PrinterPage ? PrintersRootName : DriversRootName);
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanGoBack)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanGoForward)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BackTargetName)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ForwardTargetName)));
    }

    public void GoBack()
    {
        var current = FiltersTree.SelectedItem as FilterLeaf;
        _navHistory.GoBack(current, leaf => FiltersTree.SelectedItem = leaf);
    }

    public void GoForward()
    {
        var current = FiltersTree.SelectedItem as FilterLeaf;
        _navHistory.GoForward(current, leaf => FiltersTree.SelectedItem = leaf);
    }

    public void GoUp()
    {
        if (FiltersTree.SelectedItem is FilterLeaf leaf && !leaf.IsRoot)
        {
            string targetRootName = leaf.FilterPage == FilterPages.PrinterPage ? PrintersRootName : DriversRootName;
            var rootLeaf = FiltersList.FirstOrDefault(f => f.Name == targetRootName);

            if (rootLeaf != null)
            {
                _navHistory.GoTo(leaf, rootLeaf, target => FiltersTree.SelectedItem = target);
            }
        }
    }

    public async void Refresh()
    {
        try { await LoadAllAsync(); }
        catch (Exception ex) { Logger.Error(ex); }
    }
}