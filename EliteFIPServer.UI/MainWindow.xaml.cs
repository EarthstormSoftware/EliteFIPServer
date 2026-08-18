using Microsoft.UI.Xaml;
using System;

namespace EliteFIPServer;

public sealed partial class MainWindow : Window
{
    private readonly CoreServer serverCore;

    public MainWindow(string[] args)
    {
        InitializeComponent();

        serverCore = new CoreServer(args);
        serverCore.CurrentState.onStateChange += OnCoreStateChanged;
        serverCore.MatricAPI.CurrentState.onStateChange += OnMatricStateChanged;
        serverCore.PanelServer.CurrentState.onStateChange += OnPanelStateChanged;
        Closed += MainWindow_Closed;

        UpdateAllStatus();
        serverCore.Start();
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        serverCore.CurrentState.onStateChange -= OnCoreStateChanged;
        serverCore.MatricAPI.CurrentState.onStateChange -= OnMatricStateChanged;
        serverCore.PanelServer.CurrentState.onStateChange -= OnPanelStateChanged;
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
    }

    private void UpdatePanelStatus(RunState state)
    {
        txtPanelStatus.Text = state.ToString();
        cmdPanel.Content = state == RunState.Started ? "Stop Panel" : "Start Panel";
        cmdPanel.IsEnabled = state is not RunState.Starting and not RunState.Stopping;
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