using EliteAPI;
using EliteAPI.Abstractions;
using EliteFIPProtocol;
using EliteFIPServer.Hubs;
using EliteFIPServer.Logging;
using EliteFIPServer.MatricIntegration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Text.Json;

namespace EliteFIPServer {
    public enum State {
        Stopped,
        Starting,
        Started,
        Stopping,
    }

    public enum GameEventType {
        Empty,
        Status,
        Target,
        Location,
        Navigation
    }

    public struct GameEventTrigger {
        public GameEventType GameEvent { get; set; }
        public object EventData { get; set; }

        public GameEventTrigger(GameEventType gameEvent, Object eventData) {
            GameEvent = gameEvent;
            EventData = eventData;
        }
    }

    public class ServerCore : IGameDataEvent {

        // Reference to Primary UI 
        private ServerConsole ServerConsole;

        // Server States
        private State ServerCoreState { get; set; }
        private State MatricIntegrationState { get; set; }
        private State PanelServerState { get; set; }

        // Game Event Worker
        private CancellationTokenSource GameDataWorkerCTS;
        private Task GameDataTask;
        private State GameDataWorkerState { get; set; }
        BlockingCollection<GameEventTrigger> GameDataQueue = new BlockingCollection<GameEventTrigger>(Constants.MaxGameDataQueueSize);

        // Game State Handling
        private IEliteDangerousApi EliteAPI;

        // Current State Information
        private StatusData currentStatus;
        private ShipTargetedData currentTarget;
        private LocationData currentLocation;
        private NavigationData currentNavRoute;

        // Event Handling
        private StatusEventHandler StatusEventHandler;
        private TargetEventHandler TargetEventHandler;
        private LocationEventHandler LocationEventHandler;
        private FSDJumpEventHandler FSDJumpEventHandler;
        private NavRouteEventHandler NavRouteEventHandler;
        private NavRouteClearEventHandler NavRouteClearEventHandler;

        // Matric Integration
        private MatricApiClient matricapi = new MatricApiClient();

        // Panel Server        
        private CancellationTokenSource PanelServerCTS;
        private Task PanelServerTask;
        private bool PanelServerStarted = false;
        private GameDataUpdateController GameDataUpdateController;

        public ServerCore(ServerConsole serverConsole) {
            ServerConsole = serverConsole;
            ClientConnect.setServerCore(this);

            EliteAPI = EliteDangerousApi.Create();

            // Add events to watch list
            StatusEventHandler = new StatusEventHandler(this);
            EliteAPI.Events.On<EliteAPI.Events.Status.Ship.StatusEvent>(StatusEventHandler.HandleEvent);

            TargetEventHandler = new TargetEventHandler(this);
            EliteAPI.Events.On<EliteAPI.Events.ShipTargetedEvent>(TargetEventHandler.HandleEvent);

            LocationEventHandler = new LocationEventHandler(this);
            EliteAPI.Events.On<EliteAPI.Events.LocationEvent>(LocationEventHandler.HandleEvent);

            FSDJumpEventHandler = new FSDJumpEventHandler(this);
            EliteAPI.Events.On<EliteAPI.Events.FsdJumpEvent>(FSDJumpEventHandler.HandleEvent);

            NavRouteEventHandler = new NavRouteEventHandler(this);
            EliteAPI.Events.On<EliteAPI.Events.Status.NavRoute.NavRouteEvent>(NavRouteEventHandler.HandleEvent);

            NavRouteClearEventHandler = new NavRouteClearEventHandler(this);
            EliteAPI.Events.On<EliteAPI.Events.Status.NavRoute.NavRouteClearEvent>(NavRouteClearEventHandler.HandleEvent);

            this.Start();
        }

        public void Start() {
            Log.Instance.Info("Server Core starting");
            ServerCoreState = State.Starting;
            ServerConsole.updateInfoText("Starting core server...");

            // Start Game Data Worker Thread
            Log.Instance.Info("Starting Game data worker");
            GameDataWorkerCTS = new CancellationTokenSource();
            GameDataTask = new Task(new Action(GameDataWorkerThread), GameDataWorkerCTS.Token);
            GameDataTask.ContinueWith(GameDataWorkerThreadEnded);
            GameDataTask.Start();

            // Start Matric Integration if set to autostart
            if (Properties.Settings.Default.AutostartMatricIntegration) {
                this.StartMatricIntegration();
            }
            // Start Matric Integration if set to autostart
            if (Properties.Settings.Default.AutostartPanelServer) {
                this.StartPanelServer();
            }


            // Start tracking game events
            EliteAPI.StartAsync();

            // Start Matric Flashing Lights thread
            matricapi.StartMatricFlashWorker();

            // Start Panel Server Thread
            if (Properties.Settings.Default.AutostartPanelServer == true) {
                Log.Instance.Info("Starting Panel Server");
                ServerConsole.updateInfoText("Starting panel server...");
                try {
                    var panelServerUrl = "http://*:" + Properties.Settings.Default.PanelServerPort;
                    var panelServerBuilder = WebApplication.CreateBuilder(EliteFIPServerApplication.GetArgs());

                    panelServerBuilder.Services.AddMvcCore().AddMvcOptions(options => options.EnableEndpointRouting=false);
                    panelServerBuilder.Services.AddCors(cors => cors.AddPolicy("CorsPolicy", builder => {
                        builder
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials()
                            .WithOrigins(panelServerUrl);
                    }));
                    panelServerBuilder.Services.AddControllers().AddNewtonsoftJson();
                    panelServerBuilder.Services.AddSignalR();
                    var panelServer = panelServerBuilder.Build();

                    if (panelServer.Environment.IsDevelopment()) {
                        panelServer.UseDeveloperExceptionPage();
                    }

                    Log.Instance.Info("Listening on {panelserverurl}", panelServerUrl);
                    panelServer.Urls.Add(panelServerUrl);
                    panelServer.UseStaticFiles();
                    panelServer.UseRouting();
                    panelServer.UseMvc();
                    panelServer.UseCors("CorsPolicy");

                    panelServer.MapHub<GameDataUpdateHub>("/gamedataupdatehub");
                    var hubContext = panelServer.Services.GetService(typeof(IHubContext<GameDataUpdateHub>)) as IHubContext<GameDataUpdateHub>;
                    GameDataUpdateController = new GameDataUpdateController(hubContext);


                    PanelServerCTS = new CancellationTokenSource();
                    PanelServerTask = panelServer.RunAsync(PanelServerCTS.Token);
                    PanelServerTask.ContinueWith(PanelServerThreadEnded);
                    PanelServerStarted = true;
                } catch (Exception ex) {
                    Log.Instance.Error("Exception: {exception}", ex.ToString());
                    PanelServerStarted = false;
                }
            }

            ServerCoreState = State.Started;
            //ServerConsole.UpdateServerStatus(ServerCoreState);
            Log.Instance.Info("Server Core started");
            ServerConsole.updateInfoText("");
            ServerConsole.updateServerCoreState(true);
        }

        public void StartMatricIntegration() {
            Log.Instance.Info("Matric Integration starting");
            MatricIntegrationState = State.Starting;            

            // Start Matric Integration
            matricapi.Connect(Properties.Settings.Default.MatricApiPort);

            // Start Matric Flashing Lights thread
            matricapi.StartMatricFlashWorker();

            MatricIntegrationState = State.Started;                        
            ServerConsole.updateMatricState(true);
            Log.Instance.Info("Matric Integration started");
        }

        public void StartPanelServer() {
            Log.Instance.Info("Panel Server starting");
            PanelServerState = State.Starting;            

            try {
                var panelServerUrl = "http://*:" + Properties.Settings.Default.PanelServerPort;
                var panelServerBuilder = WebApplication.CreateBuilder(EliteFIPServerApplication.GetArgs());

                panelServerBuilder.Services.AddMvcCore().AddMvcOptions(options => options.EnableEndpointRouting=false);
                panelServerBuilder.Services.AddCors(cors => cors.AddPolicy("CorsPolicy", builder => {
                    builder
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .WithOrigins(panelServerUrl);
                }));
                panelServerBuilder.Services.AddControllers().AddNewtonsoftJson();
                panelServerBuilder.Services.AddSignalR();
                var panelServer = panelServerBuilder.Build();

                if (panelServer.Environment.IsDevelopment()) {
                    panelServer.UseDeveloperExceptionPage();
                }

                Log.Instance.Info("Listening on {panelserverurl}", panelServerUrl);
                panelServer.Urls.Add(panelServerUrl);
                panelServer.UseStaticFiles();
                panelServer.UseRouting();
                panelServer.UseMvc();
                panelServer.UseCors("CorsPolicy");

                panelServer.MapHub<GameDataUpdateHub>("/gamedataupdatehub");
                var hubContext = panelServer.Services.GetService(typeof(IHubContext<GameDataUpdateHub>)) as IHubContext<GameDataUpdateHub>;
                GameDataUpdateController = new GameDataUpdateController(hubContext);


                PanelServerCTS = new CancellationTokenSource();
                PanelServerTask = panelServer.RunAsync(PanelServerCTS.Token);
                PanelServerTask.ContinueWith(PanelServerThreadEnded);
                PanelServerStarted = true;
            } catch (Exception ex) {
                Log.Instance.Error("Exception: {exception}", ex.ToString());
                PanelServerStarted = false;
            }
            
            PanelServerState = PanelServerStarted ? State.Started : State.Stopped;                                   
            ServerConsole.updatePanelServerState(PanelServerStarted);
            Log.Instance.Info("Panel start complete");
        }


        public void Stop() {
            Log.Instance.Info("Server Core stopping");
            ServerConsole.updateInfoText("Core server stopping...");
            ServerCoreState = State.Stopping;
            // Isssue the cancel to signal worker threads to end
            GameDataWorkerCTS.Cancel();


            // Stop tracking game events
            EliteAPI.StopAsync();

            // Stop Matric Integration
            StopMatricIntegration();

            // Stop Panel Server
            StopPanelServer();

            GameDataQueue.CompleteAdding();
            ServerCoreState = State.Stopped;            
            
            ServerConsole.updateInfoText("");
            ServerConsole.updateServerCoreState(false);
            Log.Instance.Info("Server Core stopped");
        }

        public void StopMatricIntegration() {
            Log.Instance.Info("Matric Integration stopping");
            MatricIntegrationState = State.Stopping;            

            // Stop Matric Flashing Lights thread
            matricapi.StopMatricFlashWorker();

            MatricIntegrationState = State.Stopped;                        
            
            ServerConsole.updateMatricState(false);
            Log.Instance.Info("Matric Integration stopped");
        }

        public void StopPanelServer() {
            Log.Instance.Info("Panel server stopping");
            PanelServerState = State.Stopping;            

            // Stop Panel Server
            if (PanelServerStarted) {
                PanelServerCTS.Cancel();
            }
            
            PanelServerState = State.Stopped;                    
            
            ServerConsole.updatePanelServerState(false);
            Log.Instance.Info("Panel Server stopped");
        }

        private void GameDataWorkerThread() {

            GameDataWorkerState = State.Started;
            Log.Instance.Info("Game Data Worker Thread started");

            DateTime lastSuccessfulUpdate = DateTime.Today;

            CancellationToken cToken = GameDataWorkerCTS.Token;


            while (cToken.IsCancellationRequested == false && !GameDataQueue.IsCompleted) {

                GameEventTrigger gameEventTrigger = new GameEventTrigger(GameEventType.Empty, null);
                try {
                    gameEventTrigger = GameDataQueue.Take(cToken);
                } catch (InvalidOperationException) { }

                if (gameEventTrigger.GameEvent != GameEventType.Empty) {
                    Log.Instance.Info("Game data event received");

                    if (gameEventTrigger.GameEvent == GameEventType.Status) {
                        currentStatus = new StatusData();
                        currentStatus = gameEventTrigger.EventData as StatusData;
                        Log.Instance.Info("Current State: {gamestate}", JsonSerializer.Serialize(currentStatus));
                        if (PanelServerStarted) { GameDataUpdateController.SendStatusUpdate(currentStatus); }

                    } else if (gameEventTrigger.GameEvent == GameEventType.Target) {
                        currentTarget = new ShipTargetedData();
                        currentTarget = gameEventTrigger.EventData as ShipTargetedData;
                        Log.Instance.Info("Current Target: {target}", JsonSerializer.Serialize(currentTarget));
                        if (PanelServerStarted) { GameDataUpdateController.SendTargetUpdate(currentTarget); }
                    } else if (gameEventTrigger.GameEvent == GameEventType.Location) {
                        currentLocation = new LocationData();
                        currentLocation = gameEventTrigger.EventData as LocationData;
                        Log.Instance.Info("Current Location: {location}", JsonSerializer.Serialize(currentLocation));
                        if (PanelServerStarted) { GameDataUpdateController.SendLocationUpdate(currentLocation); }
                    } else if (gameEventTrigger.GameEvent == GameEventType.Navigation) {
                        currentNavRoute = new NavigationData();
                        currentNavRoute = gameEventTrigger.EventData as NavigationData;
                        Log.Instance.Info("Current Route: {route}", JsonSerializer.Serialize(currentNavRoute));
                        if (PanelServerStarted) { GameDataUpdateController.SendNavRouteUpdate(currentNavRoute); }
                    }
                    // Update Matric state
                    if (matricapi.IsConnected()) {
                        Log.Instance.Info("Updating Matric state");
                        matricapi.UpdateStatus(currentStatus);
                        matricapi.UpdateTarget(currentTarget);
                    }
                }
                Log.Instance.Info("Game Data Worker Thread waiting for new work");
            }
            Log.Instance.Info("Game Data Worker Thread ending");
        }

        private void GameDataWorkerThreadEnded(Task task) {
            GameDataWorkerState = State.Stopped;
            if (task.Exception != null) {
                Log.Instance.Info("GameData Worker Thread Exception: {exception}", task.Exception.ToString());
            }
            Log.Instance.Info("GameData Worker Thread ended");
        }

        public void GameDataEvent(GameEventType eventType, Object evt) {

            GameEventTrigger newStatusEvent = new GameEventTrigger(eventType, evt);
            CancellationToken cToken = GameDataWorkerCTS.Token;
            GameDataQueue.Add(newStatusEvent, cToken);
        }

        private void PanelServerThreadEnded(Task task) {
            if (task.Exception != null) {
                Log.Instance.Info("Panel Server Thread Exception: {exception}", task.Exception.ToString());
            }
            Log.Instance.Info("Panel Server Thread ended");
        }

        public State GetState() {
            return ServerCoreState;
        }

        public MatricApiClient GetMatricApi() {
            return matricapi;
        }

        public void fullClientUpdate() {
            if (currentStatus != null) { GameDataUpdateController.SendStatusUpdate(currentStatus); }
            if (currentTarget != null) { GameDataUpdateController.SendTargetUpdate(currentTarget); }
            if (currentLocation != null) { GameDataUpdateController.SendLocationUpdate(currentLocation); }
            if (currentNavRoute != null) { GameDataUpdateController.SendNavRouteUpdate(currentNavRoute); }
        }
    }
}
