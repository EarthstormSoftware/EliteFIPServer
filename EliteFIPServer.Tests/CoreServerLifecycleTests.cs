using EliteFIPServer;
using Xunit;

namespace EliteFIPServer.Tests;

public class CoreServerLifecycleTests
{
    [Fact]
    public void Stop_should_not_throw_when_called_after_start()
    {
        var coreServer = new CoreServer(Array.Empty<string>());

        coreServer.Start();
        coreServer.Stop();

        Assert.Equal(RunState.Stopped, coreServer.CurrentState.State);
    }

    [Fact]
    public void GameDataEvent_should_not_throw_when_stopped()
    {
        var coreServer = new CoreServer(Array.Empty<string>());

        coreServer.Stop();

        var exception = Record.Exception(() => coreServer.GameDataEvent(GameEventType.Status, new object()));

        Assert.Null(exception);
    }

    [Fact]
    public void ResolveWebRootPath_should_use_the_app_wwwroot_directory()
    {
        var expected = Path.Combine(AppContext.BaseDirectory, "wwwroot");

        Assert.Equal(expected, PanelServer.ResolveWebRootPath());
    }

    [Fact]
    public void Start_panel_server_switch_should_not_reach_the_web_host_arguments()
    {
        var coreServer = new CoreServer(new[] { "--urls=http://127.0.0.1:1", CoreServer.StartPanelServerSwitch.ToUpperInvariant() });

        Assert.Equal(new[] { "--urls=http://127.0.0.1:1" }, coreServer.ApplicationArgs);
    }

    [Fact]
    public async Task PanelServer_should_serve_the_landing_page_at_the_root_url()
    {
        var previousPort = Properties.Settings.Default.PanelServerPort;
        var previousLanAccess = Properties.Settings.Default.PanelServerAllowLanAccess;
        var coreServer = new CoreServer(Array.Empty<string>());

        try
        {
            Properties.Settings.Default.PanelServerPort = 4545;
            Properties.Settings.Default.PanelServerAllowLanAccess = false;
            coreServer.PanelServer.Start();

            // The web host starts in the background; allow it a few seconds to begin listening.
            using var client = new HttpClient();
            HttpResponseMessage response = null;
            for (var attempt = 0; attempt < 20 && response == null; attempt++)
            {
                try { response = await client.GetAsync("http://127.0.0.1:4545/"); }
                catch (HttpRequestException) { await Task.Delay(250); }
            }

            Assert.NotNull(response);

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Dashboard.html", await response.Content.ReadAsStringAsync());
        }
        finally
        {
            coreServer.PanelServer.Stop();
            Properties.Settings.Default.PanelServerPort = previousPort;
            Properties.Settings.Default.PanelServerAllowLanAccess = previousLanAccess;
        }
    }

    [Fact]
    public void PanelServer_should_start_and_remain_started_on_an_available_port()
    {
        var previousPort = Properties.Settings.Default.PanelServerPort;
        var previousLanAccess = Properties.Settings.Default.PanelServerAllowLanAccess;

        try
        {
            Properties.Settings.Default.PanelServerPort = 4545;
            Properties.Settings.Default.PanelServerAllowLanAccess = false;

            var coreServer = new CoreServer(Array.Empty<string>());
            coreServer.PanelServer.Start();

            Thread.Sleep(500);

            var taskField = typeof(PanelServer).GetField("PanelServerTask", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var task = taskField?.GetValue(coreServer.PanelServer) as Task;

            Console.WriteLine($"Task status after 500ms: {task?.Status}");
            Console.WriteLine($"Current state after 500ms: {coreServer.PanelServer.CurrentState.State}");
            if (task != null && task.Exception != null)
            {
                Console.WriteLine(task.Exception.ToString());
                throw new InvalidOperationException(task.Exception.ToString());
            }

            Assert.Equal(RunState.Started, coreServer.PanelServer.CurrentState.State);

            coreServer.PanelServer.Stop();
            Thread.Sleep(250);
            Assert.Equal(RunState.Stopped, coreServer.PanelServer.CurrentState.State);
        }
        finally
        {
            Properties.Settings.Default.PanelServerPort = previousPort;
            Properties.Settings.Default.PanelServerAllowLanAccess = previousLanAccess;
        }
    }
}
