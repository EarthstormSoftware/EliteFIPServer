using EliteFIPServer.Logging;
using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EliteJournalReader;
using EliteJournalReader.Events;
using EliteFIPProtocol;


namespace EliteFIPServer {

    public enum CoreState {
        Stopped,
        Starting,
        Started,
        Stopping,
    }

    public enum GameEventType {
        Status,
        Target,
        ServerStop
    }

    public struct GameEventTrigger {
        public GameEventType GameEvent { get; set; }       
        public Object EventData { get; set; }

        public GameEventTrigger(GameEventType gameEvent, Object eventData) {
            GameEvent = gameEvent;            
            EventData = eventData;
        }
    }

    class ServerCore : IGameDataEvent {

        // Reference to Primary UI 
        private EDFIPServerConsole ServerConsole;

        // Server States
        private CoreState ServerCoreState { get; set; }        

        // Game Event Worker
        private CancellationTokenSource GameDataWorkerCTS;
        private Task GameDataTask;
        private CoreState GameDataWorkerState { get; set; }
        ConcurrentQueue<GameEventTrigger> GameDataQueue = new ConcurrentQueue<GameEventTrigger>();
        private static EventWaitHandle GameDataWorkerWait;

        // Game State Handling
        private string JournalFileFolder;
        private StatusWatcher StatusWatcher;
        private JournalWatcher JournalWatcher;

        private StatusData currentStatus;
        private ShipTargetedData currentTarget;

        // Event Handling
        private StatusEventHandler StatusEventHandler;
        private TargetEventHandler  TargetEventHandler;

        // Matric Integration
        private MatricIntegration matricapi = new MatricIntegration();

        public ServerCore (EDFIPServerConsole serverConsole) {
            ServerConsole = serverConsole;

            // Set up the Journal feeds to be passed to clients
            var userHome = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
            JournalFileFolder = userHome + Constants.GameStateFolder;
            Log.Instance.Info("Tracking game data from folder: {journalfilefolder}", JournalFileFolder);
            StatusWatcher = new StatusWatcher(JournalFileFolder);
            JournalWatcher = new JournalWatcher(JournalFileFolder);
            GameDataWorkerWait = new EventWaitHandle(false, EventResetMode.AutoReset);

            // Add events to watch list
            StatusEventHandler = new StatusEventHandler(this);
            StatusWatcher.StatusUpdated += StatusEventHandler.HandleEvent;

            TargetEventHandler = new TargetEventHandler(this);
            JournalWatcher.GetEvent<ShipTargetedEvent>().Fired += TargetEventHandler.HandleEvent;

            // If Immediate start is enabled, start the server
            if (Properties.Settings.Default.ImmediateStart == true) {
                this.Start();
            }
            
        }

        public void Start() {
            Log.Instance.Info("Server Core starting");
            ServerCoreState = CoreState.Starting;

            // Start Game Data Worker Thread
            Log.Instance.Info("Starting Game data worker");
            GameDataWorkerCTS = new CancellationTokenSource();            
            GameDataTask = new Task(new Action(GameDataWorkerThread), GameDataWorkerCTS.Token);
            GameDataTask.ContinueWith(GameDataWorkerThreadEnded);
            GameDataTask.Start();

            // Start Matric Integration
            matricapi.Connect();

            // Start tracking game events
            StatusWatcher.StartWatching();
            JournalWatcher.StartWatching();

            ServerCoreState = CoreState.Started;
            ServerConsole.UpdateServerStatus(ServerCoreState);
            Log.Instance.Info("Server Core started");
        }

        public void Stop() {
            Log.Instance.Info("Server Core stopping");
            ServerCoreState = CoreState.Stopping;
            // Isssue the cancel to signal worker threads to end, and send stop message
            GameDataWorkerCTS.Cancel();
            GameDataEvent(GameEventType.ServerStop, null);

            // Stop tracking game events
            StatusWatcher.StopWatching();
            JournalWatcher.StopWatching();

            ServerCoreState = CoreState.Stopped;
            ServerConsole.UpdateServerStatus(ServerCoreState);
            Log.Instance.Info("Server Core stopped");
        }

        private void GameDataWorkerThread() {
            GameDataWorkerState = CoreState.Started;
            Log.Instance.Info("Game Data Worker Thread started");

            DateTime lastSuccessfulUpdate = DateTime.Today;

            CancellationToken token = GameDataWorkerCTS.Token;
            while (token.IsCancellationRequested == false) { 

                while (!GameDataQueue.IsEmpty) {
                    GameDataQueue.TryDequeue(out GameEventTrigger gameEventTrigger);
                    Log.Instance.Info("Game data event received");

                    if (gameEventTrigger.GameEvent == GameEventType.ServerStop) {
                        Log.Instance.Info("Server stop event received");
                        break;

                    } else if (gameEventTrigger.GameEvent == GameEventType.Status) {
                        currentStatus = gameEventTrigger.EventData as StatusData;
                        Log.Instance.Info("Current State: {gamestate}", JsonSerializer.Serialize(currentStatus));

                        // Update Matric state
                        Log.Instance.Info("Updating Matric status");
                        matricapi.UpdateStatus(currentStatus);

                    } else if (gameEventTrigger.GameEvent == GameEventType.Target) {
                        currentTarget = gameEventTrigger.EventData as ShipTargetedData;
                        Log.Instance.Info("Current Target: {target}", JsonSerializer.Serialize(currentTarget));

                        // Update Matric state
                        Log.Instance.Info("Updating Matric target");
                        matricapi.UpdateTarget(currentTarget);
                    }
                    Log.Instance.Info("Game Data Worker Thread waiting for new work");                    
                }
                GameDataWorkerWait.WaitOne();
            }
            Log.Instance.Info("Game Data Worker Thread ending");
        }

        private void GameDataWorkerThreadEnded(Task task) {
            GameDataWorkerState = CoreState.Stopped;
            if (task.Exception != null) {
                Log.Instance.Info("GameData Worker Thread Exception: {exception}", task.Exception.ToString());
            }
            Log.Instance.Info("GameData Worker Thread ended");
        }

        public void GameDataEvent(GameEventType eventType, Object evt) {

            GameEventTrigger newStatusEvent = new GameEventTrigger(eventType, evt);
            GameDataQueue.Enqueue(newStatusEvent);
            GameDataWorkerWait.Set();
        }

        public CoreState GetState() {
            return ServerCoreState;
        }

        public MatricIntegration GetMatricApi() {
            return matricapi;
        }        
    }
}
