
using EliteFIPServer.Logging;
using Matric.Integration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace EliteFIPServer {
    /// <summary>
    /// Interaction logic for ServerConsole.xaml
    /// </summary>
    public partial class ServerConsole : Window {

        private ServerCore ServerCore;
        private bool MatricIntegrationActive = false;
        private bool PanelServerActive = false;
        private List<ClientInfo> MatricClientList = new List<ClientInfo>();

        public ServerConsole() {
            InitializeComponent();
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            txtVersion.Text = version.ToString();
            Log.LogEnabled(Properties.Settings.Default.EnableLog);
            refreshSettingsTab();
            ServerCore = new ServerCore(this);
            dgMatricClients.ItemsSource = MatricClientList;

        }

        private void refreshSettingsTab() {

            chkEnableLog.IsChecked = Properties.Settings.Default.EnableLog;
            chkAutostartMatricIntegration.IsChecked = Properties.Settings.Default.AutostartMatricIntegration;
            txtMatricPort.Value = Properties.Settings.Default.MatricApiPort;
            txtMatricRetryInterval.Value = Properties.Settings.Default.MatricRetryInterval;
            chkAutostartPanelServer.IsChecked= Properties.Settings.Default.AutostartPanelServer;
            txtPanelServerPort.Value = Properties.Settings.Default.PanelServerPort;
        }

        private void saveSettings() {                       

            string messageBoxText = "Do you want to save changes?";
            string caption = "Elite FIP Server Settings";
            MessageBoxButton button = MessageBoxButton.YesNo;
            MessageBoxImage icon = MessageBoxImage.Warning;
            MessageBoxResult result;

            result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
            if (result == MessageBoxResult.Yes) {
                Log.Instance.Info("Saving settings");
                Properties.Settings.Default.EnableLog = (bool)chkEnableLog.IsChecked;
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
            cmdPanelServer.IsEnabled = false;
            if (PanelServerActive) {
                cmdPanelServer.Content = "Stopping...";
                ServerCore.StopPanelServer();
            } else {
                cmdPanelServer.Content = "Starting...";
                ServerCore.StartPanelServer();
            }
        }

        private void CmdRevertSettings_onClick(object sender, RoutedEventArgs e) {
            string messageBoxText = "Do you want to revert to saved settings?";
            string caption = "Elite FIP Server Settings";
            MessageBoxButton button = MessageBoxButton.YesNo;
            MessageBoxImage icon = MessageBoxImage.Warning;
            MessageBoxResult result;

            result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
            if (result == MessageBoxResult.Yes) {
                refreshSettingsTab();
            }

        }
        private void CmdsaveSettings_onClick(object sender, RoutedEventArgs e) {
            saveSettings();
        }

        public void updateServerCoreState(bool newstate) {
            setStatusImage(imgCoreServerStatus, newstate);
        }

        public void updateMatricState(bool newstate) {
            setStatusImage(imgMatricStatus, newstate);
            MatricIntegrationActive = newstate;
            cmdMatric.Content = newstate ? "Stop" : "Start";
            cmdMatric.IsEnabled = true;
        }

        public void updatePanelServerState(bool newstate) {
            setStatusImage(imgPanelServerStatus, newstate);
            PanelServerActive = newstate;
            cmdPanelServer.Content = newstate ? "Stop" : "Start";
            cmdPanelServer.IsEnabled = true;
        }
        public void updateInfoText(string newInfoText) {
            txtInfoText.Text = newInfoText;
        }   
        
        private void setStatusImage(Image target, bool started) {
            if (started) {
                target.Source = new BitmapImage(new Uri("pack://application:,,,/Images/yes32.png"));
            } else {
                target.Source = new BitmapImage(new Uri("pack://application:,,,/Images/minus32.png"));
            }
        }

        private void mainTabMenu_SelectionChanged(object sender, SelectionChangedEventArgs e) {

        }
    }
}
