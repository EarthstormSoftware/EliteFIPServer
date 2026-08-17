using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EliteFIPServer.Infrastructure;
using EliteFIPServer.Infrastructure.Services;
using EliteFIPServer.Logging;
using System.Windows;

namespace EliteFIPServer.ViewModels
{
    /// <summary>
    /// ViewModel for application settings management
    /// </summary>
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IDialogService _dialogService;
        private readonly ThemeManager _themeManager;

        [ObservableProperty]
        private bool enableLog;

        [ObservableProperty]
        private bool darkMode;

        [ObservableProperty]
        private bool autostartMatricIntegration;

        [ObservableProperty]
        private int matricApiPort;

        [ObservableProperty]
        private int matricRetryInterval;

        [ObservableProperty]
        private bool autostartPanelServer;

        [ObservableProperty]
        private int panelServerPort;

        public SettingsViewModel(IDialogService dialogService, ThemeManager themeManager)
        {
            _dialogService = dialogService;
            _themeManager = themeManager;

            LoadSettings();
        }

        [RelayCommand]
        public void SaveSettings()
        {
            MessageBoxResult result = _dialogService.ShowConfirmation("Elite FIP Server Settings", "Do you want to save changes?");

            if (result == MessageBoxResult.Yes)
            {
                Log.Instance.Info("Saving settings");
                Properties.Settings.Default.EnableLog = EnableLog;
                Properties.Settings.Default.DarkMode = DarkMode;
                Properties.Settings.Default.AutostartMatricIntegration = AutostartMatricIntegration;
                Properties.Settings.Default.MatricApiPort = MatricApiPort;
                Properties.Settings.Default.MatricRetryInterval = MatricRetryInterval;
                Properties.Settings.Default.AutostartPanelServer = AutostartPanelServer;
                Properties.Settings.Default.PanelServerPort = PanelServerPort;
                Properties.Settings.Default.Save();
                Log.LogEnabled(EnableLog);
            }
        }

        [RelayCommand]
        public void RevertSettings()
        {
            MessageBoxResult result = _dialogService.ShowConfirmation("Elite FIP Server Settings", "Do you want to revert to saved settings?");

            if (result == MessageBoxResult.Yes)
            {
                LoadSettings();
            }
        }

        partial void OnDarkModeChanged(bool value)
        {
            if (value && !_themeManager.IsDarkMode)
            {
                _themeManager.ToggleTheme();
            }
            else if (!value && _themeManager.IsDarkMode)
            {
                _themeManager.ToggleTheme();
            }
        }

        private void LoadSettings()
        {
            EnableLog = Properties.Settings.Default.EnableLog;
            DarkMode = Properties.Settings.Default.DarkMode;
            AutostartMatricIntegration = Properties.Settings.Default.AutostartMatricIntegration;
            MatricApiPort = Properties.Settings.Default.MatricApiPort;
            MatricRetryInterval = Properties.Settings.Default.MatricRetryInterval;
            AutostartPanelServer = Properties.Settings.Default.AutostartPanelServer;
            PanelServerPort = Properties.Settings.Default.PanelServerPort;
        }
    }
}
