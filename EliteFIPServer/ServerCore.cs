using EliteFIPProtocol;
using EliteFIPServer.Logging;
using EliteAPI;
using EliteAPI.Abstractions;
using System.Collections.Concurrent;
using System.Text.Json;
using EliteFIPServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        Target         
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
        private EDFIPServerConsole ServerConsole;

        // Server States
        private State ServerCoreState { get; set; }

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

        // Event Handling
        private StatusEventHandler StatusEventHandler;
        private TargetEventHandler TargetEventHandler;

        // Matric Integration
        private MatricIntegration matricapi = new MatricIntegration();

        // Panel Server        
        private CancellationTokenSource PanelServerCTS;
        private Task PanelServerTask;
        private bool PanelServerStarted = false;
        private GameDataUpdateController GameDataUpdateController;

        public ServerCore(EDFIPServerConsole serverConsole) {
            ServerConsole = serverConsole;            

            // Set up the Journal feeds to be passed to clients
            //var userHome = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
            //var JournalFileFolder = userHome + Constants.GameStateFolder;
            //var OptionsFileFolder = userHome + Constants.OptionsFolder;
            //Log.Instance.Info("Tracking game data from folder: {journalfilefolder}", JournalFileFolder);
            //Log.Instance.Info("Options folder: {optionsfilefolder}", OptionsFileFolder);

            EliteAPI = EliteDangerousApi.Create();


            // Add events to watch list
            StatusEventHandler = new StatusEventHandler(this);
            EliteAPI.Events.On<EliteAPI.Events.Status.Ship.StatusEvent>(StatusEventHandler.HandleEvent);
                        
            TargetEventHandler = new TargetEventHandler(this);
            EliteAPI.Events.On<EliteAPI.Events.ShipTargetedEvent>(TargetEventHandler.HandleEvent);

            // Start Matric Integration
            matricapi.Connect(Properties.Settings.Default.MatricApiPort);

            // If Immediate start is enabled, start the server
            if (Properties.Settings.Default.ImmediateStart == true) {
                this.Start();
            }

        }

        public void Start() {
            Log.Instance.Info("Server Core starting");
            ServerCoreState = State.Starting;

            // Start Game Data Worker Thread
            Log.Instance.Info("Starting Game data worker");
            GameDataWorkerCTS = new CancellationTokenSource();
            GameDataTask = new Task(new Action(GameDataWorkerThread), GameDataWorkerCTS.Token);
            GameDataTask.ContinueWith(GameDataWorkerThreadEnded);
            GameDataTask.Start();

            // Start tracking game events
            EliteAPI.StartAsync();
            
            // Start Matric Flashing Lights thread
            matricapi.StartMatricFlashWorker();

            // Start Panel Server Thread
            if (Properties.Settings.Default.EnablePanelServer == true)
            {
                Log.Instance.Info("Starting Panel Server");
                try
                {
                    var panelServerUrl = "http://*:" + Properties.Settings.Default.PanelServerPort;
                    var panelServerBuilder = WebApplication.CreateBuilder(Program.GetArgs());
                    
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

                    if (panelServer.Environment.IsDevelopment())
                    {
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
                }
                catch (Exception ex)
                {
                    Log.Instance.Error("Exception: {exception}", ex.ToString());
                    PanelServerStarted = false;
                }
            }

            ServerCoreState = State.Started;
            ServerConsole.UpdateServerStatus(ServerCoreState);
            Log.Instance.Info("Server Core started");
        }

        public void Stop() {
            Log.Instance.Info("Server Core stopping");
            ServerCoreState = State.Stopping;
            // Isssue the cancel to signal worker threads to end
            GameDataWorkerCTS.Cancel();


            // Stop tracking game events
            EliteAPI.StopAsync();

            // Stop Matric Flashing Lights thread
            matricapi.StopMatricFlashWorker();

            // Stop Panel Server
            if (PanelServerStarted) {
                PanelServerCTS.Cancel();
            }

            GameDataQueue.CompleteAdding();
            ServerCoreState = State.Stopped;
            ServerConsole.UpdateServerStatus(ServerCoreState);
            Log.Instance.Info("Server Core stopped");
        }

        private void GameDataWorkerThread() {
            
            GameDataWorkerState = State.Started;
            Log.Instance.Info("Game Data Worker Thread started");

            DateTime lastSuccessfulUpdate = DateTime.Today;

            CancellationToken cToken = GameDataWorkerCTS.Token;
            while (cToken.IsCancellationRequested == false) {
                // Update Matric state
                if (matricapi.IsConnected()) {
                    Log.Instance.Info("Updating Matric state");
                    matricapi.UpdateStatus(currentStatus);                    
                    matricapi.UpdateTarget(currentTarget);
                }

                while (!GameDataQueue.IsCompleted) {

                    GameEventTrigger gameEventTrigger = new GameEventTrigger(GameEventType.Empty, null);
                    try {
                        gameEventTrigger = GameDataQueue.Take(cToken);
                    } catch (InvalidOperationException) { }

                    if (gameEventTrigger.GameEvent != GameEventType.Empty) {
                        Log.Instance.Info("Game data event received");

                        if (gameEventTrigger.GameEvent == GameEventType.Status) {
                            currentStatus = new StatusData();
                            currentStatus = gameEventTrigger.EventData as StatusData;
                            string statusJSON = JsonSerializer.Serialize(currentStatus);
                            Log.Instance.Info("Current State: {gamestate}", statusJSON);                            
                            if (PanelServerStarted) { GameDataUpdateController.SendStatusUpdate(currentStatus); }                                                    

                        }
                        else if (gameEventTrigger.GameEvent == GameEventType.Target) {
                            currentTarget = new ShipTargetedData();
                            currentTarget = gameEventTrigger.EventData as ShipTargetedData;
                            Log.Instance.Info("Current Target: {target}", JsonSerializer.Serialize(currentTarget));
                            if (PanelServerStarted) { GameDataUpdateController.SendTargetUpdate(currentTarget); }
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
            GameDataQueue.Add(newStatusEvent,cToken);            
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

        public MatricIntegration GetMatricApi() {
            return matricapi;
        }
    }
}
