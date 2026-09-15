using Microsoft.UI.Xaml;

namespace EliteFIPServer;

public partial class App : Application
{
    private Window window;

    public App()
    {
        MatricAssemblyResolver.Register();
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs launchArgs)
    {
        var mainWindow = new MainWindow(Environment.GetCommandLineArgs().Skip(1).ToArray());
        window = mainWindow;

        if (Properties.Settings.Default.StartMinimised && Properties.Settings.Default.MinimiseToTray)
        {
            mainWindow.StartHiddenToTray();
            return;
        }

        window.Activate();

        if (Properties.Settings.Default.StartMinimised)
        {
            (mainWindow.AppWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter)?.Minimize();
        }
    }
}