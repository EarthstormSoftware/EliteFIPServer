using EliteFIPServer.Logging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System.IO;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;

namespace EliteFIPServer;

public sealed partial class MainWindow : Window
{
    private const int DefaultWindowWidth = 877;
    private const int DefaultWindowHeight = 880;
    private const int MinimumWindowWidth = 720;
    private const int MinimumWindowHeight = 520;

    private readonly CoreServer serverCore;
    private readonly MainWindowViewModel viewModel = new();
    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer clientRefreshTimer;
    private readonly TrayIconService trayIconService = new();

    public MainWindow(string[] args)
    {
        InitializeComponent();
        RestoreWindowBounds();
        Activated += MainWindow_Activated;
        RootGrid.DataContext = viewModel;
        SetTitleBarIcon();

        trayIconService.OpenRequested += TrayIconService_OpenRequested;
        trayIconService.ExitRequested += TrayIconService_ExitRequested;
        AppWindow.Changed += AppWindow_Changed;

        serverCore = new CoreServer(args);
        clientRefreshTimer = DispatcherQueue.CreateTimer();
        clientRefreshTimer.Interval = TimeSpan.FromSeconds(20);
        clientRefreshTimer.Tick += ClientRefreshTimer_Tick;
        serverCore.CurrentState.onStateChange += OnCoreStateChanged;
        serverCore.MatricAPI.CurrentState.onStateChange += OnMatricStateChanged;
        serverCore.PanelServer.CurrentState.onStateChange += OnPanelStateChanged;
        serverCore.ConnectedMatricClientsChanged += OnConnectedMatricClientsChanged;
        serverCore.MatricClientAdded += OnMatricClientAdded;
        serverCore.MatricClientRemoved += OnMatricClientRemoved;
        serverCore.PanelClientConnected += OnPanelClientConnected;
        serverCore.PanelClientDisconnected += OnPanelClientDisconnected;
        Closed += MainWindow_Closed;

        LoadSettings();
        ShowSettingsHome(this, EventArgs.Empty);
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        UpdateRuntimeMetadata();
        UpdateAllStatus();
        RefreshClients();
        AddActivity("Starting server core...");
        serverCore.Start();
        clientRefreshTimer.Start();
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        ShutdownApp();
    }

    private void ShutdownApp()
    {
        SaveWindowBounds();
        serverCore.CurrentState.onStateChange -= OnCoreStateChanged;
        serverCore.MatricAPI.CurrentState.onStateChange -= OnMatricStateChanged;
        serverCore.PanelServer.CurrentState.onStateChange -= OnPanelStateChanged;
        serverCore.ConnectedMatricClientsChanged -= OnConnectedMatricClientsChanged;
        serverCore.MatricClientAdded -= OnMatricClientAdded;
        serverCore.MatricClientRemoved -= OnMatricClientRemoved;
        serverCore.PanelClientConnected -= OnPanelClientConnected;
        serverCore.PanelClientDisconnected -= OnPanelClientDisconnected;
        clientRefreshTimer.Stop();
        trayIconService.Dispose();

        try
        {
            Task.Run(serverCore.Stop).Wait(TimeSpan.FromSeconds(2));
        }
        finally
        {
            Environment.Exit(0);
        }
    }

    private void TrayIconService_OpenRequested(object sender, EventArgs args)
    {
        RestoreFromTray();
    }

    private void TrayIconService_ExitRequested(object sender, EventArgs args)
    {
        ShutdownApp();
    }

    private void RestoreFromTray()
    {
        trayIconService.Hide();
        AppWindow.Show();
        if ((AppWindow.Presenter as OverlappedPresenter)?.State == OverlappedPresenterState.Minimized)
        {
            (AppWindow.Presenter as OverlappedPresenter).Restore();
        }

        Activate();
    }

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (!args.DidPresenterChange || !viewModel.MinimiseToTray)
        {
            return;
        }

        if (sender.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized })
        {
            trayIconService.Show();
            sender.Hide();
        }
    }

    public void StartHiddenToTray()
    {
        trayIconService.Show();
    }

    private void ClientRefreshTimer_Tick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {
        RefreshClients(serverCore.GetConnectedMatricClients(), false);
        UpdateRuntimeMetadata();
        serverCore.RefreshConnectedMatricClients();
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            return;
        }

        Activated -= MainWindow_Activated;
        RestoreWindowPosition();
    }

    private void RestoreWindowBounds()
    {
        var settings = Properties.Settings.Default;
        var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(AppWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;
        var width = Math.Clamp(settings.WindowWidth, MinimumWindowWidth, Math.Max(MinimumWindowWidth, workArea.Width));
        var height = Math.Clamp(settings.WindowHeight, MinimumWindowHeight, Math.Max(MinimumWindowHeight, workArea.Height));

        if (settings.WindowWidth <= 0 || settings.WindowHeight <= 0)
        {
            width = Math.Min(DefaultWindowWidth, workArea.Width);
            height = Math.Min(DefaultWindowHeight, workArea.Height);
        }

        AppWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
    }

    private void RestoreWindowPosition()
    {
        var settings = Properties.Settings.Default;
        if (settings.WindowLeft != -1 && settings.WindowTop != -1)
        {
            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(AppWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
            var workArea = displayArea.WorkArea;
            var size = AppWindow.Size;
            var left = Math.Clamp(settings.WindowLeft, workArea.X, workArea.X + workArea.Width - size.Width);
            var top = Math.Clamp(settings.WindowTop, workArea.Y, workArea.Y + workArea.Height - size.Height);
            AppWindow.Move(new Windows.Graphics.PointInt32(left, top));
        }
    }

    private void SaveWindowBounds()
    {
        var settings = Properties.Settings.Default;
        var size = AppWindow.Size;
        var position = AppWindow.Position;

        settings.WindowWidth = size.Width;
        settings.WindowHeight = size.Height;
        settings.WindowLeft = position.X;
        settings.WindowTop = position.Y;
        settings.Save();
    }

    private void CmdMatric_Click(object sender, RoutedEventArgs args)
    {
        if (serverCore.MatricAPI.CurrentState.State == RunState.Started)
        {
            AddActivity("Stopping Matric integration...");
            serverCore.StopMatricIntegration();
            return;
        }

        AddActivity("Starting Matric integration...");
        serverCore.StartMatricIntegration();
    }

    private void ShowSettingsHome(object sender, EventArgs args)
    {
        var page = new SettingsHomePage { DataContext = viewModel };
        page.MatricSettingsRequested += ShowMatricSettings;
        SettingsFrame.Content = page;
    }

    private void ShowMatricSettings(object sender, EventArgs args)
    {
        var page = new MatricSettingsPage { DataContext = viewModel };
        page.BackRequested += ShowSettingsHome;
        SettingsFrame.Content = page;
    }

    private void CmdPanel_Click(object sender, RoutedEventArgs args)
    {
        if (serverCore.PanelServer.CurrentState.State == RunState.Started)
        {
            AddActivity("Stopping Panel Server...");
            serverCore.PanelServer.Stop();
            return;
        }

        AddActivity("Starting Panel Server...");
        serverCore.PanelServer.Start();
    }

    private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        switch (args.PropertyName)
        {
            case nameof(MainWindowViewModel.EnableLog):
            case nameof(MainWindowViewModel.DarkMode):
            case nameof(MainWindowViewModel.AutostartMatricIntegration):
            case nameof(MainWindowViewModel.MatricApiPort):
            case nameof(MainWindowViewModel.MatricRetryInterval):
            case nameof(MainWindowViewModel.AutostartPanelServer):
            case nameof(MainWindowViewModel.PanelServerAllowLanAccess):
            case nameof(MainWindowViewModel.PanelServerPort):
            case nameof(MainWindowViewModel.MinimiseToTray):
            case nameof(MainWindowViewModel.StartMinimised):
                SaveSettings();
                break;
        }
    }

    private void SaveSettings()
    {
        Log.Instance.Info("Saving settings");
        Properties.Settings.Default.EnableLog = viewModel.EnableLog;
        Properties.Settings.Default.DarkMode = viewModel.DarkMode;
        Properties.Settings.Default.AutostartMatricIntegration = viewModel.AutostartMatricIntegration;
        Properties.Settings.Default.MatricApiPort = GetNumberBoxValue(viewModel.MatricApiPort, Properties.Settings.Default.MatricApiPort);
        Properties.Settings.Default.MatricRetryInterval = GetNumberBoxValue(viewModel.MatricRetryInterval, Properties.Settings.Default.MatricRetryInterval);
        Properties.Settings.Default.AutostartPanelServer = viewModel.AutostartPanelServer;
        Properties.Settings.Default.PanelServerAllowLanAccess = viewModel.PanelServerAllowLanAccess;
        Properties.Settings.Default.PanelServerPort = GetNumberBoxValue(viewModel.PanelServerPort, Properties.Settings.Default.PanelServerPort);
        Properties.Settings.Default.MinimiseToTray = viewModel.MinimiseToTray;
        Properties.Settings.Default.StartMinimised = viewModel.StartMinimised;
        Properties.Settings.Default.Save();

        Log.LogEnabled(Properties.Settings.Default.EnableLog);
        ApplyTheme(Properties.Settings.Default.DarkMode);
    }

    private void CmdRefreshClients_Click(object sender, RoutedEventArgs args)
    {
        viewModel.ClientCountText = "Refreshing clients...";
        AddActivity("Refreshing Matric clients...");
        serverCore.RefreshConnectedMatricClients();
        RefreshClients(serverCore.GetConnectedMatricClients(), false);
    }

    private void CmdClearActivity_Click(object sender, RoutedEventArgs args)
    {
        viewModel.ActivityLog.Clear();
        AddActivity("Activity log cleared");
    }

    private void OnCoreStateChanged(object sender, RunState state)
    {
        UpdateOnUiThread(() => UpdateCoreStatus(state));
    }

    private void OnMatricStateChanged(object sender, RunState state)
    {
        UpdateOnUiThread(() => UpdateMatricStatus(state));
    }

    private void OnPanelStateChanged(object sender, RunState state)
    {
        UpdateOnUiThread(() => UpdatePanelStatus(state));
    }

    private void OnConnectedMatricClientsChanged(object sender, IReadOnlyList<MatricClientSummary> clients)
    {
        UpdateOnUiThread(() => RefreshClients(clients, false));
    }

    private void CopyClientValue_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string value && !string.IsNullOrWhiteSpace(value))
        {
            var package = new DataPackage();
            package.SetText(value);
            Clipboard.SetContent(package);
        }
    }

    private void OnMatricClientAdded(object sender, MatricClientSummary client)
    {
        UpdateOnUiThread(() => AddActivity($"Matric client connected: {FormatClient(client)}"));
    }

    private void OnMatricClientRemoved(object sender, MatricClientSummary client)
    {
        UpdateOnUiThread(() => AddActivity($"Matric client disconnected: {FormatClient(client)}"));
    }

    private void OnPanelClientConnected(object sender, string connectionId)
    {
        UpdateOnUiThread(() => AddActivity($"Panel client subscribed: {ShortConnectionId(connectionId)}"));
    }

    private void OnPanelClientDisconnected(object sender, string connectionId)
    {
        UpdateOnUiThread(() => AddActivity($"Panel client unsubscribed: {ShortConnectionId(connectionId)}"));
    }

    private string FormatClient(MatricClientSummary client)
    {
        return string.IsNullOrWhiteSpace(client.Name) ? client.IP : $"{client.Name} ({client.IP})";
    }

    private string ShortConnectionId(string connectionId)
    {
        return connectionId?.Length > 8 ? connectionId[..8] : connectionId;
    }

    private void UpdateAllStatus()
    {
        UpdateCoreStatus(serverCore.CurrentState.State);
        UpdateMatricStatus(serverCore.MatricAPI.CurrentState.State);
        UpdatePanelStatus(serverCore.PanelServer.CurrentState.State);
    }

    private void UpdateCoreStatus(RunState state)
    {
        viewModel.CoreStatus = state.ToString();
        UpdateStatusVisual(coreStatusIndicator, coreStatusProgress, state);
        UpdateStatusIndicator(coreSummaryIndicator, state);
        RefreshStatusSummary();
        AddActivity($"Core {state}");
    }

    private void UpdateMatricStatus(RunState state)
    {
        viewModel.MatricStatus = state.ToString();
        viewModel.MatricButtonText = state == RunState.Started ? "Stop Matric" : "Start Matric";
        viewModel.MatricButtonEnabled = state is not RunState.Starting and not RunState.Stopping;
        UpdateStatusVisual(matricStatusIndicator, matricStatusProgress, state);
        UpdateStatusIndicator(matricSummaryIndicator, state);
        RefreshStatusSummary();
        AddActivity($"Matric integration {state}");

        if (state == RunState.Started)
        {
            serverCore.RefreshConnectedMatricClients();
        }
    }

    private void UpdatePanelStatus(RunState state)
    {
        viewModel.PanelStatus = state.ToString();
        viewModel.PanelButtonText = state == RunState.Started ? "Stop Panel" : "Start Panel";
        viewModel.PanelButtonEnabled = state is not RunState.Starting and not RunState.Stopping;
        UpdateStatusVisual(panelStatusIndicator, panelStatusProgress, state);
        UpdateStatusIndicator(panelSummaryIndicator, state);
        RefreshStatusSummary();
        AddActivity($"Panel Server {state}");
    }

    private void LoadSettings()
    {
        viewModel.EnableLog = Properties.Settings.Default.EnableLog;
        viewModel.DarkMode = Properties.Settings.Default.DarkMode;
        viewModel.AutostartMatricIntegration = Properties.Settings.Default.AutostartMatricIntegration;
        viewModel.MatricApiPort = Properties.Settings.Default.MatricApiPort;
        viewModel.MatricRetryInterval = Properties.Settings.Default.MatricRetryInterval;
        viewModel.AutostartPanelServer = Properties.Settings.Default.AutostartPanelServer;
        viewModel.PanelServerAllowLanAccess = Properties.Settings.Default.PanelServerAllowLanAccess;
        viewModel.PanelServerPort = Properties.Settings.Default.PanelServerPort;
        viewModel.MinimiseToTray = Properties.Settings.Default.MinimiseToTray;
        viewModel.StartMinimised = Properties.Settings.Default.StartMinimised;
        ApplyTheme(Properties.Settings.Default.DarkMode);
    }

    private void RefreshClients()
    {
        RefreshClients(serverCore.GetConnectedMatricClients(), true);
    }

    private void RefreshClients(IReadOnlyList<MatricClientSummary> connectedClients, bool recordActivity)
    {
        try
        {
            viewModel.MatricClients.Clear();
            if (connectedClients != null)
            {
                foreach (MatricClientSummary client in connectedClients)
                {
                    viewModel.MatricClients.Add(client);
                }
            }

            viewModel.ClientCountText = viewModel.MatricClients.Count == 1 ? "1 client" : $"{viewModel.MatricClients.Count} clients";
            viewModel.UpdateMatricClientProfileOptions(viewModel.MatricClients);
            if (recordActivity)
            {
                AddActivity("Matric clients refreshed");
            }
        }
        catch (Exception ex)
        {
            viewModel.MatricClients.Clear();
            viewModel.ClientCountText = "Unable to refresh clients";
            if (recordActivity)
            {
                AddActivity("Unable to refresh Matric clients");
            }
            Log.Instance.Error("Error refreshing Matric clients: {error}", ex.ToString());
        }
    }

    private void ApplyTheme(bool darkMode)
    {
        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = darkMode ? ElementTheme.Dark : ElementTheme.Light;
        }

        var titleBar = AppWindow.TitleBar;
        if (darkMode)
        {
            titleBar.BackgroundColor = Microsoft.UI.Colors.Black;
            titleBar.ForegroundColor = Microsoft.UI.Colors.White;
            titleBar.InactiveBackgroundColor = Microsoft.UI.Colors.Black;
            titleBar.InactiveForegroundColor = Microsoft.UI.Colors.Gray;
            titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Black;
            titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 42, 42, 42);
            titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.White;
            titleBar.ButtonPressedBackgroundColor = Color.FromArgb(255, 60, 60, 60);
            titleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.White;
        }
        else
        {
            titleBar.BackgroundColor = Microsoft.UI.Colors.White;
            titleBar.ForegroundColor = Microsoft.UI.Colors.Black;
            titleBar.InactiveBackgroundColor = Microsoft.UI.Colors.White;
            titleBar.InactiveForegroundColor = Microsoft.UI.Colors.Gray;
            titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.White;
            titleBar.ButtonForegroundColor = Microsoft.UI.Colors.Black;
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 230, 230, 230);
            titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.Black;
            titleBar.ButtonPressedBackgroundColor = Color.FromArgb(255, 210, 210, 210);
            titleBar.ButtonPressedForegroundColor = Microsoft.UI.Colors.Black;
        }
    }

    private int GetNumberBoxValue(double value, int fallback)
    {
        return double.IsNaN(value) ? fallback : Convert.ToInt32(value);
    }

    private void RefreshStatusSummary()
    {
        viewModel.StatusMessage = $"{GetStateLabel(viewModel.CoreStatus)} Core | {GetStateLabel(viewModel.MatricStatus)} Matric | {GetStateLabel(viewModel.PanelStatus)} Panel";
        UpdateRuntimeMetadata();
    }

    private void UpdateRuntimeMetadata()
    {
        var panelUrl = Properties.Settings.Default.PanelServerPort > 0
            ? $"http://127.0.0.1:{Properties.Settings.Default.PanelServerPort}/"
            : "http://127.0.0.1:4545/";

        viewModel.PanelUrlText = panelUrl;
        viewModel.BuildText = BuildInfo.Version;
        viewModel.LastUpdatedText = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        viewModel.MatricVersionText = GetMatricVersion();
        viewModel.EliteApiVersionText = GetEliteApiVersion();
        viewModel.CommanderText = GetCommanderName();
        viewModel.SystemText = GetCurrentSystem();
        viewModel.ShipText = GetCurrentShip();
    }

    private static string GetMatricVersion()
    {
        try
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "MatricIntegration");
            return assembly != null ? assembly.GetName().Version?.ToString() ?? "unknown" : "unknown";
        }
        catch
        {
            return "unknown";
        }
    }

    private static string GetEliteApiVersion()
    {
        try
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "EliteAPI");
            return assembly != null ? assembly.GetName().Version?.ToString() ?? "unknown" : "unknown";
        }
        catch
        {
            return "unknown";
        }
    }

    private string GetCommanderName()
    {
        return serverCore?.EliteAPIIntegration?.CurrentCommanderName ?? "unavailable";
    }

    private string GetCurrentSystem()
    {
        return serverCore?.EliteAPIIntegration?.CurrentSystemName ?? "unavailable";
    }

    private string GetCurrentShip()
    {
        return serverCore?.EliteAPIIntegration?.CurrentShipName ?? "unavailable";
    }

    private string GetStateLabel(string state)
    {
        return state switch
        {
            nameof(RunState.Stopped) => "Stopped",
            nameof(RunState.Starting) => "Starting",
            nameof(RunState.Started) => "Started",
            nameof(RunState.Stopping) => "Stopping",
            _ => "Unknown"
        };
    }

    private void UpdateStatusVisual(Ellipse indicator, ProgressRing progress, RunState state)
    {
        bool isTransitioning = state is RunState.Starting or RunState.Stopping;
        progress.IsActive = isTransitioning;
        progress.Visibility = isTransitioning ? Visibility.Visible : Visibility.Collapsed;
        indicator.Visibility = isTransitioning ? Visibility.Collapsed : Visibility.Visible;
        indicator.Fill = new SolidColorBrush(state switch
        {
            RunState.Started => Colors.LimeGreen,
            RunState.Stopped => Colors.Gray,
            _ => Colors.Goldenrod
        });
    }

    private void UpdateStatusIndicator(Ellipse indicator, RunState state)
    {
        indicator.Fill = new SolidColorBrush(state switch
        {
            RunState.Started => Colors.LimeGreen,
            RunState.Stopped => Colors.Gray,
            _ => Colors.Goldenrod
        });
    }

    private void AddActivity(string message)
    {
        viewModel.ActivityLog.Add(new ActivityLogEntry { Timestamp = DateTime.Now, Message = message });
        if (viewModel.ActivityLog.Count > 500)
        {
            viewModel.ActivityLog.RemoveAt(0);
        }

        lstActivity?.ScrollIntoView(viewModel.ActivityLog[^1]);
    }

    private void SetTitleBarIcon()
    {
        string iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "EliteFIPServerIcon256.ico");
        if (File.Exists(iconPath))
        {
            AppWindow.SetIcon(iconPath);
        }
    }

    private async Task<bool> Confirm(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot
        };

        ContentDialogResult result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private void UpdateOnUiThread(Action update)
    {
        if (DispatcherQueue.HasThreadAccess)
        {
            update();
            return;
        }

        DispatcherQueue.TryEnqueue(() => update());
    }
}