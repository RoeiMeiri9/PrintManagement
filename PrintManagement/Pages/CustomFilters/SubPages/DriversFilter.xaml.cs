using Microsoft.UI.Xaml.Controls;
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
    public ObservableCollection<DriversFilterModel> DriverFiltersList { get; } = [];

    public DriversFilter()
    {
        InitializeComponent();
        LoadInitialPrinters();
    }

    private void LoadInitialPrinters()
    {

        //PrintersTable.Columns.Add(new TableColumn { Header = "Port", Path = "PortName", Width = 100 });

        DriverFiltersList.Add(new DriversFilterModel
        {
            DriverName = "Microsoft enhanced Point and Print compatibility driver",
            Environment = "Windows x64",
            DriverVersion = "10.0.26100.33438",
            DriverIsolation = "None",
            Provider = "Microsoft",
            ServerName = "WIN-HO9KT6LB37V (local)",
            PrintProcessor = "winprint",
            Packaged = true,
            DriverDate = "21/06/2006",
            InfPath = "C:\\WINDOWS\\System32\\DriverStore\\FileRepository\\prnms003.inf_amd64_99d5a2dd460918a4\\prnms003.inf",
            XPSDriver = true,
            ConfigFilePath = "C:\\WINDOWS\\system32\\spool\\DRIVERS\\x64\\3\\PrintConfig.dll",
            DataFilePath = "C:\\WINDOWS\\system32\\spool\\DRIVERS\\x64\\3\\unishare.gpd",
            DefaultDatatype = "",
            DriverFilePath = "C:\\WINDOWS\\system32\\spool\\DRIVERS\\x64\\3\\mxdwdrv.dll",
            HardwareID = "microsoftmicrosoft_musd",
            HelpFilePath = "",
            LanguageMonitor = "",
            Manufacturer = "Microsoft",
            OEMURL = "",
            Shareable = false,
            Type = "Type 3 - User Mode"
        });

        DriverFiltersList.Add(new DriversFilterModel
        {
            DriverName = "Microsoft enhanced Point and Print compatibility driver",
            Environment = "Windows NT x86",
            DriverVersion = "10.0.26100.33438",
            DriverIsolation = "None",
            Provider = "Microsoft",
            ServerName = "WIN-HO9KT6LB37V (local)",
            PrintProcessor = "winprint",
            Packaged = true,
            DriverDate = "21/06/2006",
            InfPath = "C:\\WINDOWS\\System32\\DriverStore\\FileRepository\\prnms003.inf_x86_dbc4ef29094c9391\\prnms003.inf",
            XPSDriver = true,
            ConfigFilePath = "C:\\WINDOWS\\system32\\spool\\DRIVERS\\W32X86\\3\\PrintConfig.dll",
            DataFilePath = "C:\\WINDOWS\\system32\\spool\\DRIVERS\\W32X86\\3\\unishare.gpd",
            DefaultDatatype = "",
            DriverFilePath = "C:\\WINDOWS\\system32\\spool\\DRIVERS\\W32X86\\3\\mxdwdrv.dll",
            HardwareID = "microsoftmicrosoft_musd",
            HelpFilePath = "",
            LanguageMonitor = "",
            Manufacturer = "Microsoft",
            OEMURL = "",
            Shareable = false,
            Type = "Type 3 - User Mode"
        });

        DriverFiltersList.Add(new DriversFilterModel
        {
            DriverName = "Microsoft IPP Class Driver",
            Environment = "Windows x64",
            DriverVersion = "10.0.26100.33438",
            DriverIsolation = "None",
            Provider = "Microsoft",
            ServerName = "WIN-HO9KT6LB37V (local)",
            PrintProcessor = "winprint",
            Packaged = true,
            DriverDate = "21/06/2006",
            InfPath = "C:\\WINDOWS\\System32\\DriverStore\\FileRepository\\prnms012.inf_amd64_e3e11c18988bb31c\\prnms012.inf",
            XPSDriver = true,
            ConfigFilePath = "C:\\WINDOWS\\System32\\DriverStore\\FileRepository\\prnms003.inf_amd64_99d5a2dd460918a4\\Amd64\\PrintConfig.dll",
            DataFilePath = "C:\\WINDOWS\\System32\\DriverStore\\FileRepository\\prnms012.inf_amd64_e3e11c18988bb31c\\MSIPP.xml",
            DefaultDatatype = "",
            DriverFilePath = "C:\\WINDOWS\\System32\\DriverStore\\FileRepository\\ntprint.inf_amd64_e38ce1c36a1ccbdc\\Amd64\\mxdwdrv.dll",
            HardwareID = "{b71661f4-a4dc-43df-bc16-39d337d15a95}",
            HelpFilePath = "",
            LanguageMonitor = "",
            Manufacturer = "Microsoft",
            OEMURL = "",
            Shareable = true,
            Type = "Type 4 - User Mode"
        });
    }
}
