using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PrintManagement.Pages.CustomFilters;
using PrintManagement.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PrintManagement.Pages.CommonPageWrapper;

public sealed partial class CommonPageWrapper : Page
{
    public CommonPageWrapper()
    {
        InitializeComponent();
        navFrame.Navigate(typeof(CustomFiltersPage));
    }

    private void ViewMode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioMenuFlyoutItem item)
        {
            UpdateSelectedIcon(item);
        }
    }

    private void ViewDropDownButton_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateSelectedIcon(DefaultDetailsViewItem);
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

    private void ToggleMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleMenuFlyoutItem toggleItem)
        {
            ViewDDSettingsService.IsCompactView = toggleItem.IsChecked;
        }
    }
}
