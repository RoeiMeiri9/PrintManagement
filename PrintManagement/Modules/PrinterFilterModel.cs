using PrintManagement.Pages.CustomFilterPage.Helpers;
using System.Collections.Generic;

namespace PrintManagement.Pages.CustomFilterPage.Filters
{
    public partial class PrinterFilterModel : FilterModler
    {

        private string _name = string.Empty;
        private List<PrinterStatusInfo> _queueStatus = [];
        private uint _jobsInQueue = DEFAULT_VALUE;
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
        private string _portName = string.Empty;
        private string _processor = string.Empty;
        private string _dataType = string.Empty;
        private uint _attributes = DEFAULT_VALUE;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public List<PrinterStatusInfo> QueueStatus
        {
            get => _queueStatus;
            set { _queueStatus = value; OnPropertyChanged(); }
        }

        public uint JobsInQueue
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
        public string PortName
        {
            get => _portName;
            set { _portName = value; OnPropertyChanged(); }
        }
        public string PrintProcessor
        {
            get => _processor;
            set { _processor = value; OnPropertyChanged(); }
        }
        public string DataType
        {
            get => _dataType;
            set { _dataType = value; OnPropertyChanged(); }
        }
        public uint Attributes
        {
            get => _attributes;
            set { _attributes = value; OnPropertyChanged(); }
        }

    }
}