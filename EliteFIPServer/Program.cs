namespace EliteFIPServer {
    static class Program {


        private static string[] AppArgs;

        private static EDFIPServerConsole ServerConsole;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {

            AppArgs = args;
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ServerConsole = new EDFIPServerConsole();
            Application.Run(ServerConsole);
        }

        public static string[] GetArgs() { return AppArgs; }
        public static EDFIPServerConsole GetServerConsole() { return ServerConsole; }


    }
}
