namespace EliteFIPServer;

/// <summary>
/// Build information and versioning constants.
/// 
/// External version (4.0.Build.0) is used for package, file, and Store versions.
/// Build string is internal and includes date information for diagnostics.
/// 
/// When incrementing the build:
/// 1. Update AssemblyVersion in EliteFIPServer.UI.csproj to 4.0.{Build}.0
/// 2. Update BuildString below to 4.0.YYYYMMDD_{Build:D5}
/// 
/// Example:
///   Version: 4.0.1.0
///   BuildString: 4.0.20260818_00001
/// </summary>
internal static class BuildInfo
{
    /// <summary>
    /// Internal build string with date and build number.
    /// Format: Major.Minor.YYYYMMDD_BBBBB
    /// where BBBBB matches the third component of the version number.
    /// </summary>
    public const string BuildString = "4.0.20260929_00051";

    /// <summary>
    /// Application version (synchronized with assembly version).
    /// </summary>
    public const string Version = "4.0.51.0";
}
















































