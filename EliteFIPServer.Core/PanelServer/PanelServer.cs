
using EliteFIPServer.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
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
                Path.Combine(appBaseDir, "..", "..", "wwwroot"),
                Path.Combine(appBaseDir, "..", "..", "..", "..", "EliteFIPServer.Core", "wwwroot"),
                Path.Combine(appBaseDir, "..", "..", "..", "..", "..", "EliteFIPServer.Core", "wwwroot")
            };

            foreach (var candidate in candidateDirs) {
                if (Directory.Exists(candidate)) {
                    return Path.GetFullPath(candidate);
                }
            }

            return Path.GetFullPath(Path.Combine(appBaseDir, "wwwroot"));
        }

        // Users' own and edited pages. Documents, not AppData, because a Store install redirects AppData into
        // the package's private storage where users wouldn't find it; the built-in wwwroot is read-only there.
        public static string CustomPanelsPath { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EliteFIPServer", "Panels");

        // Relative paths of custom panel files that replace built-in ones, for the running server.
        public IReadOnlyList<string> ReplacedBuiltInFiles { get; private set; } = Array.Empty<string>();
        public int CustomPanelFileCount { get; private set; }

        CoreServer serverCore;

        public ComponentState CurrentState { get; private set; }
        public event EventHandler<string> StartFailed;
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
            StartFailed?.Invoke(this, null);
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

                if (!Properties.Settings.Default.PanelServerAllowLanAccess) {
                    // Only answer to loopback host names, so a website can't reach the server through a DNS name it points at 127.0.0.1.
                    panelServerBuilder.Configuration["AllowedHosts"] = "localhost;127.0.0.1;[::1]";
                }
                panelServerBuilder.Services.AddSignalR().AddJsonProtocol(options => {
                    options.PayloadSerializerOptions.PropertyNamingPolicy = null;
                });

                PanelHost = panelServerBuilder.Build();
                UseCustomPanels(webRootPath);

                if (PanelHost.Environment.IsDevelopment()) {
                    PanelHost.UseDeveloperExceptionPage();
                }

                // The dashboard is served by this server, so it never needs cross-origin access. Refuse requests
                // made by pages from other sites, including WebSocket connections, which CORS does not cover.
                PanelHost.Use(async (context, next) => {
                    if (!IsSameOriginRequest(context.Request)) {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return;
                    }
                    await next();
                });

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
                PanelHost.UseDefaultFiles();
                PanelHost.UseStaticFiles(new StaticFileOptions {
                    // Pages must revalidate (cheap via ETag) so long-running displays and home-screen icons pick up
                    // new asset ?v= versions after an update; the versioned CSS/JS keep normal caching.
                    OnPrepareResponse = context => {
                        if (context.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase)) {
                            context.Context.Response.Headers.CacheControl = "no-cache";
                        }
                    }
                });
                PanelHost.UseRouting();

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
                StartFailed?.Invoke(this, DescribeStartFailure(ex));
            }

            CurrentState.Set(panelServerStarted ? RunState.Started : RunState.Stopped);
            Log.Instance.Info("Panel server start complete");
        }

        // Files in the custom panels folder are served in place of built-in files with the same path, and
        // alongside them otherwise. PhysicalFileProvider refuses paths outside its folder and hidden files.
        private void UseCustomPanels(string webRootPath) {
            ReplacedBuiltInFiles = Array.Empty<string>();
            CustomPanelFileCount = 0;
            string customPath = CustomPanelsPath;
            if (!Properties.Settings.Default.UseCustomPanels || string.IsNullOrWhiteSpace(customPath) || !Directory.Exists(customPath)) {
                return;
            }

            var customFiles = Directory.EnumerateFiles(customPath, "*", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(customPath, file))
                .ToArray();
            CustomPanelFileCount = customFiles.Length;
            ReplacedBuiltInFiles = customFiles
                .Where(file => File.Exists(Path.Combine(webRootPath, file)))
                .Select(file => file.Replace('\\', '/'))
                .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            PanelHost.Environment.WebRootFileProvider = new CompositeFileProvider(
                new PhysicalFileProvider(Path.GetFullPath(customPath)),
                PanelHost.Environment.WebRootFileProvider);
            Log.Instance.Info("Serving {count} custom panel files from {path}", CustomPanelFileCount, customPath);
            foreach (string file in ReplacedBuiltInFiles) {
                Log.Instance.Info("Custom panel file replaces the built-in {file}", file);
            }
        }

        // Browsers send Origin on cross-origin requests and WebSocket upgrades. Requests without it come from
        // same-origin page loads or non-browser clients.
        public static bool IsSameOriginRequest(HttpRequest request) {
            string origin = request.Headers.Origin;
            if (string.IsNullOrEmpty(origin)) {
                return true;
            }
            return Uri.TryCreate(origin, UriKind.Absolute, out var originUri)
                && string.Equals(originUri.Authority, request.Host.Value, StringComparison.OrdinalIgnoreCase);
        }

        private static string DescribeStartFailure(Exception ex) {
            Exception root = ex is AggregateException aggregate ? aggregate.Flatten().InnerException ?? aggregate : ex;
            if (root is System.Net.Sockets.SocketException socketEx && socketEx.HResult == 10048) {
                return $"Port {Properties.Settings.Default.PanelServerPort} is already in use. Check the configured Panel Server port and that no other application is using it.";
            }
            return $"Unable to start the Panel Server: {root.Message}";
        }

        public void Stop() {
            Log.Instance.Info("Panel server stopping");
            if ((CurrentState.State == RunState.Started || CurrentState.State == RunState.Starting) && PanelServerCTS != null) {
                CurrentState.Set(RunState.Stopping);
                PanelServerCTS.Cancel();

                try {
                    if (PanelHost != null) {
                        var host = PanelHost;
                        // Run the shutdown on a thread-pool thread rather than awaiting it inline: ASP.NET Core's
                        // host shutdown can post continuations back to the calling SynchronizationContext, and
                        // Stop() is invoked synchronously from the WinUI dispatcher thread (button click handlers),
                        // so blocking there with GetAwaiter().GetResult() would deadlock waiting on itself.
                        Task.Run(async () => {
                            using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                            await host.StopAsync(stopTimeout.Token).ConfigureAwait(false);
                        }).GetAwaiter().GetResult();
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
                StartFailed?.Invoke(this, DescribeStartFailure(task.Exception));
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
                } else if (eventType == GameEventType.RouteTarget) {
                    await GameDataUpdateController.SendRouteTargetUpdate(gameData as RouteTargetData);
                } else if (eventType == GameEventType.ReceivedText) {
                    ReceivedTextData receivedTextData = gameData as ReceivedTextData;
                    await GameDataUpdateController.SendReceivedTextUpdate(receivedTextData);
                } else if (eventType == GameEventType.Station) {
                    await GameDataUpdateController.SendStationUpdate(gameData as StationData);
                } else if (eventType == GameEventType.Exploration) {
                    await GameDataUpdateController.SendExplorationUpdate(gameData as ExplorationData);
                } else if (eventType == GameEventType.Loadout) {
                    await GameDataUpdateController.SendLoadoutUpdate(gameData as LoadoutData);
                } else if (eventType == GameEventType.Mission) {
                    await GameDataUpdateController.SendMissionUpdate(gameData as MissionData);
                } else if (eventType == GameEventType.MissionCollection) {
                    await GameDataUpdateController.SendMissionCollectionUpdate(gameData as MissionCollectionData);
                } else if (eventType == GameEventType.MissionLifecycle) {
                    await GameDataUpdateController.SendMissionLifecycleUpdate(gameData as MissionLifecycleData);
                } else if (eventType == GameEventType.Docking) {
                    await GameDataUpdateController.SendDockingUpdate(gameData as DockingData);
                } else if (eventType == GameEventType.Cargo) {
                    await GameDataUpdateController.SendCargoUpdate(gameData as CargoData);
                } else if (eventType == GameEventType.Materials) {
                    await GameDataUpdateController.SendMaterialsUpdate(gameData as MaterialsData);
                } else if (eventType == GameEventType.Combat) {
                    await GameDataUpdateController.SendCombatUpdate(gameData as CombatData);
                } else if (eventType == GameEventType.System) {
                    await GameDataUpdateController.SendSystemUpdate(gameData as SystemData);
                } else if (eventType == GameEventType.SystemExploration) {
                    await GameDataUpdateController.SendSystemExplorationUpdate(gameData as SystemExplorationData);
                } else if (eventType == GameEventType.Exobiology) {
                    await GameDataUpdateController.SendExobiologyUpdate(gameData as ExobiologyData);
                } else if (PanelEventNames.TryGetValue(eventType, out string eventName)) {
                    await GameDataUpdateController.Send(eventName, gameData);
                }
            } catch (Exception ex) {
                Log.Instance.Warn("Panel server update failed: {exception}", ex.ToString());
            }
        }

        public Task SendSnapshot(string connectionId, StatusData status, ShipTargetedData target,
            LocationData location, NavigationData navigation, NavigationData previousNavigation,
            JumpData jump, RouteTargetData routeTarget, ReceivedTextData receivedText, StationData station,
            ExplorationData exploration, LoadoutData loadout, MissionData mission,
            MissionCollectionData missions, DockingData docking,
            CargoData cargo, MaterialsData materials, SystemData system, SystemExplorationData systemExploration,
            ExobiologyData exobiology, IEnumerable<(GameEventType EventType, object Data)> additional) {
            var additionalSends = additional
                .Where(item => item.Data != null && PanelEventNames.ContainsKey(item.EventType))
                .Select(item => GameDataUpdateController.Send(PanelEventNames[item.EventType], item.Data, connectionId));
            return Task.WhenAll(additionalSends.Append(Task.WhenAll(
                GameDataUpdateController.SendStatusUpdate(status, connectionId),
                GameDataUpdateController.SendTargetUpdate(target, connectionId),
                GameDataUpdateController.SendLocationUpdate(location, connectionId),
                GameDataUpdateController.SendNavRouteUpdate(navigation, connectionId),
                GameDataUpdateController.SendPreviousNavRoute(previousNavigation, connectionId),
                GameDataUpdateController.SendJumpUpdate(jump, connectionId),
                GameDataUpdateController.SendRouteTargetUpdate(routeTarget, connectionId),
                GameDataUpdateController.SendReceivedTextUpdate(receivedText, connectionId),
                GameDataUpdateController.SendStationUpdate(station, connectionId),
                GameDataUpdateController.SendExplorationUpdate(exploration, connectionId),
                GameDataUpdateController.SendLoadoutUpdate(loadout, connectionId),
                GameDataUpdateController.SendMissionUpdate(mission, connectionId),
                GameDataUpdateController.SendMissionCollectionUpdate(missions, connectionId),
                GameDataUpdateController.SendDockingUpdate(docking, connectionId),
                GameDataUpdateController.SendCargoUpdate(cargo, connectionId),
                GameDataUpdateController.SendMaterialsUpdate(materials, connectionId),
                GameDataUpdateController.SendSystemUpdate(system, connectionId),
                GameDataUpdateController.SendSystemExplorationUpdate(systemExploration, connectionId),
                GameDataUpdateController.SendExobiologyUpdate(exobiology, connectionId))));
        }

        // SignalR message names for event families sent without a dedicated controller method.
        internal static readonly IReadOnlyDictionary<GameEventType, string> PanelEventNames = new Dictionary<GameEventType, string> {
            [GameEventType.Commander] = "CommanderData",
            [GameEventType.CombatEarnings] = "CombatEarningsData",
            [GameEventType.Mining] = "MiningData",
            [GameEventType.Trade] = "TradeData",
            [GameEventType.Carrier] = "CarrierData",
            [GameEventType.OnFoot] = "OnFootData"
        };
    }
}
