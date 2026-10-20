using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using PrintManagement.Models.enums;
using System.Collections.Specialized;

namespace PrintManagement.Controls.IconGridViewControl;

public sealed partial class IconGridView : UserControl
{
    #region Dependency Propreties
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(object),
            typeof(IconGridView),
            new PropertyMetadata(null, (d, e) => (d as IconGridView)?.OnItemsSourceChanged()));

    public object ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty ViewTypeProperty =
        DependencyProperty.Register(
            nameof(ViewType),
            typeof(ViewMode),
            typeof(IconGridView),
            new PropertyMetadata(ViewMode.Tiles, (d, e) => (d as IconGridView)?.OnViewTypeChanged((ViewMode)e.NewValue)));

    public ViewMode ViewType
    {
        get => (ViewMode)GetValue(ViewTypeProperty);
        set => SetValue(ViewTypeProperty, value);
    }

    private INotifyCollectionChanged? _observed;

    #endregion
    public IconGridView()
    {
        InitializeComponent();
        Loaded += IconGridView_Loaded;
    }

    private void OnCollectionChanged(object? s, NotifyCollectionChangedEventArgs e)
    {
        // Only new items need styling, but re-applying to all is cheap and simple
        ApplyToItems(ViewType);
    }

    private void IconGridView_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateLayoutMode(ViewType);
    }

    private void OnItemsSourceChanged()
    {
        if (_observed != null) _observed.CollectionChanged -= OnCollectionChanged;
        _observed = ItemsSource as INotifyCollectionChanged;
        if (_observed != null) _observed.CollectionChanged += OnCollectionChanged;

        UpdateLayoutMode(ViewType);
    }

    private void OnViewTypeChanged(ViewMode newMode)
    {
        UpdateLayoutMode(newMode);
    }

    private void UpdateLayoutMode(ViewMode mode)
    {
        bool isList = (mode == ViewMode.List);

        if (ContentRepeater != null)
        {
            if (isList)
            {
                ContentRepeater.Layout = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 4
                };
            }
            else
            {
                ContentRepeater.Layout = new UniformGridLayout
                {
                    MinItemWidth = 90,
                    MinItemHeight = 80,
                    MinRowSpacing = 8,
                    MinColumnSpacing = 8,
                    ItemsJustification = UniformGridLayoutItemsJustification.Start
                };
            }
        }

        ApplyToItems(mode);
    }

    private void ApplyToItems(ViewMode mode)
    {
        bool isList = (mode == ViewMode.List);

        Orientation targetOrientation = isList ? Orientation.Horizontal : Orientation.Vertical;
        double targetSize = isList ? 18.0 : 36.0;
        HorizontalAlignment targetAlignment = isList ? HorizontalAlignment.Stretch : HorizontalAlignment.Center;
        TextAlignment targetTextAlignment = isList ? TextAlignment.Left : TextAlignment.Center;

        if (ItemsSource is System.Collections.IEnumerable items)
        {
            foreach (var item in items)
            {
                if (item is ItemModel model)
                {
                    model.Orientation = targetOrientation;
                    model.IconSize = targetSize;
                    model.Alignment = targetAlignment;
                    model.TextAlignment = targetTextAlignment;
                }
            }
        }
    }

    private ItemModel? _selectedItem;

    private void SelectItem(ItemModel model)
    {
        if (_selectedItem == model)
        {
            model.IsActive = true;          // re-clicking the border-only item re-activates it
            return;
        }

        if (_selectedItem != null)
        {
            _selectedItem.IsSelected = false;
            _selectedItem.IsActive = false;
        }

        _selectedItem = model;
        model.IsSelected = true;
        model.IsActive = true;
    }

    public void ClearSelection()
    {
        if (_selectedItem == null) return;
        _selectedItem.IsSelected = false;
        _selectedItem.IsActive = false;
        _selectedItem = null;
    }

    public void OnClickedOutside()
    {
        if (_selectedItem == null) return;

        if (_selectedItem.IsActive)
            _selectedItem.IsActive = false;
        else
            ClearSelection();
    }

    private void Item_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Control { Tag: ItemModel model } element)
        {
            SelectItem(model);
            element.Focus(FocusState.Keyboard);
        }
    }

    private void Item_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is Control { Tag: ItemModel model } element
            && element.FocusState == FocusState.Keyboard)
        {
            SelectItem(model);
        }
    }

    private void Root_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var source = e.OriginalSource as DependencyObject;
        while (source != null && source != (DependencyObject)sender)
        {
            if (source is Microsoft.UI.Xaml.Controls.Primitives.ScrollBar) return;   // moved inside
            if (source is Control { IsTabStop: true, IsEnabled: true }) return;
            source = VisualTreeHelper.GetParent(source);
        }

        OnClickedOutside();
        FocusSink.Focus(FocusState.Programmatic);
    }
}