using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PrintManagement.Models.Entity;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PrintManagement.SubWindows.AdvancedFilterEditor.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Filter_Stage1 : Page
{
    public FilterDraft Draft { get; private set; } = null!;

    public Filter_Stage1() => InitializeComponent();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Draft = (FilterDraft)e.Parameter;
        FilterName.PlaceholderText = Draft.EntityType == typeof(PrinterModel)
            ? "e.g. Shows printers that are offline or in error"
            : "e.g. Shows driver that supports XPS";
        Bindings.Update();
    }
}
