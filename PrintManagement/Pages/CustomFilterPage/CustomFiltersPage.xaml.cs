using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PrintManagement.Pages.CustomFilterPage.Components.TreeViewLeaf;
using PrintManagement.Pages.CustomFilterPage.Helpers;
using PrintManagement.Pages.CustomFilterPage.SubPages;
using PrintManagement.Resources;
using PrintManagement.Services;
using System;
using System.Collections.Generic;
using System.Linq;


namespace PrintManagement.Pages
{
    public sealed partial class CustomFiltersPage : Page
    {
        public List<FilterLeaf> FiltersList = [];
        public FilterLeaf? SelectedFilterLeaf;

        public CustomFiltersPage()
        {
            InitializeComponent();
            LoadFilters();

        }

        private void LoadFilters()
        {
            // --- Printers Root Leaf ---
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
                TotalItems = 2,
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
                TotalItems = 7,
                FilterPage = FilterPages.DriverPage
            });

            FiltersList.Add(driversRoot);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            string targetName = NavigationStateService.LastSelectedFilterName ?? "AllPrinters";

            FilterLeaf? selectedLeaf = FindLeaf(FiltersList, targetName, FiltersTree);

            if (selectedLeaf != null)
            {
                FiltersTree.SelectedItem = selectedLeaf;
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
                    var container = FiltersTree.ContainerFromItem(node) as TreeViewItem;
                    if (container != null)
                    {
                        container.IsExpanded = true;
                    }
                    return foundInDescendants;
                }
            }
            return null;
        }

        private void FiltersTree_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs e)
        {
            if (e.AddedItems.FirstOrDefault() is FilterLeaf leaf)
            {
                Type? pageType = leaf.FilterPage switch
                {
                    FilterPages.PrinterPage => typeof(PrintersFilter),
                    FilterPages.DriverPage => typeof(DriversFilter),
                    _ => null
                };

                if (pageType != null)
                {
                    FilterSubPage.Navigate(pageType, leaf);
                    NavigationStateService.LastSelectedFilterName = leaf.Name;
                }
            }
        }
    }
}
