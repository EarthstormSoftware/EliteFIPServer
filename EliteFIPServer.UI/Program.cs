using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Velopack;

namespace EliteFIPServer;

// Replaces the XAML-generated Main (DISABLE_XAML_GENERATED_MAIN) so Velopack can handle its install,
// update and uninstall hooks before any app startup code runs. The rest matches the generated Main.
public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        VelopackApp.Build()
            .OnBeforeUninstallFastCallback(_ => StartupRegistration.RemoveRunEntry())
            .Run();

        // Before App reads settings, so a copy replacing the Store version starts with its settings.
        AppInstallation.ImportStoreDataIfMissing();

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }
}
