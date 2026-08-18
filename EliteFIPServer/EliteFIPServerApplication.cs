using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using EliteFIPServer.Infrastructure;
using EliteFIPServer.Infrastructure.Services;
using EliteFIPServer.ViewModels;

namespace EliteFIPServer {
    public class EliteFIPServerApplication : Application {

        private static string[] AppArgs;
        private static IServiceProvider ServiceProvider;


        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            AppArgs = args;

            // MatricIntegration.dll is compiled against a vendored copy (libs\Matric) but is not
            // shipped - at runtime load whichever copy is installed alongside the MATRIC app.
            MatricAssemblyResolver.Register();

            // Setup Dependency Injection
            ServiceProvider = ConfigureServices();

            EliteFIPServerApplication serverApp = new EliteFIPServerApplication();
            serverApp.StartupUri = new Uri("ServerConsole.xaml", UriKind.RelativeOrAbsolute);
            serverApp.Run();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Register services
            services.AddSingleton<ThemeManager>();
            services.AddSingleton<IDialogService, DialogService>();

            // Register ViewModels (as transient - will be created by ServerConsole)
            services.AddTransient<ServerStatusViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<ClientsViewModel>();

            return services.BuildServiceProvider();
        }

        public static IServiceProvider GetServiceProvider() => ServiceProvider;
        public static T GetService<T>() where T : class => ServiceProvider?.GetService(typeof(T)) as T;

        public static string[] GetArgs() { return AppArgs; }

    }
}
