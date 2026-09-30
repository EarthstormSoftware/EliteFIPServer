using Windows.Storage;
using Windows.System;

namespace EliteFIPServer;

internal static class LogFolder
{
    // NLog.config writes to %AppData%\EliteFIPServer. In a Store install Windows redirects those writes
    // into the package's LocalCache\Roaming folder, which Explorer does not see through the normal path.
    public static string Resolve()
    {
        try
        {
            string packagedFolder = Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "Roaming", "EliteFIPServer");
            if (Directory.Exists(packagedFolder))
            {
                return packagedFolder;
            }
        }
        catch (Exception)
        {
            // ApplicationData.Current throws when the app is not running from a package.
        }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EliteFIPServer");
    }

    public static async Task OpenAsync()
    {
        string folder = Resolve();
        try
        {
            Directory.CreateDirectory(folder);
            await Launcher.LaunchFolderPathAsync(folder);
        }
        catch (Exception ex)
        {
            Logging.Log.Instance.Warn("Unable to open the log folder {path}: {error}", folder, ex.Message);
        }
    }
}
