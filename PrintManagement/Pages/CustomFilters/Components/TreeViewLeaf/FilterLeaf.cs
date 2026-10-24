using Microsoft.UI.Xaml;
using PrintManagement.Models.Filter;
using PrintManagement.Pages.CustomFilters.Helpers;
using PrintManagement.Resources;
using System;
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
    public Guid ID { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Icons LeafIcon { get; set; } = Icons.Filter;
    public Visibility DisplayTotalItems { get; set; } = Visibility.Collapsed;
    private bool _isExpended = false;
    public bool IsExpanded
    {
        get => _isExpended;
        set
        {
            if (_isExpended != value)
            {
                _isExpended = value;
                OnPropertyChanged();
            }
        }
    }
    public FilterPages FilterPage { get; set; } = FilterPages.PrinterPage;
    public ObservableCollection<FilterLeaf> Children { get; set; } = [];
    public bool IsRoot = false;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public GenericFilterDefinition Filter { get; set; } = new();
}
