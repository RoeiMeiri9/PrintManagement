using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using PrintManagement.Models.enums;
using PrintManagement.Services;
using System;
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

    public static readonly DependencyProperty CompactViewProperty =
       DependencyProperty.Register(
           nameof(CompactView),
           typeof(bool),
           typeof(IconGridView),
           new PropertyMetadata(false, (d, e) => (d as IconGridView)?.OnCompactViewChanged((bool)e.NewValue)));

    public bool CompactView
    {
        get => (bool)GetValue(CompactViewProperty);
        set => SetValue(CompactViewProperty, value);
    }


    private INotifyCollectionChanged? _observed;

    private static readonly Thickness DefaultPadding = new(10, 8, 10, 8);
    private static readonly Thickness ShrinkedPadding = new(10, 6, 10, 6);

    #endregion
    public IconGridView()
    {
        InitializeComponent();
        Loaded += IconGridView_Loaded;
        Unloaded += IconGridView_Unloaded;
    }


    private void IconGridView_Loaded(object sender, RoutedEventArgs e)
    {
        ViewType = ViewDDSettingsService.CurrentViewMode_Filter;
        CompactView = ViewDDSettingsService.IsCompactView;
        ViewDDSettingsService.IconSize_FilterChanged += ViewDDSettingsService_IconSize_FilterChanged;
        ViewDDSettingsService.CompactViewChanged += ViewDDSettingsService_CompactViewChanged;
        UpdateLayoutMode(ViewType, CompactView);
    }

    private void IconGridView_Unloaded(object sender, RoutedEventArgs e)
    {
        ViewDDSettingsService.IconSize_FilterChanged -= ViewDDSettingsService_IconSize_FilterChanged;
        ViewDDSettingsService.CompactViewChanged -= ViewDDSettingsService_CompactViewChanged;
    }

    private void ViewDDSettingsService_IconSize_FilterChanged(object? sender, double newSize)
    {
        ApplyToItems(ViewType, CompactView);
    }

    private void ViewDDSettingsService_CompactViewChanged(object? sender, bool isCompact)
    {
        UpdateLayoutMode(ViewType, isCompact);
    }

    private void OnCollectionChanged(object? s, NotifyCollectionChangedEventArgs e)
    {
        ApplyToItems(ViewType, CompactView);
    }
    private void OnItemsSourceChanged()
    {
        if (_observed != null) _observed.CollectionChanged -= OnCollectionChanged;
        _observed = ItemsSource as INotifyCollectionChanged;
        if (_observed != null) _observed.CollectionChanged += OnCollectionChanged;

        UpdateLayoutMode(ViewType, CompactView);
    }

    private void OnCompactViewChanged(bool isCompact)
    {
        UpdateLayoutMode(ViewType, isCompact);
    }

    private void OnViewTypeChanged(ViewMode newMode)
    {
        UpdateLayoutMode(newMode, CompactView);
    }

    private void UpdateLayoutMode(ViewMode mode, bool isCompact)
    {
        bool isList = (mode == ViewMode.List);

        if (ContentRepeater != null)
        {
            if (isList)
            {
                ContentRepeater.Layout = new StackLayout
                {
                    Orientation = Orientation.Vertical,
                    Spacing = isCompact ? 0.0 : 2.0,
                };
            }
            else
            {
                double currentIconSize = ViewDDSettingsService.CurrentIconSize_Filter;

                double itemWidth = Math.Max(120, currentIconSize + 40);

                double itemHeight = currentIconSize * 1.3 + 130;

                ContentRepeater.Layout = new UniformGridLayout
                {
                    MinItemWidth = itemWidth,
                    MinItemHeight = itemHeight,
                    MinRowSpacing = 8,
                    MinColumnSpacing = 8,
                    ItemsJustification = UniformGridLayoutItemsJustification.Start,
                    ItemsStretch = UniformGridLayoutItemsStretch.None
                };
            }
        }

        ApplyToItems(mode, isCompact);
    }

    private void ApplyToItems(ViewMode mode, bool isCompact)
    {
        bool isList = (mode == ViewMode.List);

        Orientation targetOrientation = isList ? Orientation.Horizontal : Orientation.Vertical;

        double targetSize = isList ? 18.0 : ViewDDSettingsService.CurrentIconSize_Filter;


        Thickness newPadding = isList ? isCompact ? ShrinkedPadding : DefaultPadding : DefaultPadding;

        HorizontalAlignment targetAlignment = isList ? HorizontalAlignment.Stretch : HorizontalAlignment.Center;
        TextAlignment targetTextAlignment = isList ? TextAlignment.Left : TextAlignment.Center;

        if (ItemsSource is System.Collections.IEnumerable items)
        {
            foreach (var item in items)
            {
                if (item is ItemModel model)
                {
                    model.Padding = newPadding;
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