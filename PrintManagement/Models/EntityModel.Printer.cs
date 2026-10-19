using PrintManagement.Pages.CustomFilters.Helpers;
using PrintManagement.Services;
using System.Collections.Generic;

namespace PrintManagement.Models;

public partial class PrinterModel : EntityModel
{
    private string _name = string.Empty;
    private List<PrinterStatusInfo> _queueStatus = [];
    private uint _jobsInQueue = DEFAULT_VALUE;
    private string _serverName = string.Empty;
    private string _portName = string.Empty;
    private string _driverName = string.Empty;
    private string _driverType = string.Empty;
    private string _driverVersion = string.Empty;
    private bool _branchOffice = false;
    private string _comments = string.Empty;
    private bool _isShared = false;
    private string _location = string.Empty;
    private string _provider = string.Empty;
    private string _shareName = "<Not Shared>";
    private string _processor = string.Empty;
    private string _dataType = string.Empty;
    private uint _attributes = DEFAULT_VALUE;
    private string _connectionSummary = string.Empty;
    private string _connectionKind = string.Empty;

    /// <summary>
    /// The name of the printer
    /// </summary>
    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// The status of the printer queue
    /// </summary>
    public List<PrinterStatusInfo> QueueStatus
    {
        get => _queueStatus;
        set { _queueStatus = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// The number of jobs in the printer queue
    /// </summary>
    public uint JobsInQueue
    {
        get => _jobsInQueue;
        set { _jobsInQueue = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// The name of the port to which the printer is connected
    /// </summary>
    public string PortName
    {
        get => _portName;
        set { _portName = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// The name of the server hosting the printer
    /// </summary>
    public string ServerName
    {
        get => _serverName;
        set { _serverName = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// The name of the printer driver
    /// </summary>
    public string DriverName
    {
        get => _driverName;
        set { _driverName = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// The version of the printer driver
    /// </summary>
    public string DriverVersion
    {
        get => _driverVersion;
        set { _driverVersion = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// The type of the printer driver
    /// <list type="number">
    /// <item><b>Type 3 - Kernel Mode</b> The old type, ran only on the Kernel</item>
    /// <item><b>Type 4 - User Mode</b> Newer type, supported since Windows 8 / Windows Server 2012, running on User Mode</item>
    /// </list>
    /// </summary>
    public string DriverType
    {
        get => _driverType;
        set { _driverType = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Indicates whether the printer is a branch office printer.
    /// </summary>
    public bool BranchOffice
    {
        get => _branchOffice;
        set { _branchOffice = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// A brief description of the printer
    /// </summary>
    public string Comments
    {
        get => _comments;
        set { _comments = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Indicates whether the printer is shared on the network.
    /// </summary>
    public bool IsShared
    {
        get => _isShared;
        set { _isShared = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// The physical location of the printer
    /// </summary>
    public string Location
    {
        get => _location;
        set { _location = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// The name of the printer provider
    /// </summary>
    public string Provider
    {
        get => _provider;
        set { _provider = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// The share name of the printer, if it is shared on the network.
    /// </summary>
    public string ShareName
    {
        get => _shareName;
        set { _shareName = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// The name of the print processor used by the printer.
    /// </summary>
    public string PrintProcessor
    {
        get => _processor;
        set { _processor = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// The type of data that the printer can process, such as RAW or EMF.
    /// </summary>
    public string DataType
    {
        get => _dataType;
        set { _dataType = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// A smart summary of the printer connection type (e.g. Local USB, Direct IP, Branch-Office, Server Shared, or Virtual/Fax).
    /// </summary>
    public string ConnectionSummary
    {
        get => _connectionSummary;
        set { _connectionSummary = value; OnPropertyChanged(); }
    }

    #region internal-only properties
    /// <summary>
    /// The kind of connection the printer uses (e.g. Local, Network, USB, Virtual, etc.).<br />
    /// This is a more technical classification than ConnectionSummary, used internaly and not shown.
    /// </summary>
    public string ConnectionKind
    {
        get => _connectionKind;
        set { _connectionKind = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// The attributes of the printer, represented as a bitmask of flags.<br />
    /// Used internally for filtering and not shown to the user. See <see cref="PrinterAttributesMask"/> for the possible flags.
    /// </summary>
    public uint Attributes
    {
        get => _attributes;
        set { _attributes = value; OnPropertyChanged(); }
    }
    #endregion
}