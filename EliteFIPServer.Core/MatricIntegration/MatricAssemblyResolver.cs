using EliteFIPServer.Logging;
using System.IO;
using System.Reflection;

namespace EliteFIPServer;

public static class MatricAssemblyResolver
{
    private static bool registered;

    public static void Register()
    {
        if (registered) { return; }

        AppDomain.CurrentDomain.AssemblyResolve += ResolveMatricAssembly;
        registered = true;
    }

    private static Assembly ResolveMatricAssembly(object sender, ResolveEventArgs args)
    {
        string assemblyName = new AssemblyName(args.Name).Name;
        if (assemblyName != "MatricIntegration") { return null; }

        string installedPath = Path.Combine(MatricLocator.GetInstallDirectory(), "MatricIntegration.dll");
        if (!File.Exists(installedPath))
        {
            Log.Instance.Error("MatricIntegration.dll not found at {path}. Is MATRIC installed?", installedPath);
            return null;
        }

        return Assembly.LoadFrom(installedPath);
    }
}