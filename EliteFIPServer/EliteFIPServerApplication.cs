
using System.Reflection;
using System.IO;
using System.Windows;

namespace EliteFIPServer {
    public class EliteFIPServerApplication : Application {

        private static string[] AppArgs;


        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            AppArgs = args;

            // MatricIntegration.dll is compiled against a vendored copy (libs\Matric) but is not
            // shipped - at runtime load whichever copy is installed alongside the MATRIC app.
            AppDomain.CurrentDomain.AssemblyResolve += ResolveMatricAssembly;

            EliteFIPServerApplication serverApp = new EliteFIPServerApplication();
            serverApp.StartupUri = new Uri("ServerConsole.xaml", UriKind.RelativeOrAbsolute);
            serverApp.Run();
        }
        public static string[] GetArgs() { return AppArgs; }

        private static Assembly ResolveMatricAssembly(object sender, ResolveEventArgs args) {
            string assemblyName = new AssemblyName(args.Name).Name;
            if (assemblyName != "MatricIntegration") { return null; }

            string installedPath = Path.Combine(MatricLocator.GetInstallDirectory(), "MatricIntegration.dll");
            if (!File.Exists(installedPath)) {
                Logging.Log.Instance.Error("MatricIntegration.dll not found at {path}. Is MATRIC installed?", installedPath);
                return null;
            }
            return Assembly.LoadFrom(installedPath);
        }

    }
}
