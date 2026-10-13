using Microsoft.UI.Xaml.Controls;
using PrintManagement.Pages.CustomFilterPage.Filters;
using PrintManagement.Services;
using System.Collections.ObjectModel;

namespace PrintManagement.Pages.CustomFilterPage.SubPages
{
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
                SpoolerPrinterService.GetPrinters(includeConnections: true)
            );
        }
    }
}