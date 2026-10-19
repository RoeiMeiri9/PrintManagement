using PrintManagement.Pages.CustomFilters.Helpers;
using System;

namespace PrintManagement.Models
{
    public partial class DriverModel : EntityModel
    {
        private string _driverName = string.Empty;
        private string _environment = string.Empty;
        private string _driverVersion = string.Empty;
        private IsolationFlag _driverIsolation = IsolationFlag.Unknown;
        private string _provider = string.Empty;
        private string _serverName = string.Empty;
        private string _printProcessor = string.Empty;
        private bool _packaged = false;
        private DateTime? _driverDate = null;
        private string _infPath = string.Empty;
        private bool _XPSDriver = false;
        private string _configFilePath = string.Empty;
        private string _dataFilePath = string.Empty;
        private string _defaultDatatype = string.Empty;
        private string _driverFilePath = string.Empty;
        private string _hardwareID = string.Empty;
        private string _helpFilePath = string.Empty;
        private string _languageMonitor = string.Empty;
        private string _manufacturer = string.Empty;
        private string _OEMURL = string.Empty;
        private bool _shareable = false;
        private string _type = string.Empty;
        private uint _cVersion = 0;

        /// <summary>
        /// The name of the printer driver.
        /// </summary>
        public string DriverName
        {
            get => _driverName;
            set { _driverName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// The environment for which the driver was written (for example, Windows x86, Windows IA64, and Windows x64).
        /// </summary>
        public string Environment
        {
            get => _environment;
            set { _environment = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// The version of the printer driver.
        /// </summary>
        public string DriverVersion
        {
            get => _driverVersion;
            set { _driverVersion = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// <b>Claude says:</b><br />
        /// UNVERIFIED proxy. Print Management shows None / Shared / Isolated. I derive it from
        /// the SANDBOX_ENABLED / SANDBOX_DISABLED attribute bits; compare against the MMC
        /// on your Server 2025 VM and adjust here if they differ.
        /// </summary>
        public IsolationFlag DriverIsolation
        {
            get => _driverIsolation;
            set { _driverIsolation = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// The provider of the printer driver (for example, "Microsoft Windows 2000").
        /// </summary>
        public string Provider
        {
            get => _provider;
            set { _provider = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// The name of the print server on which the printer driver is installed. <br />
        /// If the printer driver is installed on the local computer, this property contains the name of the local computer.
        /// </summary>
        public string ServerName
        {
            get => _serverName;
            set { _serverName = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// The name of the print processor associated with the printer driver.
        /// Used to process print jobs before they are sent to the printer. <br />
        /// Such processes include splitting content into pages, managing user instructions (number of copies, page range, etc.), as part of the printing pipeline. <br />
        /// </summary>
        public string PrintProcessor
        {
            get => _printProcessor;
            set { _printProcessor = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Also called "Package-Aware". Indicates whether the printer driver is packaged.
        /// </summary>
        public bool Packaged
        {
            get => _packaged;
            set { _packaged = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// The release date of the driver package.
        /// </summary>
        public DateTime? DriverDate
        {
            get => _driverDate;
            set { _driverDate = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Specifies the path to the driver's .inf file in the driver store.
        /// </summary>
        public string InfPath
        {
            get => _infPath;
            set { _infPath = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Indicates whether the printer driver supports the Microsoft XPS format.
        /// </summary>
        public bool XPSDriver
        {
            get => _XPSDriver;
            set { _XPSDriver = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Specifies a file name or a full path and file name for the device driver's configuration dynamic-link library (for example, C:\DRIVERS\Pscrptui.dll).
        /// </summary>
        public string ConfigFilePath
        {
            get => _configFilePath;
            set { _configFilePath = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Specifies a file name or a full path and file name for the file that contains driver data (for example, C:\DRIVERS\Qms810.ppd).
        /// </summary>
        public string DataFilePath
        {
            get => _dataFilePath;
            set { _dataFilePath = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Specifies the default data type of the print job (for example, "EMF").
        /// </summary>
        public string DefaultDatatype
        {
            get => _defaultDatatype;
            set { _defaultDatatype = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Specifies a file name or a full path and file name for the file that contains the device driver (for example, C:\DRIVERS\Pscript.dll).
        /// </summary>
        public string DriverFilePath
        {
            get => _driverFilePath;
            set { _driverFilePath = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Specifies the hardware identifier for the printer driver. <br />
        /// </summary>
        public string HardwareID
        {
            get => _hardwareID;
            set { _hardwareID = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Specifies a file name or a full path and file name for the device driver's help file (for example, C:\DRIVERS\Pscrptui.hlp).
        /// </summary>
        public string HelpFilePath
        {
            get => _helpFilePath;
            set { _helpFilePath = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// The name of the language monitor. <br />
        /// Manages bidirectional communication with the printer when supported; otherwise, NULL.
        /// </summary>
        public string LanguageMonitor
        {
            get => _languageMonitor;
            set { _languageMonitor = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// The name of the manufacturer of the printer driver (for example, "HP", "Canon", or "Microsoft").
        /// </summary>
        public string Manufacturer
        {
            get => _manufacturer;
            set { _manufacturer = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Specifies the URL of the printer driver's manufacturer. <br />
        /// </summary>
        public string OEMURL
        {
            get => _OEMURL;
            set { _OEMURL = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Indicates whether the printer driver can be shared with other users on the network. <br />
        /// </summary>
        public bool Shareable
        {
            get => _shareable;
            set { _shareable = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Based on <i>cVersion</i>: The value that indicates the operating system version for which the driver was written. <br />
        /// cVersion is used to identify the type and architecture of the driver.<br />
        /// <list type="bullet">
        ///  <item>
        ///     <description>A value of 3 for cVersion indicates a legacy v3 driver (Pre-Windows 8, kernel-mode or classic user-mode).</description>
        ///  </item>
        ///  <item>
        ///     <description>A value of 4 for cVersion indicates a modern v4 driver, which runs strictly in User Mode with full Package-Aware support.</description></item>
        /// </list>
        /// </summary>
        public string Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// NOTE: This property is part of the Type property. Not to be displayed in the UI.<br />
        /// It is used for internal checks.
        /// </summary>
        public uint CVersion
        {
            get => _cVersion;
            set { _cVersion = value; OnPropertyChanged(); }
        }
    }
}
