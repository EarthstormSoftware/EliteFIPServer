using NLog;
using NLog.Config;
using NLog.Targets;
using LogLevel = NLog.LogLevel;

namespace EliteFIPServer.Logging {
    internal static class Log {

        public static Logger Instance { get; private set; }

        static Log() {
#if DEBUG
            // Setup the logging view for Sentinel - http://sentinel.codeplex.com
            var sentinelTarget = new NLogViewerTarget() {
                Name = "sentinel",
                Address = "udp://127.0.0.1:9999",
                IncludeNLogData = false
            };
            var sentinelRule = new LoggingRule("*", LogLevel.Trace, sentinelTarget);
            LogManager.Configuration.AddTarget("sentinel", sentinelTarget);
            LogManager.Configuration.LoggingRules.Add(sentinelRule);

#endif

            LogManager.ReconfigExistingLoggers();
            Instance = LogManager.GetCurrentClassLogger();
        }

        public static void LogEnabled(bool newState) {
            if (newState == true && LogManager.IsLoggingEnabled() == false) {
                LogManager.ResumeLogging();
            }
            if (newState == false && LogManager.IsLoggingEnabled() == true) {
                LogManager.SuspendLogging();
            }
        }
    }
}
