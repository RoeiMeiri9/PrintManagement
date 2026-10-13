using Microsoft.UI.Xaml;
using PrintManagement.Pages.CustomFilterPage.Helpers;
using PrintManagement.Resources;
using System.Collections.ObjectModel;

namespace PrintManagement.Pages.CustomFilterPage.Components.TreeViewLeaf
{

    public class FilterLeaf
    {
        public required string Name { get; set; }
        public required string DisplayName { get; set; }
        public required Icons LeafIcon { get; set; }
        public Visibility DisplayTotalItems { get; set; } = Visibility.Collapsed;
        public int TotalItems { get; set; } = 0;
        public required FilterPages FilterPage { get; set; }
        public ObservableCollection<FilterLeaf> Children { get; set; } = [];
    }
}
