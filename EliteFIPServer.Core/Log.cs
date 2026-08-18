using NLog;
using NLog.Config;
using LogLevel = NLog.LogLevel;

namespace EliteFIPServer.Logging {
    public static class Log {

        public static Logger Instance { get; private set; }

        static Log() {
            // Note: NLogViewerTarget (Sentinel) was removed in NLog 6.x
            // File-based logging configured in NLog.config provides debugging capability

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
