using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PrintManagement.Models.enums;
using PrintManagement.Models.Statics;
using PrintManagement.Pages.CustomFilters;
using PrintManagement.Services;


namespace PrintManagement.Pages.CommonPageWrapper;

public sealed partial class CommonPageWrapper : Page
{
    public CommonPageWrapper()
    {
        InitializeComponent();
        navFrame.Navigate(typeof(CustomFiltersPage));
        Loaded += CommonPageWrapper_Loaded;
    }

    private void CommonPageWrapper_Loaded(object sender, RoutedEventArgs e)
    {
        RadioMenuFlyoutItem selectedItem;
        ViewMode storedViewMode = ViewDDSettingsService.CurrentViewMode_Filter;
        double iconSize = ViewDDSettingsService.CurrentIconSize_Filter;

        if (storedViewMode == ViewMode.Table)
        {
            selectedItem = DetailsView;
        }
        else if (storedViewMode == ViewMode.List)
        {
            selectedItem = ListView;
        }
        else
        {
            if (iconSize == StaticValues.SmallIcons)
            {
                selectedItem = SmallIconsView;
            }
            else
            {
                selectedItem = LargeIconsView;
            }
        }
        selectedItem.IsChecked = true;
        UpdateSelectedIcon(selectedItem);
        CompactViewToggleItem.IsChecked = ViewDDSettingsService.IsCompactView;
    }

    private void UpdateSelectedIcon(RadioMenuFlyoutItem item)
    {
        if (item == null) return;

        if (item.Icon is FontIcon fontIcon)
        {
            SelectedIconPresenter.Content = new FontIcon
            {
                Glyph = fontIcon.Glyph,
                FontFamily = fontIcon.FontFamily,
                FontSize = 20
            };
        }
        else if (item.Icon is SymbolIcon symbolIcon)
        {
            SelectedIconPresenter.Content = new SymbolIcon
            {
                Symbol = symbolIcon.Symbol
            };
        }
    }

    private void ViewMode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioMenuFlyoutItem item)
        {
            UpdateSelectedIcon(item);
            ViewMode selectedView = item.Name switch
            {
                "DetailsView" => ViewMode.Table,
                "ListView" => ViewMode.List,
                _ => ViewMode.Tiles
            };
            if (selectedView == ViewMode.Tiles)
            {
                ViewDDSettingsService.CurrentIconSize_Filter = item.Name switch
                {
                    "SmallIconsView" => StaticValues.SmallIcons,
                    _ => StaticValues.LargeIcons
                };
            }

            ViewDDSettingsService.CurrentViewMode_Filter = selectedView;
        }
    }

    private void ToggleMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleMenuFlyoutItem toggleItem)
        {
            ViewDDSettingsService.IsCompactView = toggleItem.IsChecked;
        }
    }


    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        string pageName = string.Empty;

        // נגשים ל-Content של navFrame (שהוא CustomFiltersPage) ושואלים אותו ישירות
        if (navFrame.Content is CustomFiltersPage customFiltersPage)
        {
            pageName = customFiltersPage.SelectedFilterName;
        }

        WeakReferenceMessenger.Default.Send(new ExportCsvRequestMessage
        {
            PageName = pageName
        });
    }
}
