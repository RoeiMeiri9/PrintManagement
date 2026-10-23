using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PrintManagement.Helpers.CustomCollections;
using PrintManagement.Interfaces;
using PrintManagement.Models;
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

    //private readonly string _serverName = "";
    private readonly string _serverName = @"\\WIN-HO9KT6LB37V";
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



    private readonly Stack<FilterLeaf> _backStack = new();
    private readonly Stack<FilterLeaf> _forwardStack = new();
    private bool _isNavigatingHistory = false;
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
    public bool CanGoBack => _backStack.Count > 0;
    public bool CanGoForward => _forwardStack.Count > 0;


    private string _backTargetName = string.Empty;
    public string BackTargetName
    {
        get => _backTargetName;
        set { if (_backTargetName != value) { _backTargetName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BackTargetName))); } }
    }

    private string _forwardTargetName = string.Empty;
    public string ForwardTargetName
    {
        get => _forwardTargetName;
        set { if (_forwardTargetName != value) { _forwardTargetName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ForwardTargetName))); } }
    }

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
        Loaded += (s, e) =>
        {
            if (_isInitialized) return;
            _isInitialized = true;

            LoadFilters();
            _ = LoadAllAsync();
        };
    }

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

    private void LoadFilters()
    {
        // --- Printers Root Leaf ---
        FiltersList.Clear();
        var printersRoot = new FilterLeaf
        {
            Name = PrintersRootName,
            DisplayName = "Printers",
            LeafIcon = Icons.Printer,
            FilterPage = FilterPages.PrinterPage,
            IsRoot = true
        };

        // Adding sub leafs

        var allPrintersLeaf = new FilterLeaf
        {
            Name = "AllPrinters",
            DisplayName = "All Printers",
            LeafIcon = Icons.Filter,
            DisplayTotalItems = Visibility.Visible,
            TotalItems = PrinterModelList.Count,
            FilterPage = FilterPages.PrinterPage
        };

        PrinterModelList.CollectionChanged += (s, args) =>
        {
            allPrintersLeaf.TotalItems = PrinterModelList.Count;
        };

        printersRoot.Children.Add(allPrintersLeaf);

        printersRoot.Children.Add(new FilterLeaf
        {
            Name = "PrintersNotReady",
            DisplayName = "Printers Not Ready",
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
            Name = DriversRootName,
            DisplayName = "Drivers",
            LeafIcon = Icons.Driver,
            FilterPage = FilterPages.DriverPage,
            IsRoot = true
        };

        // Adding sub leafs

        var allDriversLeaf = new FilterLeaf
        {
            Name = "AllDrivers",
            DisplayName = "All Drivers",
            LeafIcon = Icons.Filter,
            DisplayTotalItems = Visibility.Visible,
            TotalItems = DriverModelList.Count,
            FilterPage = FilterPages.DriverPage
        };

        DriverModelList.CollectionChanged += (s, args) =>
        {
            allDriversLeaf.TotalItems = DriverModelList.Count;
        };

        driversRoot.Children.Add(allDriversLeaf);

        FiltersList.Add(driversRoot);

        SetSelectedLeaf();
    }

    private void SetSelectedLeaf()
    {
        string targetName = NavigationStateService.LastSelectedFilterName ?? "AllPrinters";

        FilterLeaf? selectedLeaf = FindLeaf(FiltersList, targetName);

        if (selectedLeaf != null)
        {
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                FiltersTree.SelectedItem = selectedLeaf;
            });
        }
    }
    public static FilterLeaf? FindLeaf(IEnumerable<FilterLeaf> nodes, string targetName)
    {
        foreach (var node in nodes)
        {
            if (node.Name == targetName)
                return node;

            var foundInDescendants = FindLeaf(node.Children, targetName);
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
            if (!_isNavigatingHistory && e.RemovedItems.FirstOrDefault() is FilterLeaf oldLeaf && oldLeaf != leaf)
            {
                _backStack.Push(oldLeaf);
                _forwardStack.Clear();
            }

            switch (leaf.FilterPage)
            {
                case FilterPages.PrinterPage:
                    FilterSubPage.Navigate(typeof(PrintersFilter), PrinterModelList);
                    break;
                case FilterPages.DriverPage:
                    FilterSubPage.Navigate(typeof(DriversFilter), DriverModelList);
                    break;
            }

            NavigationStateService.LastSelectedFilterName = leaf.Name;

            UpdateNavigationStates(leaf);
        }
    }

    private void UpdateNavigationStates(FilterLeaf? current)
    {
        if (current is not null)
        {
            CanGoUp = !current.IsRoot;
            RefereshTargetName = current.DisplayName;
            UpTargetName = current.IsRoot
                ? string.Empty
                : (current.FilterPage == FilterPages.PrinterPage ? PrintersRootName : DriversRootName);
        }

        BackTargetName = _backStack.Count > 0 ? _backStack.Peek().DisplayName : string.Empty;
        ForwardTargetName = _forwardStack.Count > 0 ? _forwardStack.Peek().DisplayName : string.Empty;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanGoBack)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanGoForward)));
    }

    public void GoBack()
    {
        if (_backStack.Count == 0) return;

        if (FiltersTree.SelectedItem is FilterLeaf currentLeaf)
        {
            _forwardStack.Push(currentLeaf); // דוחפים את הנוכחי לקדימה
        }

        var previousLeaf = _backStack.Pop();
        _isNavigatingHistory = true;
        FiltersTree.SelectedItem = previousLeaf; // מעבר לבחירה הקודמת
        _isNavigatingHistory = false;

        UpdateNavigationStates(previousLeaf);
    }

    public void GoForward()
    {
        if (_forwardStack.Count == 0) return;

        if (FiltersTree.SelectedItem is FilterLeaf currentLeaf)
        {
            _backStack.Push(currentLeaf); // דוחפים את הנוכחי לאחורה
        }

        var nextLeaf = _forwardStack.Pop();
        _isNavigatingHistory = true;
        FiltersTree.SelectedItem = nextLeaf; // מעבר לבחירה הבאה
        _isNavigatingHistory = false;

        UpdateNavigationStates(nextLeaf);
    }

    public void GoUp()
    {
        if (FiltersTree.SelectedItem is FilterLeaf leaf)
        {
            string targetName = leaf.FilterPage switch
            {
                FilterPages.PrinterPage => PrintersRootName,
                _ => DriversRootName
            };

            FilterLeaf? selectedLeaf = FindLeaf(FiltersList, targetName);
            if (selectedLeaf != null && selectedLeaf != leaf)
            {
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    _backStack.Push(leaf);
                    _forwardStack.Clear();

                    _isNavigatingHistory = true;
                    FiltersTree.SelectedItem = selectedLeaf;
                    _isNavigatingHistory = false;

                    UpdateNavigationStates(selectedLeaf);
                });
            }
        }
    }

    public async void Refresh()
    {
        try { await LoadAllAsync(); }
        catch (Exception ex) { Logger.Error(ex); }
    }
}