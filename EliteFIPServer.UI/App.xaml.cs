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
        window = new MainWindow(Environment.GetCommandLineArgs().Skip(1).ToArray());
        window.Activate();
    }
}