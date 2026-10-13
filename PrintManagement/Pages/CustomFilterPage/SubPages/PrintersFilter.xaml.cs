using Microsoft.UI.Xaml.Controls;
using PrintManagement.Pages.CustomFilterPage.Filters;
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
        public ObservableCollection<PrinterFilterModel> PrinterFiltersList { get; } = [];


        public PrintersFilter()
        {
            InitializeComponent();
            LoadInitialPrinters();
        }

        private void LoadInitialPrinters()
        {

            //PrintersTable.Columns.Add(new TableColumn { Header = "Port", Path = "PortName", Width = 100 });

            PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Microsoft Print to PDF",
                QueueStatus = "Ready",
                JobsInQueue = 0,
                ServerName = "WIN-HO9KT6LB37V (local)",
                DriverName = "Microsoft Print To PDF",
                DriverVersion = "10.0.26100.4484",
                DriverType = "Type 4 - User Mode",
                BranchOffice = false,
                IsShared = false,
                Provider = "Microsoft",

            });

            PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Office-HP-LaserJet",
                ServerName = "192.168.1.100",
                QueueStatus = "Pending",
                DriverName = "HP Universal Printing PCL 6",
            });

            PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            });

            PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            }); PrinterFiltersList.Add(new PrinterFilterModel
            {
                PrinterName = "Warehouse-Zebra-Label",
                ServerName = "192.168.1.105",
                QueueStatus = "Offline",
                DriverName = "Zebra ZPL Driver"
            });
        }
    }
}
