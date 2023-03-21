using EliteFIPServer.Logging;
using System.Collections.Concurrent;


namespace EliteFIPServer
{
    public enum GameEventType {
        Empty,
        Status,
        Target,
        Location,
        Navigation,
        PreviousNavRoute,
        Jump
    }
    public struct GameEventTrigger {
        public GameEventType GameEvent { get; set; }
        public object EventData { get; set; }

        public GameEventTrigger(GameEventType gameEvent, Object eventData) {
            GameEvent = gameEvent;
            EventData = eventData;
        }
    }

    public class CoreServer {

        // Reference to Primary UI 
        private ServerConsole ServerConsole;

        // Server States
        public ComponentState CurrentState { get; private set; }

        // Game Event Worker
        private CancellationTokenSource GameDataWorkerCTS;
        private Task GameDataTask;
        private RunState GameDataWorkerState { get; set; }
        BlockingCollection<GameEventTrigger> GameDataQueue = new BlockingCollection<GameEventTrigger>(Constants.MaxGameDataQueueSize);

        public EliteAPIIntegration EliteAPIIntegration { get; private set; }

        // Matric Integration
        public MatricApiClient MatricAPI { get; private set; }

        // Panel Server
        public PanelServer PanelServer { get; private set; }


        public CoreServer(ServerConsole serverConsole) {
            ServerConsole = serverConsole;
            CurrentState = new ComponentState();

            EliteAPIIntegration = new EliteAPIIntegration(this);
            MatricAPI = new MatricApiClient();
            PanelServer = new PanelServer(this);            

        }

        public void Start() {
            Log.Instance.Info("Server Core starting");
            CurrentState.Set(RunState.Starting);            

            // Start Game Data Worker Thread
            Log.Instance.Info("Starting Game data worker");
            GameDataWorkerCTS = new CancellationTokenSource();
            GameDataTask = new Task(new Action(GameDataWorkerThread), GameDataWorkerCTS.Token);
            GameDataTask.ContinueWith(GameDataWorkerThreadEnded);
            GameDataTask.Start();

            EliteAPIIntegration.Start();

            // Start Matric Integration if set to autostart
            if (Properties.Settings.Default.AutostartMatricIntegration) {
                this.StartMatricIntegration();
            }
            // Start Matric Integration if set to autostart
            if (Properties.Settings.Default.AutostartPanelServer) {
                PanelServer.Start();
            }

            CurrentState.Set(RunState.Started);
            //ServerConsole.UpdateServerStatus(ServerCoreState);
            Log.Instance.Info("Server Core started");            
        }

        public void StartMatricIntegration() {
            Log.Instance.Info("Matric Integration starting");

            // Start Matric Integration
            MatricAPI.Start();
        }

        public void Stop() {
            Log.Instance.Info("Server Core stopping");            
            CurrentState.Set(RunState.Stopping);
            EliteAPIIntegration.Stop();

            // Isssue the cancel to signal worker threads to end
            GameDataWorkerCTS.Cancel();

            // Stop Matric Integration
            StopMatricIntegration();

            // Stop Panel Server
            PanelServer.Stop();

            GameDataQueue.CompleteAdding();
            CurrentState.Set(RunState.Stopped);                                   
            Log.Instance.Info("Server Core stopped");
        }

        public void StopMatricIntegration() {
            Log.Instance.Info("Matric Integration stopping");
            MatricAPI.Stop();
        }


        private void GameDataWorkerThread() {

            GameDataWorkerState = RunState.Started;
            Log.Instance.Info("Game Data Worker Thread started");

            DateTime lastSuccessfulUpdate = DateTime.Today;

            CancellationToken cToken = GameDataWorkerCTS.Token;


            while (cToken.IsCancellationRequested == false && !GameDataQueue.IsCompleted) {

                GameEventTrigger gameEventTrigger = new GameEventTrigger(GameEventType.Empty, null);
                try {
                    gameEventTrigger = GameDataQueue.Take(cToken);
                } catch (InvalidOperationException) { }

                if (gameEventTrigger.GameEvent != GameEventType.Empty) {
                    Log.Instance.Info("Updating {statetype} data", gameEventTrigger.GameEvent.ToString());
                    PanelServer.UpdateGameState(gameEventTrigger.GameEvent, gameEventTrigger.EventData);
                    MatricAPI.UpdateGameState(gameEventTrigger.GameEvent, gameEventTrigger.EventData);
                }
                Log.Instance.Info("Game Data Worker Thread waiting for new work");
            }
            Log.Instance.Info("Game Data Worker Thread ending");
        }

        private void GameDataWorkerThreadEnded(Task task) {
            GameDataWorkerState = RunState.Stopped;
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

        public MatricApiClient GetMatricApi() {
            return MatricAPI;
        }   
    }
}
