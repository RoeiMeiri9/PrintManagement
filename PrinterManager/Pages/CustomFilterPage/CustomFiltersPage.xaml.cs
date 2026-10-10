using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PrinterManager.Pages.CustomFilterPage.Components.TreeViewLeaf;
using PrinterManager.Pages.CustomFilterPage.SubPages;
using PrinterManager.Resources;
using System;
using System.Collections.Generic;
using System.Linq;


namespace PrinterManager.Pages
{
    public sealed partial class CustomFiltersPage : Page
    {

        public List<FilterLeaf> FiltersList = [];

        public CustomFiltersPage()
        {
            InitializeComponent();
            LoadFilters();
            FilterSubPage.Navigate(typeof(DriversFilter));
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
            // 1. מחפשים את הפריט הרצוי מתוך הרשימה שלך (לפי שם או מפתח)
            FilterLeaf? printersRoot = FiltersList.FirstOrDefault(f => f.Name == "Printers");
            if (printersRoot != null)
            {
                var container = FiltersTree.ContainerFromItem(printersRoot) as TreeViewItem;
                if (container != null)
                {
                    container.IsExpanded = true;
                }
            }
            FilterLeaf? defaultLeaf = printersRoot?.Children.FirstOrDefault(c => c.FilterPage == FilterPages.PrinterPage);
            if (defaultLeaf != null)
            {
                FiltersTree.SelectedItem = defaultLeaf;
            }
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
                }
            }
        }
    }
}
