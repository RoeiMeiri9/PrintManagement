using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PrintManagement.Pages.CustomFilters;

public sealed partial class CustomFiltersPage : Page, INavigationCommands
{
    #region Dependency Properties
    private bool _isLoadingPrinters;
    private bool _isLoadingDrivers;
    public ObservableCollection<FilterLeaf> FiltersList { get; } = [];
    public BulkObservableCollection<PrinterModel> PrinterModelList { get; } = [];
    public BulkObservableCollection<DriverModel> DriverModelList { get; } = [];

    private PrinterWatcher? _watcher;
    private CancellationTokenSource? _watcherCts;
    private DispatcherQueueTimer? _recountTimer;

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

    public bool _canRefresh = true;
    public bool CanRefresh
    {
        get => _canRefresh;
        set
        {
            if (_canRefresh != value)
            {
                _canRefresh = value;
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

    public string RefreshTargetName
    {
        get;
        set { if (field != value) { field = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RefreshTargetName))); } }
    } = string.Empty;

    private static readonly string PrintersRootName = "Printers";
    private static readonly string DriversRootName = "Drivers";

    private readonly Guid AllPrintersLeafID = new("b83c27e3-5482-4136-9815-568285521b40");
    private readonly Guid AllDriversLeafID = new("b83c27e3-5482-4136-9815-568285521b41");

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool _isInitialized = false;

    private bool _firstLeafSelection;

    private bool _pageActive;

    private object? _lastSelectedNodeData = null;

    private bool _isLoadingAll;


    #endregion

    public CustomFiltersPage()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            if (_isInitialized) return;
            _isInitialized = true;

            var menu = (MenuFlyout)Resources["FilterMenu"];
            SetIcon(menu, "Edit Filter", Icons.Edit);
            SetIcon(menu, "Duplicate Filter", Icons.Copy);
            SetIcon(menu, "Remove Filter", Icons.Delete);

            _ = LoadAllAsync();
            await LoadFiltersAsync();
        };
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _pageActive = true;
        _firstLeafSelection = true;
        App.SubWindows.FilterSaved += OnFilterSaved;
        if (_isInitialized && !_isLoadingAll) StartWatcher();   // cached page coming back
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        App.SubWindows.FilterSaved -= OnFilterSaved;
        StopWatcher();
    }

    // ---- Loading Drivers and Printers ----

    /// <summary>
    /// Drivers first, because the printers load reuses that list instead of asking the server again.
    /// The filter tree is built once, at the end, so it never shows a half-loaded count.
    /// </summary>

    private async Task LoadAllAsync()
    {
        if (_isLoadingAll) return;
        _isLoadingAll = true;
        IsLoading = true;
        CanRefresh = false;
        try
        {
            await LoadDriversAsync();
            await LoadPrintersAsync();
        }
        finally
        {
            IsLoading = false;
            CanRefresh = true;
            _isLoadingAll = false;
            UpdateFilterCountsAfterDataLoaded();
            StartWatcher();
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

    // ---- Filters ----

    private async Task LoadFiltersAsync()
    {
        FiltersList.Clear();

        var printersRoot = await FilterLeafHelpers.BuildEntityRootLeafAsync<PrinterModel>(
            rootName: PrintersRootName,
            rootIcon: Icons.Printer,
            pageType: FilterPages.PrinterPage,
            totalItems: PrinterModelList.Count,
            description: "Shows all printers",
            rootDescription: "A quick overview of your filters and what needs attention.",
            countCalculator: filter => FilterEngine.Count(PrinterModelList, filter), // <--- כאן הקסם קורה!
            specialAllId: AllPrintersLeafID,
            defaultSeedFilters: InitialFilters.PrinterDefaults
        );
        FiltersList.Add(printersRoot);

        var driversRoot = await FilterLeafHelpers.BuildEntityRootLeafAsync<DriverModel>(
            rootName: DriversRootName,
            rootIcon: Icons.Driver,
            pageType: FilterPages.DriverPage,
            totalItems: DriverModelList.Count,
            description: "Shows all drivers",
            rootDescription: "A quick overview of your filters and what needs attention.",
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
                _lastSelectedNodeData = FiltersTree.SelectedItem;
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
        _lastSelectedNodeData = FiltersTree.SelectedItem;

        if (e.AddedItems.FirstOrDefault() is FilterLeaf leaf)
        {
            if (NavigationStateService.LastSelectedFilterID == leaf.ID && !_firstLeafSelection)
            {
                return;
            }
            _firstLeafSelection = false;
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

    private static void SetIcon(MenuFlyout menu, string text, dynamic icon)
    {
        var item = menu.Items.OfType<MenuFlyoutItem>().First(i => i.Text == text);
        item.Icon = new FontIcon
        {
            FontFamily = icon.Font,
            Glyph = icon.Glyph
        };
    }

    // ---- Watching Printers ----

    private void StartWatcher()
    {
        if (!_pageActive || _watcher is not null) return;

        // Debounce: bursts of changes cause one recount, 400 ms after the last one
        _recountTimer = DispatcherQueue.CreateTimer();
        _recountTimer.Interval = TimeSpan.FromMilliseconds(400);
        _recountTimer.IsRepeating = false;
        _recountTimer.Tick += (_, _) => UpdateFilterCountsAfterDataLoaded();

        double refreshSpeed = SettingsService.ReadSetting(Settings<int>.RefreshSpeedSetting);
        if (refreshSpeed == -1) return;
        _watcher = new PrinterWatcher(_serverName, TimeSpan.FromSeconds(refreshSpeed));
        _watcher.Changed += OnPrintersChanged;

        _watcherCts = new CancellationTokenSource();
        _ = _watcher.RunAsync(_watcherCts.Token);
    }

    private void OnPrintersChanged(IReadOnlyList<PrinterPulse> pulses)
    {
        // Called on a background thread, so hop to the UI thread first
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_watcher is null || _isLoadingAll) return;   // a full refresh is replacing the models right now

            var anyUpdated = false;
            foreach (var pulse in pulses)
            {
                var model = PrinterModelList.FirstOrDefault(m =>
                    string.Equals(m.Name, pulse.Name, StringComparison.OrdinalIgnoreCase));
                if (model is null) continue;   // new printer: a manual Refresh picks it up

                model.QueueStatus = PrinterStatusHelper.FormatQueueStatus(pulse.Status); // new list, so the setter fires
                model.JobsInQueue = pulse.Jobs;
                anyUpdated = true;
            }

            if (anyUpdated)
            {
                _recountTimer?.Stop();
                _recountTimer?.Start();
            }
        });
    }

    private void StopWatcher()
    {
        Debug.WriteLine("StopWatcher called");
        _pageActive = false;

        _watcher?.Changed -= OnPrintersChanged;   // releases the page reference
        _watcherCts?.Cancel();
        _watcherCts?.Dispose();
        _watcherCts = null;
        _recountTimer?.Stop();
        _recountTimer = null;
        _watcher = null;
    }

    // ---- Navigation ----

    private void UpdateNavigationStates(FilterLeaf? current)
    {
        if (current is not null)
        {
            CanGoUp = !current.IsRoot;
            RefreshTargetName = current.Name;
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

    private void AddFilter_Printer_Click(object sender, RoutedEventArgs e)
    {
        App.SubWindows.ShowFilterEditor<PrinterModel>();
    }

    private void AddFilter_Driver_Click(object sender, RoutedEventArgs e)
    {
        App.SubWindows.ShowFilterEditor<DriverModel>();
    }

    private void CollapseAllFilter_Click(object sender, RoutedEventArgs e)
    {
        foreach (FilterLeaf leaf in FiltersList)
        {
            leaf.IsExpanded = false;
        }
    }

    private void EditFilter_Click(object sender, RoutedEventArgs e)
    {
        if (_contextLeaf is not { } leaf) return;
        if (leaf.FilterPage == FilterPages.PrinterPage)
            App.SubWindows.ShowFilterEditor<PrinterModel>(leaf.Filter);
        else
            App.SubWindows.ShowFilterEditor<DriverModel>(leaf.Filter);


    }

    private async void DuplicateFilter_Click(object sender, RoutedEventArgs e)
    {
        if (_contextLeaf is not { } leaf) return;

        if (leaf.FilterPage == FilterPages.PrinterPage)
            await DuplicateAsync<PrinterModel>(leaf.Filter);
        else
            await DuplicateAsync<DriverModel>(leaf.Filter);

        await ReloadFiltersAsync();
    }

    private async void OnFilterSaved(object? s, GenericFilterDefinition def)
    {
        try { await ReloadFiltersAsync(); }
        catch (Exception ex) { Logger.Error(ex); }
    }

    private async void DeleteFilter_Click(object sender, RoutedEventArgs e)
    {
        if (_contextLeaf is not { } leaf) return;

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,                       // required in WinUI 3
            Title = "Remove filter?",
            Content = $"“{leaf.Filter.Name}” will be permanently removed.",
            PrimaryButtonText = "Remove",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

        if (leaf.FilterPage == FilterPages.PrinterPage)
            await App.ffs.DeleteAsync<PrinterModel>(leaf.Filter.ID);
        else
            await App.ffs.DeleteAsync<DriverModel>(leaf.Filter.ID);

        await ReloadFiltersAsync();   // replace with whatever refreshes your tree
    }

    private static async Task DuplicateAsync<TEntity>(GenericFilterDefinition source)
    where TEntity : EntityModel
    {
        // JSON round-trip = deep copy, so the original is never touched
        var copy = JsonSerializer.Deserialize<GenericFilterDefinition>(
            JsonSerializer.Serialize(source))!;

        copy.ID = Guid.NewGuid();                  // new identity, otherwise it would overwrite the original
        copy.Name = $"{source.Name} (copy)";

        await App.ffs.SaveAsync<TEntity>(copy);
    }

    private void FiltersTree_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
    {
        sender.SelectedItem = _lastSelectedNodeData;
    }

    private FilterLeaf? _contextLeaf;

    private void TreeViewItem_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
    {
        DependencyObject? node = args.OriginalSource as DependencyObject;
        while (node is not null and not TreeViewItem)
            node = VisualTreeHelper.GetParent(node);

        if (node is not TreeViewItem container) return;

        args.Handled = true;

        if (FiltersTree.ItemFromContainer(container) is not FilterLeaf leaf) return;
        if (!leaf.DisplayFlyoutContextMenu) return;

        _contextLeaf = leaf;

        var flyout = (MenuFlyout)Resources["FilterMenu"];
        if (args.TryGetPosition(container, out var point))
            flyout.ShowAt(container, point);
        else
            flyout.ShowAt(container);   // keyboard (Menu key / Shift+F10)
    }

    private async Task ReloadFiltersAsync(Guid? removedId = null)
    {
        // Rebuilding the tree creates new leaf objects, so remember what was expanded
        var expanded = new HashSet<Guid>();
        CollectExpanded(FiltersList, expanded);

        // If the selected filter was deleted, fall back to the first leaf
        if (removedId is not null && NavigationStateService.LastSelectedFilterID == removedId)
            NavigationStateService.LastSelectedFilterID = null;

        // Without this, SelectionChanged ignores "the same ID" and the sub page keeps showing
        // the old (pre-edit) filter object
        _firstLeafSelection = true;

        await LoadFiltersAsync();
        RestoreExpanded(FiltersList, expanded);
    }

    private static void CollectExpanded(IEnumerable<FilterLeaf> nodes, HashSet<Guid> result)
    {
        foreach (var n in nodes)
        {
            if (n.IsExpanded) result.Add(n.ID);
            CollectExpanded(n.Children, result);
        }
    }

    private static void RestoreExpanded(IEnumerable<FilterLeaf> nodes, HashSet<Guid> expanded)
    {
        foreach (var n in nodes)
        {
            if (expanded.Contains(n.ID)) n.IsExpanded = true;
            RestoreExpanded(n.Children, expanded);
        }
    }
}