using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PrintManagement.SubWindows.AdvancedFilterEditor.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Filter_Stage2 : Page
{
    private FilterDraft _draft = null!;

    public Filter_Stage2() => InitializeComponent();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _draft = (FilterDraft)e.Parameter;
        RulesList.ItemsSource = _draft.Rules;

        if (_draft.Rules.Count == 0)
            _draft.Rules.Add(new FilterRuleRow(_draft.Properties));
        UpdateEmptyState();
    }

    private void AddRule_Click(object sender, RoutedEventArgs e)
    {
        _draft.Rules.Add(new FilterRuleRow(_draft.Properties));
        UpdateEmptyState();
    }

    private void RemoveRule_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: FilterRuleRow row })
        {
            _draft.Rules.Remove(row);
            UpdateEmptyState();
        }
    }

    private void UpdateEmptyState()
        => EmptyHint.Visibility = _draft.Rules.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
}
