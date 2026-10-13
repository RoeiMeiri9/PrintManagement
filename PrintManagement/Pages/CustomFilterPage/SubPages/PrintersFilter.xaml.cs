using Microsoft.UI.Xaml.Controls;
using PrintManagement.Pages.CustomFilterPage.Filters;
using PrintManagement.Services;
using System.Collections.ObjectModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PrintManagement.Pages.CustomFilterPage.SubPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PrintersFilter : Page
    {
        public ObservableCollection<PrinterFilterModel> PrinterFiltersList { get; set; } = [];


        public PrintersFilter()
        {
            InitializeComponent();
            LoadInitialPrinters();
        }

        private void LoadInitialPrinters()
        {
            PrinterFiltersList = new ObservableCollection<PrinterFilterModel>(
                SpoolerPrinterService.GetPrinters(true)
            );
        }
    }
}
