using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PrintManagement.Models.enums;
using PrintManagement.Models.Statics;
using PrintManagement.Services;
using System;
using System.Collections.Specialized;

namespace PrintManagement.Controls.IconGridViewControl;

public sealed partial class IconGridView
{
    private void IconGridView_Loaded_Layout(object sender, RoutedEventArgs e)
    {
        ViewType = ViewDDSettingsService.CurrentViewMode_Filter;
        CompactView = ViewDDSettingsService.IsCompactView;
        ViewDDSettingsService.IconSize_FilterChanged += ViewDDSettingsService_IconSize_FilterChanged;
        ViewDDSettingsService.CompactViewChanged += ViewDDSettingsService_CompactViewChanged;
        UpdateLayoutMode(ViewType, CompactView);
    }

    private void IconGridView_Unloaded_Layout(object sender, RoutedEventArgs e)
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
                double spacing = currentIconSize == StaticValues.LargeIcons ? 8 : 4;

                ContentRepeater.Layout = new CommunityToolkit.WinUI.Controls.WrapLayout
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalSpacing = spacing,
                    VerticalSpacing = spacing
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
                    double targetWidth = isList
                        ? double.NaN   // list mode stretches via StackLayout
                        : Math.Max(120, ViewDDSettingsService.CurrentIconSize_Filter + 40);

                    // inside the foreach:
                    model.TileWidth = targetWidth;

                    model.Padding = newPadding;
                    model.Orientation = targetOrientation;
                    model.IconSize = targetSize;
                    model.Alignment = targetAlignment;
                    model.TextAlignment = targetTextAlignment;
                }
            }
        }
    }
}
