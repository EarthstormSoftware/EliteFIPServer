using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Windows.System;

namespace EliteFIPServer;

public static class DashboardLinks
{
    public const string DashboardPath = "Dashboard.html";

    private static int Port => Properties.Settings.Default.PanelServerPort > 0 ? Properties.Settings.Default.PanelServerPort : 4545;

    public static string LocalBaseUrl => $"http://127.0.0.1:{Port}/";

    public static string LanBaseUrl
    {
        get
        {
            IPAddress address = GetLanAddress();
            return address == null ? null : $"http://{address}:{Port}/";
        }
    }

    public static Task OpenAsync(string relativePath = DashboardPath)
    {
        return Launcher.LaunchUriAsync(new Uri(LocalBaseUrl + relativePath)).AsTask();
    }

    // Prefer an adapter with an IPv4 gateway so virtual switches (Hyper-V, WSL, VPN) don't win over the real LAN.
    private static IPAddress GetLanAddress()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(n => n.GetIPProperties())
                .OrderByDescending(p => p.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork))
                .SelectMany(p => p.UnicastAddresses)
                .Select(a => a.Address)
                .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a));
        }
        catch (NetworkInformationException)
        {
            return null;
        }
    }
}
