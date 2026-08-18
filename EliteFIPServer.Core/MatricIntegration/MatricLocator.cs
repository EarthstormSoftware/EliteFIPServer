using EliteFIPServer.Logging;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;

namespace EliteFIPServer {

    // Locates the MATRIC Desktop install directory so MatricIntegration.dll can be loaded from
    // whatever copy actually ships with the user's installed MATRIC version, rather than the
    // vendored compile-time-only stub in libs\Matric.
    public static class MatricLocator {

        public static string GetInstallDirectory() {
            string userConfigured = Properties.Settings.Default.MatricInstallPath;
            if (!string.IsNullOrWhiteSpace(userConfigured) && Directory.Exists(userConfigured)) {
                return userConfigured;
            }

            return GetFromAppPathsRegistry() ?? GetFromRunningProcess() ?? Constants.MatricInstallDirectory;
        }

        private static string GetFromAppPathsRegistry() {
            try {
                string exePath = Registry.GetValue(
                    $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{Constants.MatricProcessName}",
                    null, null) as string;

                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath)) {
                    return Path.GetDirectoryName(exePath);
                }
            } catch (Exception ex) {
                Log.Instance.Warn(ex, "Failed to read the MATRIC install location from the App Paths registry key");
            }
            return null;
        }

        private static string GetFromRunningProcess() {
            try {
                string processName = Path.GetFileNameWithoutExtension(Constants.MatricProcessName);
                Process[] processes = Process.GetProcessesByName(processName);
                string exePath = processes.Length > 0 ? processes[0].MainModule?.FileName : null;
                if (!string.IsNullOrEmpty(exePath)) {
                    return Path.GetDirectoryName(exePath);
                }
            } catch (Exception ex) {
                Log.Instance.Warn(ex, "Failed to locate the running MATRIC process");
            }
            return null;
        }
    }
}
