using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PrintManagement.Pages.CustomFilters.Filters;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PrintManagement.Pages.CustomFilters.SubPages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DriversFilter : Page
{
    public ObservableCollection<DriversFilterModel> DriverFiltersList { get; set; } = [];

    public DriversFilter()
    {
        InitializeComponent();
    }



    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is ObservableCollection<DriversFilterModel> receivedDriverFiltersList)
        {
            DriverFiltersList = receivedDriverFiltersList;
        }
    }
}
