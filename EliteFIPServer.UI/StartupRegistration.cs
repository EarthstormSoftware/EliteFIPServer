using Microsoft.Win32;
using Windows.ApplicationModel;

namespace EliteFIPServer;

internal enum StartupState
{
    Disabled,
    Enabled,
    DisabledByUser,
    DisabledByPolicy
}

// "Start with Windows". The Store package registers a StartupTask (Package.appxmanifest), which the user
// also controls from Task Manager and Settings > Apps > Startup. An unpackaged build uses the per-user Run key.
internal static class StartupRegistration
{
    private const string TaskId = "EliteFIPServerStartup";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "EliteFIPServer";

    public static async Task<StartupState> GetStateAsync()
    {
        if (IsPackaged())
        {
            StartupTask task = await StartupTask.GetAsync(TaskId);
            return Map(task.State);
        }

        using RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(RunValueName) is string ? StartupState.Enabled : StartupState.Disabled;
    }

    public static async Task<StartupState> SetEnabledAsync(bool enabled)
    {
        if (IsPackaged())
        {
            StartupTask task = await StartupTask.GetAsync(TaskId);
            if (enabled)
            {
                return Map(await task.RequestEnableAsync());
            }

            task.Disable();
            return Map(task.State);
        }

        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
        {
            key.SetValue(RunValueName, $"\"{Environment.ProcessPath}\"");
            return StartupState.Enabled;
        }

        key.DeleteValue(RunValueName, false);
        return StartupState.Disabled;
    }

    private static StartupState Map(StartupTaskState state)
    {
        return state switch
        {
            StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy => StartupState.Enabled,
            StartupTaskState.DisabledByUser => StartupState.DisabledByUser,
            StartupTaskState.DisabledByPolicy => StartupState.DisabledByPolicy,
            _ => StartupState.Disabled
        };
    }

    private static bool IsPackaged()
    {
        try
        {
            return Package.Current != null;
        }
        catch (Exception)
        {
            // Package.Current throws when the app is not running from a package.
            return false;
        }
    }
}
