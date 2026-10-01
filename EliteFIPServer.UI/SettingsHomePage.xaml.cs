using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EliteFIPServer;

public sealed partial class SettingsHomePage : Page
{
    public event EventHandler MatricSettingsRequested;

    private const string StartWithWindowsText = "Launch the application when you sign in to Windows";

    // Set while the toggle is updated from code, so that doesn't count as the user changing it.
    private bool updatingStartWithWindows;

    public SettingsHomePage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await RefreshStartWithWindowsAsync();
    }

    private async Task RefreshStartWithWindowsAsync()
    {
        try
        {
            ShowStartWithWindows(await StartupRegistration.GetStateAsync());
        }
        catch (Exception ex)
        {
            Logging.Log.Instance.Warn("Unable to read the Start with Windows setting: {error}", ex.Message);
            StartWithWindowsDescription.Text = "Not available on this PC";
        }
    }

    private async void StartWithWindows_Toggled(object sender, RoutedEventArgs e)
    {
        if (updatingStartWithWindows)
        {
            return;
        }

        StartWithWindowsToggle.IsEnabled = false;
        try
        {
            ShowStartWithWindows(await StartupRegistration.SetEnabledAsync(StartWithWindowsToggle.IsOn));
        }
        catch (Exception ex)
        {
            Logging.Log.Instance.Warn("Unable to change the Start with Windows setting: {error}", ex.Message);
            await RefreshStartWithWindowsAsync();
        }
    }

    private void ShowStartWithWindows(StartupState state)
    {
        updatingStartWithWindows = true;
        StartWithWindowsToggle.IsOn = state == StartupState.Enabled;
        updatingStartWithWindows = false;

        // Windows won't let the app turn this back on once the user has turned it off in Task Manager,
        // or when a policy controls it; the user has to change it there instead.
        StartWithWindowsToggle.IsEnabled = state is StartupState.Enabled or StartupState.Disabled;
        StartWithWindowsDescription.Text = state switch
        {
            StartupState.DisabledByUser => "Turned off in Task Manager. Turn it back on in Settings › Apps › Startup",
            StartupState.DisabledByPolicy => "Controlled by your organisation's policy",
            _ => StartWithWindowsText
        };
    }

    private void MatricSettings_Click(object sender, RoutedEventArgs e)
    {
        MatricSettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    private async void OpenLogFolder_Click(object sender, RoutedEventArgs e)
    {
        await LogFolder.OpenAsync();
    }

    private async void OpenPanelsFolder_Click(object sender, RoutedEventArgs e)
    {
        string folder = PanelServer.CustomPanelsPath;
        try
        {
            Directory.CreateDirectory(folder);
            await Windows.System.Launcher.LaunchFolderPathAsync(folder);
        }
        catch (Exception ex)
        {
            Logging.Log.Instance.Warn("Unable to open the custom panels folder {path}: {error}", folder, ex.Message);
        }
    }

    private void PanelRestartInfoBar_CloseButtonClick(InfoBar sender, object e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.PanelRestartRequired = false;
        }
    }
}
