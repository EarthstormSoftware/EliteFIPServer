
using EliteFIPServer.Logging;
using EliteFIPServer.Infrastructure;
using EliteFIPServer.Infrastructure.Services;
using Matric.Integration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace EliteFIPServer {
    /// <summary>
    /// Interaction logic for ServerConsole.xaml
    /// </summary>
    public partial class ServerConsole : Window {

        private CoreServer ServerCore;
        private ThemeManager ThemeManager;
        private IDialogService DialogService;

        private delegate void ImageSafeCallDelegate(Image target, bool newstate);
        private delegate void ButtonSafeCallDelegate(Button target, bool newstate);

        private bool MatricIntegrationActive = false;
        private bool PanelServerActive = false;
        private List<ClientInfo> MatricClientList = new List<ClientInfo>();

        public ServerConsole() {
            InitializeComponent();
            
            // Resolve services from DI container
            ThemeManager = EliteFIPServerApplication.GetService<ThemeManager>();
            DialogService = EliteFIPServerApplication.GetService<IDialogService>();

            // Apply saved theme preference
            if (Properties.Settings.Default.DarkMode && !ThemeManager.IsDarkMode)
            {
                ThemeManager.ToggleTheme();
            }
            else if (!Properties.Settings.Default.DarkMode && ThemeManager.IsDarkMode)
            {
                ThemeManager.ToggleTheme();
            }

            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            txtVersion.Text = version.ToString();
            Log.LogEnabled(Properties.Settings.Default.EnableLog);
            refreshSettingsTab();
            
            ServerCore = new CoreServer(this);
            ServerCore.CurrentState.onStateChange += HandleCoreServerStateChange;
            ServerCore.PanelServer.CurrentState.onStateChange += HandlePanelServerStateChange;
            ServerCore.MatricAPI.CurrentState.onStateChange += HandleMatricIntegrationStateChange;

            dgMatricClients.ItemsSource = MatricClientList;
            ServerCore.Start();
            
            // Initialize status display
            Dispatcher.Invoke(() => UpdateOverallStatus());
            
            this.Closing += ServerConsole_Closing;

        }

        private void ServerConsole_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            ServerCore.Stop();
        }

        private void refreshSettingsTab() {

            chkEnableLog.IsChecked = Properties.Settings.Default.EnableLog;
            chkDarkMode.IsChecked = Properties.Settings.Default.DarkMode;
            chkAutostartMatricIntegration.IsChecked = Properties.Settings.Default.AutostartMatricIntegration;
            txtMatricPort.Value = Properties.Settings.Default.MatricApiPort;
            txtMatricRetryInterval.Value = Properties.Settings.Default.MatricRetryInterval;
            chkAutostartPanelServer.IsChecked= Properties.Settings.Default.AutostartPanelServer;
            txtPanelServerPort.Value = Properties.Settings.Default.PanelServerPort;
        }

        private void saveSettings() {
            MessageBoxResult result = DialogService.ShowConfirmation("Elite FIP Server Settings", "Do you want to save changes?");
            
            if (result == MessageBoxResult.Yes) {
                Log.Instance.Info("Saving settings");
                Properties.Settings.Default.EnableLog = (bool)chkEnableLog.IsChecked;
                Properties.Settings.Default.DarkMode = (bool)chkDarkMode.IsChecked;
                Properties.Settings.Default.AutostartMatricIntegration = (bool)chkAutostartMatricIntegration.IsChecked;
                Properties.Settings.Default.MatricApiPort = (int)txtMatricPort.Value;
                Properties.Settings.Default.MatricRetryInterval = (int)txtMatricRetryInterval.Value;
                Properties.Settings.Default.AutostartPanelServer = (bool)chkAutostartPanelServer.IsChecked;
                Properties.Settings.Default.PanelServerPort = (int)txtPanelServerPort.Value;
                Properties.Settings.Default.Save();
                Log.LogEnabled(Properties.Settings.Default.EnableLog);
            }
            
        }

        void MainTabMenu_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (tabClients.IsSelected) {
                Log.Instance.Info("Client tab selected");
                MatricClientList = ServerCore.GetMatricApi().GetConnectedClients();
                dgMatricClients.ItemsSource = MatricClientList;
            }
        }

        private void ChkDarkMode_OnChecked(object sender, RoutedEventArgs e)
        {
            if (!ThemeManager.IsDarkMode)
            {
                ThemeManager.ToggleTheme();
            }
        }

        private void ChkDarkMode_OnUnchecked(object sender, RoutedEventArgs e)
        {
            if (ThemeManager.IsDarkMode)
            {
                ThemeManager.ToggleTheme();
            }
        }

        private void CmdMatric_onClick(object sender, RoutedEventArgs e) {            
            cmdMatric.IsEnabled = false;
            if (MatricIntegrationActive) {
                cmdMatric.Content = "Stopping...";
                ServerCore.StopMatricIntegration();
            } else {
                cmdMatric.Content = "Starting...";
                ServerCore.StartMatricIntegration();
            }
                
        }
        private void CmdPanelServer_onClick(object sender, RoutedEventArgs e) {            
            if (PanelServerActive) {                
                ServerCore.PanelServer.Stop();
            } else {                
                ServerCore.PanelServer.Start();
            }
        }

        private void CmdRevertSettings_onClick(object sender, RoutedEventArgs e) {
            MessageBoxResult result = DialogService.ShowConfirmation("Elite FIP Server Settings", "Do you want to revert to saved settings?");
            
            if (result == MessageBoxResult.Yes) {
                refreshSettingsTab();
            }

        }
        private void CmdsaveSettings_onClick(object sender, RoutedEventArgs e) {
            saveSettings();
        }

        private void HandleCoreServerStateChange(object sender, RunState newState) {
            Dispatcher.Invoke(new Action(() => {
                setStatusImage(imgCoreServerStatus, newState);
                UpdateOverallStatus();
            }));
        }

        private void HandlePanelServerStateChange(object sender, RunState newState) {
            Dispatcher.Invoke(new Action(() => {
                setStatusImage(imgPanelServerStatus, newState);
                setButtonText(cmdPanelServer, newState);
                PanelServerActive = newState == RunState.Started ? true : false;
                UpdateOverallStatus();
            }));
        }

        public void HandleMatricIntegrationStateChange(object sender, RunState newState) {
            Dispatcher.Invoke(new Action(() => {
                setStatusImage(imgMatricStatus, newState);
                setButtonText(cmdMatric, newState);
                MatricIntegrationActive = newState == RunState.Started ? true : false;
                UpdateOverallStatus();
            }));
        }

        private void UpdateOverallStatus() {
            string status = GetFormattedStatus();
            setInfoText(status);
        }

        private string GetFormattedStatus() {
            string coreStatus = GetStateEmoji(ServerCore.CurrentState.State);
            string matricStatus = GetStateEmoji(ServerCore.MatricAPI.CurrentState.State);
            string panelStatus = GetStateEmoji(ServerCore.PanelServer.CurrentState.State);
            
            return $"{coreStatus} Core | {matricStatus} Matric | {panelStatus} Panel";
        }

        private string GetStateEmoji(RunState state) {
            return state switch {
                RunState.Stopped => "⏹️ Stopped",
                RunState.Starting => "🔄 Starting",
                RunState.Started => "✅ Started",
                RunState.Stopping => "⏸️ Stopping",
                _ => "❓ Unknown"
            };
        }

        private void setInfoText(string newInfoText) {
            txtInfoText.Text = newInfoText;
        }

        private void setButtonText(Button target, RunState newState) {
            
            switch (newState) {
                case RunState.Stopped:
                    target.Content = "Start";
                    target.IsEnabled = true;
                    break;

                case RunState.Starting:
                    target.Content = "Starting...";
                    target.IsEnabled = false;
                    break;

                case RunState.Started:
                    target.Content = "Stop";
                    target.IsEnabled = true;
                    break;

                case RunState.Stopping:
                    target.Content = "Stopping...";
                    target.IsEnabled = false;
                    break;
            }
        }

        private void setStatusImage(Image target, RunState newState) {

            switch (newState) {
                case RunState.Stopped:
                    target.Source = new BitmapImage(new Uri("pack://application:,,,/Images/minus32.png"));
                    break;

                case RunState.Starting:
                case RunState.Stopping:
                    target.Source = new BitmapImage(new Uri("pack://application:,,,/Images/refresh32.png"));
                    break;

                case RunState.Started:
                    target.Source = new BitmapImage(new Uri("pack://application:,,,/Images/yes32.png"));
                    break;
            }
        }

        private void mainTabMenu_SelectionChanged(object sender, SelectionChangedEventArgs e) {

        }
    }
}
