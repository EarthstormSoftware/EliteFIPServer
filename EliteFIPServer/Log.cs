using NLog;
using NLog.Config;
using NLog.Targets;

namespace EliteFIPServer.Logging {
    internal static class Log {

        public static Logger Instance { get; private set; }

        static Log() {
#if DEBUG
            // Setup the logging view for Sentinel - http://sentinel.codeplex.com
            var sentinalTarget = new NLogViewerTarget() {
                Name = "sentinal",
                Address = "udp://127.0.0.1:9999",
                IncludeNLogData = false
            };
            var sentinalRule = new LoggingRule("*", LogLevel.Trace, sentinalTarget);
            LogManager.Configuration.AddTarget("sentinal", sentinalTarget);
            LogManager.Configuration.LoggingRules.Add(sentinalRule);

            // Setup the logging view for Harvester - http://harvester.codeplex.com
            /*
            var harvesterTarget = new OutputDebugStringTarget() {
                Name = "harvester",
                Layout = "${log4jxmlevent:includeNLogData=false}"
            };
            var harvesterRule = new LoggingRule("*", LogLevel.Trace, harvesterTarget);
            LogManager.Configuration.AddTarget("harvester", harvesterTarget);
            LogManager.Configuration.LoggingRules.Add(harvesterRule);
            */
#endif

            LogManager.ReconfigExistingLoggers();
            Instance = LogManager.GetCurrentClassLogger();
        }

        public static void LogEnabled(bool newState) {
            if (newState == true && LogManager.IsLoggingEnabled() == false) {
                LogManager.EnableLogging();
            }
            if (newState == false && LogManager.IsLoggingEnabled() == true) {
                LogManager.DisableLogging();
            }
        }
    }

    
}
