namespace PrintManagement.Pages.CustomFilterPage.Filters
{
    public partial class PrinterFilterModel : FilterModler
    {

        private string _printerName = string.Empty;
        private string _queueStatus = string.Empty;
        private int _jobsInQueue = DEFAULT_VALUE;
        private string _serverName = string.Empty;
        private string _driverName = string.Empty;
        private string _driverType = string.Empty;
        private string _driverVersion = string.Empty;
        private bool _branchOffice = false;
        private string _comment = string.Empty;
        private bool _isShared = false;
        private string _location = string.Empty;
        private string _provider = string.Empty;
        private string _shareName = "<Not Shared>";

        public string PrinterName
        {
            get => _printerName;
            set { _printerName = value; OnPropertyChanged(); }
        }

        public string QueueStatus
        {
            get => _queueStatus;
            set { _queueStatus = value; OnPropertyChanged(); }
        }

        public int JobsInQueue
        {
            get => _jobsInQueue;
            set { _jobsInQueue = value; OnPropertyChanged(); }
        }

        public string ServerName
        {
            get => _serverName;
            set { _serverName = value; OnPropertyChanged(); }
        }

        public string DriverName
        {
            get => _driverName;
            set { _driverName = value; OnPropertyChanged(); }
        }
        public string DriverVersion
        {
            get => _driverVersion;
            set { _driverVersion = value; OnPropertyChanged(); }
        }
        public string DriverType
        {
            get => _driverType;
            set { _driverType = value; OnPropertyChanged(); }
        }

        public bool BranchOffice
        {
            get => _branchOffice;
            set { _branchOffice = value; OnPropertyChanged(); }
        }

        public string Comments
        {
            get => _comment;
            set { _comment = value; OnPropertyChanged(); }
        }

        public bool IsShared
        {
            get => _isShared;
            set { _isShared = value; OnPropertyChanged(); }
        }

        public string Location
        {
            get => _location;
            set { _location = value; OnPropertyChanged(); }
        }

        public string Provider
        {
            get => _provider;
            set { _provider = value; OnPropertyChanged(); }
        }

        public string ShareName
        {
            get => _shareName;
            set { _shareName = value; OnPropertyChanged(); }
        }
    }
}