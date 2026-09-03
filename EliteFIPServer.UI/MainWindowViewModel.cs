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
    private bool panelServerAllowLanAccess;
    private double panelServerPort;

    public MainWindowViewModel()
    {
        LoadMatricButtonTextConfigs();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public ObservableCollection<MatricClientSummary> MatricClients { get; } = new();
    public ObservableCollection<MatricButtonTextConfigViewModel> MatricButtonTextConfigs { get; } = new();
    public ObservableCollection<ActivityLogEntry> ActivityLog { get; } = new();

    public string VersionText { get; } = $"Version {BuildInfo.Version}";

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

    public bool PanelServerAllowLanAccess
    {
        get => panelServerAllowLanAccess;
        set => SetProperty(ref panelServerAllowLanAccess, value);
    }

    public double PanelServerPort
    {
        get => panelServerPort;
        set => SetProperty(ref panelServerPort, value);
    }

    public void ResetMatricButtonTextConfigs()
    {
        foreach (var config in MatricButtonTextConfigs)
        {
            config.PropertyChanged -= MatricButtonTextConfig_PropertyChanged;
        }

        MatricButtonTextConfigs.Clear();
        LoadMatricButtonTextConfigs(MatricButtonTextConfigStore.GetDefaults(), true);
    }

    private void LoadMatricButtonTextConfigs(bool save = false)
    {
        LoadMatricButtonTextConfigs(MatricButtonTextConfigStore.Load(), save);
    }

    private void LoadMatricButtonTextConfigs(IEnumerable<MatricButtonTextConfig> configs, bool save)
    {
        foreach (var config in configs)
        {
            var viewModel = new MatricButtonTextConfigViewModel(config);
            viewModel.PropertyChanged += MatricButtonTextConfig_PropertyChanged;
            MatricButtonTextConfigs.Add(viewModel);
        }

        if (save)
        {
            SaveMatricButtonTextConfigs();
        }
    }

    private void MatricButtonTextConfig_PropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        SaveMatricButtonTextConfigs();
    }

    private void SaveMatricButtonTextConfigs()
    {
        MatricButtonTextConfigStore.Save(MatricButtonTextConfigs.Select(config => config.ToConfig()));
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
    public string DisplayTime => Timestamp.ToString("HH:mm:ss.fff");
    public string Message { get; set; }
}

public sealed class MatricButtonTextConfigViewModel : INotifyPropertyChanged
{
    private string offText;
    private string onText;
    private bool updateButtonText;

    public MatricButtonTextConfigViewModel(MatricButtonTextConfig config)
    {
        ButtonName = config.ButtonName;
        offText = config.OffText;
        onText = config.OnText;
        updateButtonText = config.UpdateButtonText;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public string ButtonName { get; }

    public string OffText
    {
        get => offText;
        set => SetProperty(ref offText, value);
    }

    public string OnText
    {
        get => onText;
        set => SetProperty(ref onText, value);
    }

    public bool UpdateButtonText
    {
        get => updateButtonText;
        set => SetProperty(ref updateButtonText, value);
    }

    public MatricButtonTextConfig ToConfig() => new()
    {
        ButtonName = ButtonName,
        OffText = OffText,
        OnText = OnText,
        UpdateButtonText = UpdateButtonText
    };

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