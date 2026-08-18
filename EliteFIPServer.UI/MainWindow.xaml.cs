using EliteFIPServer.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System.IO;
using System;
using System.Threading.Tasks;

namespace EliteFIPServer;

public sealed partial class MainWindow : Window
{
    private readonly CoreServer serverCore;
    private readonly MainWindowViewModel viewModel = new();

    public MainWindow(string[] args)
    {
        InitializeComponent();
        RootGrid.DataContext = viewModel;
        SetTitleBarIcon();

        serverCore = new CoreServer(args);
        serverCore.CurrentState.onStateChange += OnCoreStateChanged;
        serverCore.MatricAPI.CurrentState.onStateChange += OnMatricStateChanged;
        serverCore.PanelServer.CurrentState.onStateChange += OnPanelStateChanged;
        serverCore.ConnectedMatricClientsChanged += OnConnectedMatricClientsChanged;
        Closed += MainWindow_Closed;

        LoadSettings();
        UpdateAllStatus();
        RefreshClients();
        AddActivity("Starting server core...");
        serverCore.Start();
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        serverCore.CurrentState.onStateChange -= OnCoreStateChanged;
        serverCore.MatricAPI.CurrentState.onStateChange -= OnMatricStateChanged;
        serverCore.PanelServer.CurrentState.onStateChange -= OnPanelStateChanged;
        serverCore.ConnectedMatricClientsChanged -= OnConnectedMatricClientsChanged;
        serverCore.Stop();
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

    private async void CmdSaveSettings_Click(object sender, RoutedEventArgs args)
    {
        if (!await Confirm("Elite FIP Server Settings", "Do you want to save changes?"))
        {
            return;
        }

        Log.Instance.Info("Saving settings");
        Properties.Settings.Default.EnableLog = viewModel.EnableLog;
        Properties.Settings.Default.DarkMode = viewModel.DarkMode;
        Properties.Settings.Default.AutostartMatricIntegration = viewModel.AutostartMatricIntegration;
        Properties.Settings.Default.MatricApiPort = GetNumberBoxValue(viewModel.MatricApiPort, Properties.Settings.Default.MatricApiPort);
        Properties.Settings.Default.MatricRetryInterval = GetNumberBoxValue(viewModel.MatricRetryInterval, Properties.Settings.Default.MatricRetryInterval);
        Properties.Settings.Default.AutostartPanelServer = viewModel.AutostartPanelServer;
        Properties.Settings.Default.PanelServerPort = GetNumberBoxValue(viewModel.PanelServerPort, Properties.Settings.Default.PanelServerPort);
        Properties.Settings.Default.Save();

        Log.LogEnabled(Properties.Settings.Default.EnableLog);
        ApplyTheme(Properties.Settings.Default.DarkMode);
        AddActivity("Settings saved");
    }

    private async void CmdRevertSettings_Click(object sender, RoutedEventArgs args)
    {
        if (await Confirm("Elite FIP Server Settings", "Do you want to revert to saved settings?"))
        {
            LoadSettings();
            AddActivity("Settings reverted");
        }
    }

    private void SwtDarkMode_Toggled(object sender, RoutedEventArgs args)
    {
        ApplyTheme(viewModel.DarkMode);
    }

    private void CmdRefreshClients_Click(object sender, RoutedEventArgs args)
    {
        viewModel.ClientCountText = "Refreshing clients...";
        AddActivity("Refreshing Matric clients...");
        serverCore.RefreshConnectedMatricClients();
        RefreshClients(serverCore.GetConnectedMatricClients());
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
        UpdateOnUiThread(() => RefreshClients(clients));
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
        viewModel.PanelServerPort = Properties.Settings.Default.PanelServerPort;
        ApplyTheme(Properties.Settings.Default.DarkMode);
    }

    private void RefreshClients()
    {
        RefreshClients(serverCore.GetConnectedMatricClients());
    }

    private void RefreshClients(IReadOnlyList<MatricClientSummary> connectedClients)
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
            AddActivity("Matric clients refreshed");
        }
        catch (Exception ex)
        {
            viewModel.MatricClients.Clear();
            viewModel.ClientCountText = "Unable to refresh clients";
            AddActivity("Unable to refresh Matric clients");
            Log.Instance.Error("Error refreshing Matric clients: {error}", ex.ToString());
        }
    }

    private void ApplyTheme(bool darkMode)
    {
        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = darkMode ? ElementTheme.Dark : ElementTheme.Light;
        }
    }

    private int GetNumberBoxValue(double value, int fallback)
    {
        return double.IsNaN(value) ? fallback : Convert.ToInt32(value);
    }

    private void RefreshStatusSummary()
    {
        viewModel.StatusMessage = $"{GetStateLabel(viewModel.CoreStatus)} Core | {GetStateLabel(viewModel.MatricStatus)} Matric | {GetStateLabel(viewModel.PanelStatus)} Panel";
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