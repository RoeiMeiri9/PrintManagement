using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PrintManagement.SubWindows.AddFilters;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AddFiltersWindow : Window
{
    public AddFiltersWindow()
    {
        InitializeComponent();
        SetBackground();
    }

    private void SetBackground()
    {
        ExtendsContentIntoTitleBar = true;
        if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
        {
            SystemBackdrop = new MicaBackdrop();
        }
        else
        {
            SystemBackdrop = new DesktopAcrylicBackdrop();
            MainGrid.Background = new SolidColorBrush(Color.FromArgb(60, 56, 56, 56));
        }
    }
}
