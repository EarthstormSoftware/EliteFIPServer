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
        Jump,
        ReceivedText
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

        // Server States
        public ComponentState CurrentState { get; private set; }
        public string[] ApplicationArgs { get; }

        // Game Event Worker
        private CancellationTokenSource GameDataWorkerCTS;
        private Task GameDataTask;
        private RunState GameDataWorkerState { get; set; }
        BlockingCollection<GameEventTrigger> GameDataQueue = new BlockingCollection<GameEventTrigger>(Constants.MaxGameDataQueueSize);

        public EliteAPIIntegration EliteAPIIntegration { get; private set; }

        // Matric Integration
        public MatricApiClient MatricAPI { get; private set; }
        public event EventHandler<IReadOnlyList<MatricClientSummary>> ConnectedMatricClientsChanged;
        public event EventHandler<MatricClientSummary> MatricClientAdded;
        public event EventHandler<MatricClientSummary> MatricClientRemoved;
        public event EventHandler<string> PanelClientConnected;
        public event EventHandler<string> PanelClientDisconnected;

        // Panel Server
        public PanelServer PanelServer { get; private set; }


        public CoreServer(string[] applicationArgs = null) {
            ApplicationArgs = applicationArgs ?? Array.Empty<string>();
            CurrentState = new ComponentState();

            EliteAPIIntegration = new EliteAPIIntegration(this);
            MatricAPI = new MatricApiClient();
            MatricAPI.ConnectedClientsChanged += OnConnectedMatricClientsChanged;
            MatricAPI.ClientAdded += OnMatricClientAdded;
            MatricAPI.ClientRemoved += OnMatricClientRemoved;
            GameDataUpdateHub.ClientConnected += OnPanelClientConnected;
            GameDataUpdateHub.ClientDisconnected += OnPanelClientDisconnected;
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

        public IReadOnlyList<MatricClientSummary> GetConnectedMatricClients() {
            return MatricAPI.GetConnectedClientSummaries();
        }

        public void RefreshConnectedMatricClients() {
            MatricAPI.RequestConnectedClients();
        }

        private void OnConnectedMatricClientsChanged(object sender, IReadOnlyList<MatricClientSummary> clients) {
            ConnectedMatricClientsChanged?.Invoke(this, clients);
        }

        private void OnMatricClientAdded(object sender, MatricClientSummary client)
        {
            MatricClientAdded?.Invoke(this, client);
        }

        private void OnMatricClientRemoved(object sender, MatricClientSummary client)
        {
            MatricClientRemoved?.Invoke(this, client);
        }

        private void OnPanelClientConnected(object sender, string connectionId)
        {
            PanelClientConnected?.Invoke(this, connectionId);
        }

        private void OnPanelClientDisconnected(object sender, string connectionId)
        {
            PanelClientDisconnected?.Invoke(this, connectionId);
        }
    }
}
