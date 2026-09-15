using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media;

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
    private string commanderText = "Commander: unavailable";
    private string matricVersionText = "Matric: unknown";
    private string eliteApiVersionText = "EliteAPI: unknown";
    private string panelUrlText = "Panel URL: unavailable";
    private string systemText = "System: unavailable";
    private string shipText = "Ship: unavailable";
    private string buildText = "Build: unavailable";
    private string lastUpdatedText = "Updated: never";
    private bool enableLog;
    private bool darkMode;
    private bool autostartMatricIntegration;
    private double matricApiPort;
    private double matricRetryInterval;
    private bool autostartPanelServer;
    private bool panelServerAllowLanAccess;
    private double panelServerPort;
    private bool minimiseToTray;
    private bool startMinimised;

    public MainWindowViewModel()
    {
        LoadMatricButtonTextConfigs();
        LoadMatricPageSwitchConfigs();
        LoadMatricClientProfiles();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public ObservableCollection<MatricClientSummary> MatricClients { get; } = new();
    public ObservableCollection<MatricButtonTextConfigViewModel> MatricButtonTextConfigs { get; } = new();
    public ObservableCollection<MatricPageSwitchConfigViewModel> MatricPageSwitchConfigs { get; } = new();
    public ObservableCollection<MatricClientProfileViewModel> MatricClientProfiles { get; } = new();
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

    public string CommanderText
    {
        get => commanderText;
        set => SetProperty(ref commanderText, value);
    }

    public string MatricVersionText
    {
        get => matricVersionText;
        set => SetProperty(ref matricVersionText, value);
    }

    public string EliteApiVersionText
    {
        get => eliteApiVersionText;
        set => SetProperty(ref eliteApiVersionText, value);
    }

    public string PanelUrlText
    {
        get => panelUrlText;
        set => SetProperty(ref panelUrlText, value);
    }

    public string SystemText
    {
        get => systemText;
        set => SetProperty(ref systemText, value);
    }

    public string ShipText
    {
        get => shipText;
        set => SetProperty(ref shipText, value);
    }

    public string BuildText
    {
        get => buildText;
        set => SetProperty(ref buildText, value);
    }

    public string LastUpdatedText
    {
        get => lastUpdatedText;
        set => SetProperty(ref lastUpdatedText, value);
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

    public bool MinimiseToTray
    {
        get => minimiseToTray;
        set => SetProperty(ref minimiseToTray, value);
    }

    public bool StartMinimised
    {
        get => startMinimised;
        set => SetProperty(ref startMinimised, value);
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

    public void ResetMatricPageSwitchConfigs()
    {
        foreach (var config in MatricPageSwitchConfigs)
        {
            config.PropertyChanged -= MatricPageSwitchConfig_PropertyChanged;
        }

        MatricPageSwitchConfigs.Clear();
        LoadMatricPageSwitchConfigs(MatricPageSwitchConfigStore.GetDefaults(), true);
    }

    private void LoadMatricPageSwitchConfigs()
    {
        LoadMatricPageSwitchConfigs(MatricPageSwitchConfigStore.Load(), false);
    }

    private void LoadMatricPageSwitchConfigs(IEnumerable<MatricPageSwitchConfig> configs, bool save)
    {
        foreach (var config in configs)
        {
            var viewModel = new MatricPageSwitchConfigViewModel(config);
            viewModel.PropertyChanged += MatricPageSwitchConfig_PropertyChanged;
            MatricPageSwitchConfigs.Add(viewModel);
        }

        if (save)
        {
            SaveMatricPageSwitchConfigs();
        }
    }

    private void MatricPageSwitchConfig_PropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        SaveMatricPageSwitchConfigs();
    }

    private void SaveMatricPageSwitchConfigs()
    {
        MatricPageSwitchConfigStore.Save(MatricPageSwitchConfigs.Select(config => config.ToConfig()));
    }

    public void AddMatricClientProfile()
    {
        var profile = new MatricClientProfileViewModel(new MatricClientProfile
        {
            PageSwitches = MatricPageSwitchConfigStore.GetDefaults().ToList()
        });
        profile.PropertyChanged += MatricClientProfile_PropertyChanged;
        MatricClientProfiles.Insert(0, profile);
        SaveMatricClientProfiles();
        UpdateMatricClientProfileOptions(MatricClients);
    }

    public void RemoveMatricClientProfile(MatricClientProfileViewModel profile)
    {
        if (profile == null)
        {
            return;
        }

        profile.PropertyChanged -= MatricClientProfile_PropertyChanged;
        MatricClientProfiles.Remove(profile);
        SaveMatricClientProfiles();
        UpdateMatricClientProfileOptions(MatricClients);
    }

    public void UpdateMatricClientProfileOptions(IEnumerable<MatricClientSummary> clients)
    {
        var connectedClients = clients?.ToList() ?? new List<MatricClientSummary>();
        foreach (var profile in MatricClientProfiles)
        {
            var assignedClientIds = MatricClientProfiles
                .Where(other => !ReferenceEquals(other, profile))
                .Select(other => other.ClientId)
                .Where(id => !string.IsNullOrWhiteSpace(id));
            profile.UpdateAvailableClients(connectedClients, assignedClientIds);
        }
    }

    private void LoadMatricClientProfiles()
    {
        foreach (var profile in MatricClientProfileStore.Load())
        {
            var viewModel = new MatricClientProfileViewModel(profile);
            viewModel.PropertyChanged += MatricClientProfile_PropertyChanged;
            MatricClientProfiles.Add(viewModel);
        }
        UpdateMatricClientProfileOptions(MatricClients);
    }

    private void MatricClientProfile_PropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        SaveMatricClientProfiles();
    }

    private void SaveMatricClientProfiles()
    {
        MatricClientProfileStore.Save(MatricClientProfiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.ClientId))
            .Select(profile => profile.ToConfig()));
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

public sealed class MatricClientProfileViewModel : INotifyPropertyChanged
{
    private string clientId;
    private string clientName;
    private string deckId;
    private MatricClientSummary selectedClient;

    public ObservableCollection<MatricClientSummary> AvailableClients { get; } = new();

    public MatricClientProfileViewModel(MatricClientProfile profile)
    {
        clientId = profile.ClientId;
        clientName = profile.ClientName;
        deckId = profile.DeckId;

        var existingSwitches = profile.PageSwitches ?? new List<MatricPageSwitchConfig>();
        var mergedSwitches = existingSwitches.Concat(MatricPageSwitchConfigStore.GetDefaults()
            .Where(defaultConfig => existingSwitches.All(config => config.State != defaultConfig.State)));
        foreach (var config in mergedSwitches)
        {
            var viewModel = new MatricPageSwitchConfigViewModel(config);
            viewModel.PropertyChanged += PageSwitchConfig_PropertyChanged;
            PageSwitches.Add(viewModel);
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public ObservableCollection<MatricPageSwitchConfigViewModel> PageSwitches { get; } = new();

    public string ClientId
    {
        get => clientId;
        set
        {
            if (SetProperty(ref clientId, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ClientIdBorderBrush)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ClientIdValidationText)));
            }
        }
    }

    public Brush ClientIdBorderBrush => string.IsNullOrWhiteSpace(ClientId)
        ? new SolidColorBrush(Microsoft.UI.Colors.OrangeRed)
        : new SolidColorBrush(Microsoft.UI.Colors.Transparent);

    public string ClientIdValidationText => string.IsNullOrWhiteSpace(ClientId) ? "Required" : string.Empty;

    public string ClientName
    {
        get => clientName;
        set => SetProperty(ref clientName, value);
    }

    public string DeckId
    {
        get => deckId;
        set => SetProperty(ref deckId, value);
    }

    public MatricClientSummary SelectedClient
    {
        get => selectedClient;
        set
        {
            if (value == null)
            {
                return;
            }

            selectedClient = value;
            ClientId = value.Id;
            ClientName = value.Name;
        }
    }

    public MatricClientProfile ToConfig() => new()
    {
        ClientId = ClientId,
        ClientName = ClientName,
        DeckId = DeckId,
        PageSwitches = PageSwitches.Select(config => config.ToConfig()).ToList()
    };

    public void UpdateAvailableClients(IEnumerable<MatricClientSummary> clients, IEnumerable<string> assignedClientIds)
    {
        var assigned = assignedClientIds.ToHashSet();
        var available = clients
            .Where(client => !string.IsNullOrWhiteSpace(client.Id) && (!assigned.Contains(client.Id) || client.Id == ClientId))
            .ToList();

        if (AvailableClients.Select(client => client.Id).SequenceEqual(available.Select(client => client.Id)))
        {
            return;
        }

        AvailableClients.Clear();
        foreach (var client in available)
        {
            AvailableClients.Add(client);
        }

        var matchingClient = available.FirstOrDefault(client => client.Id == ClientId);
        if (!ReferenceEquals(selectedClient, matchingClient))
        {
            selectedClient = matchingClient;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedClient)));
        }
    }

    private void PageSwitchConfig_PropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PageSwitches)));
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

public sealed class MatricPageSwitchConfigViewModel : INotifyPropertyChanged
{
    private bool enabled;
    private string pageId;

    public MatricPageSwitchConfigViewModel(MatricPageSwitchConfig config)
    {
        State = config.State;
        enabled = config.Enabled;
        pageId = config.PageId;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public string State { get; }

    public string StateDisplayName => State switch
    {
        "InMainShip" => "In Main Ship",
        "InFighter" => "In Fighter",
        "InSRV" => "In SRV",
        "OnFoot" => "On Foot",
        "HardpointsDeployed" => "Hardpoints Deployed",
        "HardpointsRetracted" => "Hardpoints Retracted",
        _ => State
    };

    public bool Enabled
    {
        get => enabled;
        set => SetProperty(ref enabled, value);
    }

    public string PageId
    {
        get => pageId;
        set => SetProperty(ref pageId, value);
    }

    public MatricPageSwitchConfig ToConfig() => new()
    {
        State = State,
        Enabled = Enabled,
        PageId = PageId
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