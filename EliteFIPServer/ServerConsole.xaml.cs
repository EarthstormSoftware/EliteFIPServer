
using EliteFIPServer.Logging;
using EliteFIPServer.Infrastructure;
using EliteFIPServer.Infrastructure.Services;
using EliteFIPServer.ViewModels;
using Matric.Integration;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace EliteFIPServer {
    /// <summary>
    /// Interaction logic for ServerConsole.xaml
    /// </summary>
    public partial class ServerConsole : Window {

        private CoreServer ServerCore;
        private ThemeManager ThemeManager;
        private IDialogService DialogService;
        private ServerStatusViewModel StatusViewModel;
        private SettingsViewModel SettingsViewModel;
        private ClientsViewModel ClientsViewModel;

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

            // Initialize CoreServer
            ServerCore = new CoreServer(EliteFIPServerApplication.GetArgs());

            // Create ViewModels
            StatusViewModel = new ServerStatusViewModel(ServerCore, DialogService);
            SettingsViewModel = new SettingsViewModel(DialogService, ThemeManager);
            ClientsViewModel = new ClientsViewModel(ServerCore);

            // Bind ViewModels to UI
            BindingOperations.SetBinding(txtInfoText, TextBox.TextProperty, 
                new System.Windows.Data.Binding("OverallStatus") { Source = StatusViewModel });

            cmdMatric.SetBinding(Button.ContentProperty, 
                new System.Windows.Data.Binding("MatricButtonText") { Source = StatusViewModel });
            cmdMatric.SetBinding(Button.IsEnabledProperty, 
                new System.Windows.Data.Binding("MatricButtonEnabled") { Source = StatusViewModel });

            cmdPanelServer.SetBinding(Button.ContentProperty, 
                new System.Windows.Data.Binding("PanelButtonText") { Source = StatusViewModel });
            cmdPanelServer.SetBinding(Button.IsEnabledProperty, 
                new System.Windows.Data.Binding("PanelButtonEnabled") { Source = StatusViewModel });

            imgCoreServerStatus.SetBinding(Image.SourceProperty, 
                new System.Windows.Data.Binding("CoreStatusImage") { Source = StatusViewModel });
            imgMatricStatus.SetBinding(Image.SourceProperty, 
                new System.Windows.Data.Binding("MatricStatusImage") { Source = StatusViewModel });
            imgPanelServerStatus.SetBinding(Image.SourceProperty, 
                new System.Windows.Data.Binding("PanelStatusImage") { Source = StatusViewModel });

            // Settings bindings
            chkEnableLog.SetBinding(CheckBox.IsCheckedProperty, 
                new System.Windows.Data.Binding("EnableLog") { Source = SettingsViewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            chkDarkMode.SetBinding(CheckBox.IsCheckedProperty, 
                new System.Windows.Data.Binding("DarkMode") { Source = SettingsViewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            chkAutostartMatricIntegration.SetBinding(CheckBox.IsCheckedProperty, 
                new System.Windows.Data.Binding("AutostartMatricIntegration") { Source = SettingsViewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            
            // Bind numeric spinners using reflection to find the ValueProperty
            var spinnerValueProperty = typeof(NumericSpinner).GetProperty("ValueProperty", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null) as System.Windows.DependencyProperty
                ?? typeof(NumericSpinner).GetField("ValueProperty", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null) as System.Windows.DependencyProperty;
            
            if (spinnerValueProperty != null)
            {
                BindingOperations.SetBinding(txtMatricPort, spinnerValueProperty, 
                    new System.Windows.Data.Binding("MatricApiPort") { Source = SettingsViewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
                BindingOperations.SetBinding(txtMatricRetryInterval, spinnerValueProperty, 
                    new System.Windows.Data.Binding("MatricRetryInterval") { Source = SettingsViewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
                BindingOperations.SetBinding(txtPanelServerPort, spinnerValueProperty, 
                    new System.Windows.Data.Binding("PanelServerPort") { Source = SettingsViewModel, Mode = System.Windows.Data.BindingMode.TwoWay });
            }
            
            chkAutostartPanelServer.SetBinding(CheckBox.IsCheckedProperty, 
                new System.Windows.Data.Binding("AutostartPanelServer") { Source = SettingsViewModel, Mode = System.Windows.Data.BindingMode.TwoWay });

            // Clients binding
            dgMatricClients.SetBinding(DataGrid.ItemsSourceProperty, 
                new System.Windows.Data.Binding("Clients") { Source = ClientsViewModel });

            ServerCore.Start();
            StatusViewModel.UpdateAllStatus();
            
            this.Closing += ServerConsole_Closing;
        }

        private void ServerConsole_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            ServerCore.Stop();
            StatusViewModel?.Cleanup();
        }

        private void MainTabMenu_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (tabClients.IsSelected) {
                Log.Instance.Info("Client tab selected");
                ClientsViewModel.RefreshClientsCommand.Execute(null);
            }
        }

        private void CmdMatric_onClick(object sender, RoutedEventArgs e) {
            try
            {
                if (StatusViewModel == null)
                {
                    MessageBox.Show("StatusViewModel is not initialized", "Error");
                    return;
                }
                
                if (StatusViewModel.MatricIntegrationActive) {
                    StatusViewModel.StopMatricCommand.Execute(null);
                } else {
                    StatusViewModel.StartMatricCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                Logging.Log.Instance.Error("Error in CmdMatric_onClick: {error}", ex.ToString());
                MessageBox.Show($"Error: {ex.Message}\n\n{ex.StackTrace}", "Matric Control Error");
            }
        }

        private void CmdPanelServer_onClick(object sender, RoutedEventArgs e) {
            try
            {
                if (StatusViewModel == null)
                {
                    MessageBox.Show("StatusViewModel is not initialized", "Error");
                    return;
                }
                
                if (StatusViewModel.PanelServerActive) {
                    StatusViewModel.StopPanelCommand.Execute(null);
                } else {
                    StatusViewModel.StartPanelCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                Logging.Log.Instance.Error("Error in CmdPanelServer_onClick: {error}", ex.ToString());
                MessageBox.Show($"Error: {ex.Message}\n\n{ex.StackTrace}", "Panel Control Error");
            }
        }

        private void CmdRevertSettings_onClick(object sender, RoutedEventArgs e) {
            try
            {
                if (SettingsViewModel == null)
                {
                    MessageBox.Show("SettingsViewModel is not initialized", "Error");
                    return;
                }
                SettingsViewModel.RevertSettingsCommand.Execute(null);
            }
            catch (Exception ex)
            {
                Logging.Log.Instance.Error("Error in CmdRevertSettings_onClick: {error}", ex.ToString());
                MessageBox.Show($"Error: {ex.Message}\n\n{ex.StackTrace}", "Settings Error");
            }
        }

        private void CmdsaveSettings_onClick(object sender, RoutedEventArgs e) {
            try
            {
                if (SettingsViewModel == null)
                {
                    MessageBox.Show("SettingsViewModel is not initialized", "Error");
                    return;
                }
                SettingsViewModel.SaveSettingsCommand.Execute(null);
            }
            catch (Exception ex)
            {
                Logging.Log.Instance.Error("Error in CmdsaveSettings_onClick: {error}", ex.ToString());
                MessageBox.Show($"Error: {ex.Message}\n\n{ex.StackTrace}", "Settings Error");
            }
        }
    }
}
