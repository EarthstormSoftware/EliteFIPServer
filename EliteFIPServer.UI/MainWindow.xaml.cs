using EliteFIPServer.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace EliteFIPServer;

public sealed partial class MainWindow : Window
{
    private readonly CoreServer serverCore;
    private readonly ObservableCollection<MatricClientSummary> matricClients = new();

    public MainWindow(string[] args)
    {
        InitializeComponent();
        lstMatricClients.ItemsSource = matricClients;

        serverCore = new CoreServer(args);
        serverCore.CurrentState.onStateChange += OnCoreStateChanged;
        serverCore.MatricAPI.CurrentState.onStateChange += OnMatricStateChanged;
        serverCore.PanelServer.CurrentState.onStateChange += OnPanelStateChanged;
        serverCore.ConnectedMatricClientsChanged += OnConnectedMatricClientsChanged;
        Closed += MainWindow_Closed;

        LoadSettings();
        UpdateAllStatus();
        RefreshClients();
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
            serverCore.StopMatricIntegration();
            return;
        }

        serverCore.StartMatricIntegration();
    }

    private void CmdPanel_Click(object sender, RoutedEventArgs args)
    {
        if (serverCore.PanelServer.CurrentState.State == RunState.Started)
        {
            serverCore.PanelServer.Stop();
            return;
        }

        serverCore.PanelServer.Start();
    }

    private async void CmdSaveSettings_Click(object sender, RoutedEventArgs args)
    {
        if (!await Confirm("Elite FIP Server Settings", "Do you want to save changes?"))
        {
            return;
        }

        Log.Instance.Info("Saving settings");
        Properties.Settings.Default.EnableLog = chkEnableLog.IsChecked == true;
        Properties.Settings.Default.DarkMode = chkDarkMode.IsChecked == true;
        Properties.Settings.Default.AutostartMatricIntegration = chkAutostartMatric.IsChecked == true;
        Properties.Settings.Default.MatricApiPort = GetNumberBoxValue(numMatricPort, Properties.Settings.Default.MatricApiPort);
        Properties.Settings.Default.MatricRetryInterval = GetNumberBoxValue(numMatricRetryInterval, Properties.Settings.Default.MatricRetryInterval);
        Properties.Settings.Default.AutostartPanelServer = chkAutostartPanel.IsChecked == true;
        Properties.Settings.Default.PanelServerPort = GetNumberBoxValue(numPanelServerPort, Properties.Settings.Default.PanelServerPort);
        Properties.Settings.Default.Save();

        Log.LogEnabled(Properties.Settings.Default.EnableLog);
        ApplyTheme(Properties.Settings.Default.DarkMode);
    }

    private async void CmdRevertSettings_Click(object sender, RoutedEventArgs args)
    {
        if (await Confirm("Elite FIP Server Settings", "Do you want to revert to saved settings?"))
        {
            LoadSettings();
        }
    }

    private void ChkDarkMode_Changed(object sender, RoutedEventArgs args)
    {
        ApplyTheme(chkDarkMode.IsChecked == true);
    }

    private void CmdRefreshClients_Click(object sender, RoutedEventArgs args)
    {
        txtClientCount.Text = "Refreshing clients...";
        serverCore.RefreshConnectedMatricClients();
        RefreshClients(serverCore.GetConnectedMatricClients());
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
        txtCoreStatus.Text = state.ToString();
    }

    private void UpdateMatricStatus(RunState state)
    {
        txtMatricStatus.Text = state.ToString();
        cmdMatric.Content = state == RunState.Started ? "Stop Matric" : "Start Matric";
        cmdMatric.IsEnabled = state is not RunState.Starting and not RunState.Stopping;

        if (state == RunState.Started)
        {
            serverCore.RefreshConnectedMatricClients();
        }
    }

    private void UpdatePanelStatus(RunState state)
    {
        txtPanelStatus.Text = state.ToString();
        cmdPanel.Content = state == RunState.Started ? "Stop Panel" : "Start Panel";
        cmdPanel.IsEnabled = state is not RunState.Starting and not RunState.Stopping;
    }

    private void LoadSettings()
    {
        chkEnableLog.IsChecked = Properties.Settings.Default.EnableLog;
        chkDarkMode.IsChecked = Properties.Settings.Default.DarkMode;
        chkAutostartMatric.IsChecked = Properties.Settings.Default.AutostartMatricIntegration;
        numMatricPort.Value = Properties.Settings.Default.MatricApiPort;
        numMatricRetryInterval.Value = Properties.Settings.Default.MatricRetryInterval;
        chkAutostartPanel.IsChecked = Properties.Settings.Default.AutostartPanelServer;
        numPanelServerPort.Value = Properties.Settings.Default.PanelServerPort;
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
            matricClients.Clear();
            if (connectedClients != null)
            {
                foreach (MatricClientSummary client in connectedClients)
                {
                    matricClients.Add(client);
                }
            }

            txtClientCount.Text = matricClients.Count == 1 ? "1 client" : $"{matricClients.Count} clients";
        }
        catch (Exception ex)
        {
            matricClients.Clear();
            txtClientCount.Text = "Unable to refresh clients";
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

    private int GetNumberBoxValue(NumberBox numberBox, int fallback)
    {
        return double.IsNaN(numberBox.Value) ? fallback : Convert.ToInt32(numberBox.Value);
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