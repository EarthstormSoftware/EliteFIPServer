
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
            EliteFIPServerApplication serverApp = new EliteFIPServerApplication();
            serverApp.StartupUri = new Uri("ServerConsole.xaml", UriKind.RelativeOrAbsolute);
            serverApp.Run();
        }
        public static string[] GetArgs() { return AppArgs; }

    }
}
