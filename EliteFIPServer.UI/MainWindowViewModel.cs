using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EliteFIPServer;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private string coreStatus;
    private string matricStatus;
    private string panelStatus;
    private string matricButtonText;
    private string panelButtonText;
    private bool matricButtonEnabled;
    private bool panelButtonEnabled;
    private string clientCountText;
    private string statusMessage;
    private bool enableLog;
    private bool darkMode;
    private bool autostartMatricIntegration;
    private double matricApiPort;
    private double matricRetryInterval;
    private bool autostartPanelServer;
    private double panelServerPort;

    public event PropertyChangedEventHandler PropertyChanged;

    public ObservableCollection<MatricClientSummary> MatricClients { get; } = new();
    public ObservableCollection<ActivityLogEntry> ActivityLog { get; } = new();

    public string VersionText { get; } = $"Version {BuildInfo.BuildString}";

    public string CoreStatus
    {
        get => coreStatus;
        set => SetProperty(ref coreStatus, value);
    }

    public string MatricStatus
    {
        get => matricStatus;
        set => SetProperty(ref matricStatus, value);
    }

    public string PanelStatus
    {
        get => panelStatus;
        set => SetProperty(ref panelStatus, value);
    }

    public string MatricButtonText
    {
        get => matricButtonText;
        set => SetProperty(ref matricButtonText, value);
    }

    public string PanelButtonText
    {
        get => panelButtonText;
        set => SetProperty(ref panelButtonText, value);
    }

    public bool MatricButtonEnabled
    {
        get => matricButtonEnabled;
        set => SetProperty(ref matricButtonEnabled, value);
    }

    public bool PanelButtonEnabled
    {
        get => panelButtonEnabled;
        set => SetProperty(ref panelButtonEnabled, value);
    }

    public string ClientCountText
    {
        get => clientCountText;
        set => SetProperty(ref clientCountText, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        set => SetProperty(ref statusMessage, value);
    }

    public bool EnableLog
    {
        get => enableLog;
        set => SetProperty(ref enableLog, value);
    }

    public bool DarkMode
    {
        get => darkMode;
        set => SetProperty(ref darkMode, value);
    }

    public bool AutostartMatricIntegration
    {
        get => autostartMatricIntegration;
        set => SetProperty(ref autostartMatricIntegration, value);
    }

    public double MatricApiPort
    {
        get => matricApiPort;
        set => SetProperty(ref matricApiPort, value);
    }

    public double MatricRetryInterval
    {
        get => matricRetryInterval;
        set => SetProperty(ref matricRetryInterval, value);
    }

    public bool AutostartPanelServer
    {
        get => autostartPanelServer;
        set => SetProperty(ref autostartPanelServer, value);
    }

    public double PanelServerPort
    {
        get => panelServerPort;
        set => SetProperty(ref panelServerPort, value);
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class ActivityLogEntry
{
    public DateTime Timestamp { get; set; }
    public string DisplayTime => Timestamp.ToString("HH:mm:ss");
    public string Message { get; set; }
}