using Microsoft.UI.Xaml;
using PrintManagement.Pages.CustomFilters.Helpers;
using PrintManagement.Resources;
using System.Collections.ObjectModel;

namespace PrintManagement.Pages.CustomFilters.Components.TreeViewLeaf
{

    public class FilterLeaf
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public Icons LeafIcon { get; set; } = Icons.Filter;
        public Visibility DisplayTotalItems { get; set; } = Visibility.Collapsed;
        public int TotalItems { get; set; } = 0;
        public bool IsExpanded { get; set; } = false;
        public FilterPages FilterPage { get; set; } = FilterPages.PrinterPage;
        public ObservableCollection<FilterLeaf> Children { get; set; } = [];
    }
}
