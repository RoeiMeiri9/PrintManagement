using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using PrintManagement.Pages.CustomFilters.Filters;
using System.Collections.ObjectModel;

namespace PrintManagement.Pages.CustomFilters.SubPages
{
    public sealed partial class PrintersFilter : Page
    {
        public ObservableCollection<PrinterFilterModel> PrinterFiltersList { get; set; } = [];

        public PrintersFilter()
        {
            InitializeComponent();

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // בודקים שהתקבל פרמטר תקין וממירים אותו לטיפוס הרצוי שלך
            if (e.Parameter is ObservableCollection<PrinterFilterModel> receivedPrinterFiltersList)
            {
                PrinterFiltersList = receivedPrinterFiltersList;
            }
        }

    }
}