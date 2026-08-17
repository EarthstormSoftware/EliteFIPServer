using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EliteFIPServer.Infrastructure;
using EliteFIPServer.Infrastructure.Services;
using Matric.Integration;
using System.Windows.Media.Imaging;

namespace EliteFIPServer.ViewModels
{
    /// <summary>
    /// ViewModel for server status display and control
    /// </summary>
    public partial class ServerStatusViewModel : ObservableObject
    {
        private readonly CoreServer _coreServer;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private string coreStatusEmoji = "❓ Unknown";

        [ObservableProperty]
        private string matricStatusEmoji = "❓ Unknown";

        [ObservableProperty]
        private string panelStatusEmoji = "❓ Unknown";

        [ObservableProperty]
        private string overallStatus = "Initializing...";

        [ObservableProperty]
        private BitmapImage coreStatusImage;

        [ObservableProperty]
        private BitmapImage matricStatusImage;

        [ObservableProperty]
        private BitmapImage panelStatusImage;

        [ObservableProperty]
        private string matricButtonText = "Start";

        [ObservableProperty]
        private bool matricButtonEnabled = true;

        [ObservableProperty]
        private string panelButtonText = "Start";

        [ObservableProperty]
        private bool panelButtonEnabled = true;

        [ObservableProperty]
        private bool matricIntegrationActive = false;

        [ObservableProperty]
        private bool panelServerActive = false;

        public ServerStatusViewModel(CoreServer coreServer, IDialogService dialogService)
        {
            _coreServer = coreServer;
            _dialogService = dialogService;

            // Subscribe to state changes
            _coreServer.CurrentState.onStateChange += OnCoreStateChanged;
            _coreServer.PanelServer.CurrentState.onStateChange += OnPanelStateChanged;
            _coreServer.MatricAPI.CurrentState.onStateChange += OnMatricStateChanged;

            // Initialize status display
            RefreshOverallStatus();
        }

        [RelayCommand]
        public void StartMatric()
        {
            try
            {
                MatricButtonEnabled = false;
                MatricButtonText = "Starting...";
                _coreServer.StartMatricIntegration();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("Start Matric Integration", $"Failed to start Matric Integration: {ex.Message}");
                MatricButtonEnabled = true;
                MatricButtonText = "Start";
                Logging.Log.Instance.Error("Failed to start Matric Integration: {error}", ex.Message);
            }
        }

        [RelayCommand]
        public void StopMatric()
        {
            try
            {
                MatricButtonEnabled = false;
                MatricButtonText = "Stopping...";
                _coreServer.StopMatricIntegration();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("Stop Matric Integration", $"Failed to stop Matric Integration: {ex.Message}");
                MatricButtonEnabled = true;
                MatricButtonText = "Stop";
                Logging.Log.Instance.Error("Failed to stop Matric Integration: {error}", ex.Message);
            }
        }

        [RelayCommand]
        public void StartPanel()
        {
            try
            {
                PanelButtonEnabled = false;
                PanelButtonText = "Starting...";
                _coreServer.PanelServer.Start();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("Start Panel Server", $"Failed to start Panel Server: {ex.Message}");
                PanelButtonEnabled = true;
                PanelButtonText = "Start";
                Logging.Log.Instance.Error("Failed to start Panel Server: {error}", ex.Message);
            }
        }

        [RelayCommand]
        public void StopPanel()
        {
            try
            {
                PanelButtonEnabled = false;
                PanelButtonText = "Stopping...";
                _coreServer.PanelServer.Stop();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("Stop Panel Server", $"Failed to stop Panel Server: {ex.Message}");
                PanelButtonEnabled = true;
                PanelButtonText = "Stop";
                Logging.Log.Instance.Error("Failed to stop Panel Server: {error}", ex.Message);
            }
        }

        private void OnCoreStateChanged(object sender, RunState newState)
        {
            UpdateCoreStatus(newState);
            RefreshOverallStatus();
        }

        private void OnPanelStateChanged(object sender, RunState newState)
        {
            UpdatePanelStatus(newState);
            PanelServerActive = newState == RunState.Started;
            RefreshOverallStatus();
        }

        private void OnMatricStateChanged(object sender, RunState newState)
        {
            UpdateMatricStatus(newState);
            MatricIntegrationActive = newState == RunState.Started;
            RefreshOverallStatus();
        }

        private void UpdateCoreStatus(RunState state)
        {
            CoreStatusEmoji = GetStateEmoji(state);
            CoreStatusImage = GetStatusImage(state);
        }

        private void UpdatePanelStatus(RunState state)
        {
            PanelStatusEmoji = GetStateEmoji(state);
            PanelStatusImage = GetStatusImage(state);
            UpdatePanelButtonState(state);
        }

        private void UpdateMatricStatus(RunState state)
        {
            MatricStatusEmoji = GetStateEmoji(state);
            MatricStatusImage = GetStatusImage(state);
            UpdateMatricButtonState(state);
        }

        private void RefreshOverallStatus()
        {
            OverallStatus = $"{CoreStatusEmoji} Core | {MatricStatusEmoji} Matric | {PanelStatusEmoji} Panel";
        }

        private void UpdateMatricButtonState(RunState state)
        {
            switch (state)
            {
                case RunState.Stopped:
                    MatricButtonText = "Start";
                    MatricButtonEnabled = true;
                    break;
                case RunState.Starting:
                    MatricButtonText = "Starting...";
                    MatricButtonEnabled = false;
                    break;
                case RunState.Started:
                    MatricButtonText = "Stop";
                    MatricButtonEnabled = true;
                    break;
                case RunState.Stopping:
                    MatricButtonText = "Stopping...";
                    MatricButtonEnabled = false;
                    break;
            }
        }

        private void UpdatePanelButtonState(RunState state)
        {
            switch (state)
            {
                case RunState.Stopped:
                    PanelButtonText = "Start";
                    PanelButtonEnabled = true;
                    break;
                case RunState.Starting:
                    PanelButtonText = "Starting...";
                    PanelButtonEnabled = false;
                    break;
                case RunState.Started:
                    PanelButtonText = "Stop";
                    PanelButtonEnabled = true;
                    break;
                case RunState.Stopping:
                    PanelButtonText = "Stopping...";
                    PanelButtonEnabled = false;
                    break;
            }
        }

        private string GetStateEmoji(RunState state) => state switch
        {
            RunState.Stopped => "⏹️ Stopped",
            RunState.Starting => "🔄 Starting",
            RunState.Started => "✅ Started",
            RunState.Stopping => "⏸️ Stopping",
            _ => "❓ Unknown"
        };

        private BitmapImage GetStatusImage(RunState state)
        {
            return state switch
            {
                RunState.Stopped => new BitmapImage(new Uri("pack://application:,,,/Images/minus32.png")),
                RunState.Starting or RunState.Stopping => new BitmapImage(new Uri("pack://application:,,,/Images/refresh32.png")),
                RunState.Started => new BitmapImage(new Uri("pack://application:,,,/Images/yes32.png")),
                _ => new BitmapImage(new Uri("pack://application:,,,/Images/minus32.png"))
            };
        }

        public void Cleanup()
        {
            _coreServer.CurrentState.onStateChange -= OnCoreStateChanged;
            _coreServer.PanelServer.CurrentState.onStateChange -= OnPanelStateChanged;
            _coreServer.MatricAPI.CurrentState.onStateChange -= OnMatricStateChanged;
        }

        public void UpdateAllStatus()
        {
            UpdateCoreStatus(_coreServer.CurrentState.State);
            UpdatePanelStatus(_coreServer.PanelServer.CurrentState.State);
            UpdateMatricStatus(_coreServer.MatricAPI.CurrentState.State);
            RefreshOverallStatus();
        }
    }
}
