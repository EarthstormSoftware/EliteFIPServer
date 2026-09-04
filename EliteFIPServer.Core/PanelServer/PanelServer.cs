using EliteFIPProtocol;
using EliteFIPServer.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;


namespace EliteFIPServer
{
    public class PanelServer {

        public static string ResolveWebRootPath() {
            var appBaseDir = AppContext.BaseDirectory;

            var candidateDirs = new[] {
                Path.Combine(appBaseDir, "wwwroot"),
                Path.Combine(appBaseDir, "EliteFIPServer.UI", "wwwroot"),
                Path.Combine(appBaseDir, "..", "wwwroot"),
                Path.Combine(appBaseDir, "..", "..", "wwwroot")
            };

            foreach (var candidate in candidateDirs) {
                if (Directory.Exists(candidate)) {
                    return Path.GetFullPath(candidate);
                }
            }

            return Path.GetFullPath(Path.Combine(appBaseDir, "wwwroot"));
        }

        CoreServer serverCore;

        public ComponentState CurrentState { get; private set; }
        Task PanelServerTask;
        private CancellationTokenSource PanelServerCTS;
        private WebApplication PanelHost;
        GameDataUpdateController GameDataUpdateController;        

        public PanelServer(CoreServer serverCore) {
            this.serverCore = serverCore;
            this.CurrentState = new ComponentState();
            ClientConnect.SetDataProvider(serverCore.EliteAPIIntegration);
        }

        public void Start() {
            if (CurrentState.State == RunState.Started) {
                return;
            }

            Log.Instance.Info("Panel Server starting");
            CurrentState.Set(RunState.Starting);
            bool panelServerStarted = false;

            try {
                var panelServerPort = Properties.Settings.Default.PanelServerPort;
                if (panelServerPort <= 0 || panelServerPort > 65535) {
                    throw new InvalidOperationException("Panel server port must be between 1 and 65535.");
                }

                var panelServerUrl = (Properties.Settings.Default.PanelServerAllowLanAccess ? "http://*:" : "http://127.0.0.1:") + panelServerPort;
                var webRootPath = ResolveWebRootPath();
                var contentRootPath = Path.GetDirectoryName(webRootPath) ?? AppContext.BaseDirectory;
                var panelServerBuilder = WebApplication.CreateBuilder(new WebApplicationOptions {
                    ApplicationName = typeof(PanelServer).Assembly.FullName,
                    ContentRootPath = contentRootPath,
                    WebRootPath = webRootPath,
                    Args = serverCore.ApplicationArgs
                });

                panelServerBuilder.Services.AddMvcCore().AddMvcOptions(options => options.EnableEndpointRouting=false);
                panelServerBuilder.Services.AddCors(cors => cors.AddPolicy("CorsPolicy", builder => {
                    builder
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .SetIsOriginAllowed(_ => true);
                }));
                panelServerBuilder.Services.AddControllers().AddNewtonsoftJson();
                panelServerBuilder.Services.AddSignalR();

                PanelHost = panelServerBuilder.Build();

                if (PanelHost.Environment.IsDevelopment()) {
                    PanelHost.UseDeveloperExceptionPage();
                }

                var authToken = Properties.Settings.Default.PanelServerAccessToken;
                if (!string.IsNullOrWhiteSpace(authToken)) {
                    PanelHost.Use(async (context, next) => {
                        var headerToken = context.Request.Headers["X-Panel-Auth"].FirstOrDefault();
                        var queryToken = context.Request.Query["token"].FirstOrDefault();
                        bool isAuthorized = string.Equals(headerToken, authToken, StringComparison.Ordinal) || string.Equals(queryToken, authToken, StringComparison.Ordinal);
                        if (!isAuthorized) {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsync("Unauthorized");
                            return;
                        }
                        await next();
                    });
                }

                Log.Instance.Info("Listening on {panelserverurl}", panelServerUrl);
                PanelHost.Urls.Add(panelServerUrl);
                PanelHost.UseStaticFiles();
                PanelHost.UseRouting();
                PanelHost.UseMvc();
                PanelHost.UseCors("CorsPolicy");

                PanelHost.MapHub<GameDataUpdateHub>("/gamedataupdatehub");
                var hubContext = PanelHost.Services.GetService(typeof(IHubContext<GameDataUpdateHub>)) as IHubContext<GameDataUpdateHub>;
                GameDataUpdateController = new GameDataUpdateController(hubContext);

                PanelServerCTS = new CancellationTokenSource();
                PanelServerTask = PanelHost.RunAsync(PanelServerCTS.Token);
                PanelServerTask.ContinueWith(PanelServerThreadEnded, TaskScheduler.Default);
                panelServerStarted = true;
            } catch (Exception ex) {
                Log.Instance.Error("Exception: {exception}", ex.ToString());
                panelServerStarted = false;
            }

            CurrentState.Set(panelServerStarted ? RunState.Started : RunState.Stopped);
            Log.Instance.Info("Panel server start complete");
        }

        public void Stop() {
            Log.Instance.Info("Panel server stopping");
            if ((CurrentState.State == RunState.Started || CurrentState.State == RunState.Starting) && PanelServerCTS != null) {
                CurrentState.Set(RunState.Stopping);
                PanelServerCTS.Cancel();

                try {
                    if (PanelHost != null) {
                        using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                        PanelHost.StopAsync(stopTimeout.Token).GetAwaiter().GetResult();
                    }
                } catch (Exception ex) {
                    Log.Instance.Warn("Panel server stop warning: {exception}", ex.ToString());
                }

                PanelHost = null;
            }
        }

        private void PanelServerThreadEnded(Task task) {
            if (task.Exception != null) {
                Log.Instance.Info("Panel Server Thread Exception: {exception}", task.Exception.ToString());
            }
            CurrentState.Set(RunState.Stopped);
            Log.Instance.Info("Panel Server Thread ended");
        }

        public async Task UpdateGameState(GameEventType eventType, Object gameData) {

            if (CurrentState.State != RunState.Started || GameDataUpdateController == null) {
                return;
            }

            try {
                if (eventType == GameEventType.Status) {
                    StatusData currentStatus = gameData as StatusData;
                    await GameDataUpdateController.SendStatusUpdate(currentStatus);

                } else if (eventType == GameEventType.Target) {
                    ShipTargetedData currentTarget = gameData as ShipTargetedData;
                    await GameDataUpdateController.SendTargetUpdate(currentTarget);

                } else if (eventType == GameEventType.Location) {
                    LocationData currentLocation = gameData as LocationData;
                    await GameDataUpdateController.SendLocationUpdate(currentLocation);

                } else if (eventType == GameEventType.Navigation) {
                    NavigationData currentNavRoute = gameData as NavigationData;
                    await GameDataUpdateController.SendNavRouteUpdate(currentNavRoute);

                } else if (eventType == GameEventType.PreviousNavRoute) {
                    NavigationData previousNavRoute = gameData as NavigationData;
                    await GameDataUpdateController.SendPreviousNavRoute(previousNavRoute);

                } else if (eventType == GameEventType.Jump) {
                    JumpData currentJumpData = gameData as JumpData;
                    await GameDataUpdateController.SendJumpUpdate(currentJumpData);
                }
            } catch (Exception ex) {
                Log.Instance.Warn("Panel server update failed: {exception}", ex.ToString());
            }
        }
    }
}
