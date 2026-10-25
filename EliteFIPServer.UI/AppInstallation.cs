using EliteFIPServer.Logging;
using System.IO;
using Velopack;
using Velopack.Sources;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace EliteFIPServer;

// The two ways the app is installed: the Microsoft Store package, and the installer from GitHub Releases
// (Velopack). Only installer copies check GitHub for updates: IsInstalled is false for the Store package
// and for dev builds, so the Store version keeps updating through the Store. Either copy warns when the
// other is also installed.
internal static class AppInstallation
{
    private const string RepositoryUrl = "https://github.com/EarthstormSoftware/EliteFIPServer";
    private const string StoreDisplayName = "Elite FIP Server";
    private const string AppFolderName = "EliteFIPServer";
    // Velopack installs to %LocalAppData%\<packId>; the packId is set in Build-Installer.ps1.
    private const string InstallerPackId = "EarthstormSoftware.EliteFIPServer";

    public static bool IsPackaged
    {
        get
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

    private static readonly Lazy<UpdateManager> updateManager = new(() =>
        new UpdateManager(new GithubSource(RepositoryUrl, null, false)));

    public static bool IsInstalled
    {
        get
        {
            try
            {
                return updateManager.Value.IsInstalled;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    // Checks GitHub and downloads a newer release; null when up to date. A downloaded update that isn't
    // applied now is applied by Velopack the next time the app starts.
    public static async Task<UpdateInfo> DownloadUpdateAsync()
    {
        UpdateInfo update = await updateManager.Value.CheckForUpdatesAsync();
        if (update != null)
        {
            await updateManager.Value.DownloadUpdatesAsync(update);
        }

        return update;
    }

    // Starts the updater, which waits for this process to exit, applies the update and restarts the app.
    public static void ApplyUpdateOnExit(UpdateInfo update)
    {
        updateManager.Value.WaitExitThenApplyUpdates(update.TargetFullRelease, silent: false, restart: true);
    }

    // A Store install keeps %AppData% in its package's private storage, so an installer copy would
    // otherwise start from defaults. Copies that data across once, never over existing files.
    public static void ImportStoreDataIfMissing()
    {
        try
        {
            if (!IsInstalled)
            {
                return;
            }

            string target = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppFolderName);
            if (File.Exists(Path.Combine(target, "settings.json")))
            {
                return;
            }

            string source = FindStoreDataFolder();
            if (source == null)
            {
                return;
            }

            foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string destination = Path.Combine(target, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                if (!File.Exists(destination))
                {
                    File.Copy(file, destination);
                }
            }

            Log.Instance.Info("Imported settings from the Microsoft Store copy at {source}", source);
        }
        catch (Exception ex)
        {
            Log.Instance.Warn(ex, "Could not import settings from the Microsoft Store copy");
        }
    }

    private static string FindStoreDataFolder()
    {
        string packagesRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages");
        if (!Directory.Exists(packagesRoot))
        {
            return null;
        }

        return Directory.EnumerateDirectories(packagesRoot)
            .Select(package => Path.Combine(package, "LocalCache", "Roaming", AppFolderName))
            .Where(folder => File.Exists(Path.Combine(folder, "settings.json")))
            .OrderByDescending(folder => File.GetLastWriteTimeUtc(Path.Combine(folder, "settings.json")))
            .FirstOrDefault();
    }

    // The name of the other kind of install if this user has both, otherwise null. Dev builds are neither
    // kind and never report one.
    public static string FindOtherCopy()
    {
        if (IsPackaged)
        {
            return InstallerCopyExists() ? "GitHub Releases version" : null;
        }

        return IsInstalled && StoreCopyExists() ? "Microsoft Store version" : null;
    }

    private static bool InstallerCopyExists()
    {
        string exe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            InstallerPackId, "current", "EliteFIPServer.exe");
        return File.Exists(exe);
    }

    // Matched by display name and confirmed by the exe the package contains.
    private static bool StoreCopyExists()
    {
        foreach (Package package in new PackageManager().FindPackagesForUser(string.Empty))
        {
            try
            {
                if (!package.IsFramework
                    && package.DisplayName == StoreDisplayName
                    && File.Exists(Path.Combine(package.InstalledLocation.Path, "EliteFIPServer.exe")))
                {
                    return true;
                }
            }
            catch (Exception)
            {
                // Some system packages refuse property reads; they are never this app.
            }
        }

        return false;
    }
}
