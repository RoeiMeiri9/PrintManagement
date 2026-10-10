namespace PrinterManager.Pages.CustomFilterPage.Filters
{
    public partial class DriversFilterModel : FilterModler
    {
        private string _driverName = string.Empty;
        private string _environment = string.Empty;
        private string _driverVersion = string.Empty;
        private string _driverIsolation = string.Empty;
        private string _provider = string.Empty;
        private string _serverName = string.Empty;
        private string _printProcessor = string.Empty;
        private bool _packaged = false;
        private string _driverDate = string.Empty;
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


        public string DriverName
        {
            get => _driverName;
            set { _driverName = value; OnPropertyChanged(); }
        }
        public string Environment
        {
            get => _environment;
            set { _environment = value; OnPropertyChanged(); }
        }
        public string DriverVersion
        {
            get => _driverVersion;
            set { _driverVersion = value; OnPropertyChanged(); }
        }
        public string DriverIsolation
        {
            get => _driverIsolation;
            set { _driverIsolation = value; OnPropertyChanged(); }
        }
        public string Provider
        {
            get => _provider;
            set { _provider = value; OnPropertyChanged(); }
        }
        public string ServerName
        {
            get => _serverName;
            set { _serverName = value; OnPropertyChanged(); }
        }
        public string PrintProcessor
        {
            get => _printProcessor;
            set { _printProcessor = value; OnPropertyChanged(); }
        }
        public bool Packaged
        {
            get => _packaged;
            set { _packaged = value; OnPropertyChanged(); }
        }
        public string DriverDate
        {
            get => _driverDate;
            set { _driverDate = value; OnPropertyChanged(); }
        }
        public string InfPath
        {
            get => _infPath;
            set { _infPath = value; OnPropertyChanged(); }
        }
        public bool XPSDriver
        {
            get => _XPSDriver;
            set { _XPSDriver = value; OnPropertyChanged(); }
        }
        public string ConfigFilePath
        {
            get => _configFilePath;
            set { _configFilePath = value; OnPropertyChanged(); }
        }
        public string DataFilePath
        {
            get => _dataFilePath;
            set { _dataFilePath = value; OnPropertyChanged(); }
        }
        public string DefaultDatatype
        {
            get => _defaultDatatype;
            set { _defaultDatatype = value; OnPropertyChanged(); }
        }
        public string DriverFilePath
        {
            get => _driverFilePath;
            set { _driverFilePath = value; OnPropertyChanged(); }
        }
        public string HardwareID
        {
            get => _hardwareID;
            set { _hardwareID = value; OnPropertyChanged(); }
        }
        public string HelpFilePath
        {
            get => _helpFilePath;
            set { _helpFilePath = value; OnPropertyChanged(); }
        }
        public string LanguageMonitor
        {
            get => _languageMonitor;
            set { _languageMonitor = value; OnPropertyChanged(); }
        }
        public string Manufacturer
        {
            get => _manufacturer;
            set { _manufacturer = value; OnPropertyChanged(); }
        }
        public string OEMURL
        {
            get => _OEMURL;
            set { _OEMURL = value; OnPropertyChanged(); }
        }
        public bool Shareable
        {
            get => _shareable;
            set { _shareable = value; OnPropertyChanged(); }
        }
        public string Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); }
        }
    }
}
