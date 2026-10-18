using Microsoft.UI.Xaml;
using PrintManagement.Pages.CustomFilters.Helpers;
using PrintManagement.Resources;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PrintManagement.Pages.CustomFilters.Components.TreeViewLeaf;

public partial class FilterLeaf : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private int _totalItems;
    public int TotalItems
    {
        get => _totalItems;
        set
        {
            if (_totalItems != value)
            {
                _totalItems = value;
                OnPropertyChanged();
            }
        }
    }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Icons LeafIcon { get; set; } = Icons.Filter;
    public Visibility DisplayTotalItems { get; set; } = Visibility.Collapsed;
    public bool IsExpanded { get; set; } = false;
    public FilterPages FilterPage { get; set; } = FilterPages.PrinterPage;
    public ObservableCollection<FilterLeaf> Children { get; set; } = [];

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
