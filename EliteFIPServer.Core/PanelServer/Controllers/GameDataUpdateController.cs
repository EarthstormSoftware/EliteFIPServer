using EliteFIPProtocol;
using Microsoft.AspNetCore.SignalR;
using System.Threading;

namespace EliteFIPServer
{
    class GameDataUpdateController
    {

        private readonly IHubContext<GameDataUpdateHub> _hubContext;
        private long sequence;

        public GameDataUpdateController(IHubContext<GameDataUpdateHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendStatusUpdate(StatusData statusData, string connectionId = null)
        {
            await SendAsync("StatusData", statusData, connectionId);
        }

        public async Task SendTargetUpdate(ShipTargetedData targetData, string connectionId = null)
        {
            await SendAsync("TargetData", targetData, connectionId);
        }

        public async Task SendLocationUpdate(LocationData locationData, string connectionId = null)
        {
            await SendAsync("LocationData", locationData, connectionId);
        }

        public async Task SendNavRouteUpdate(NavigationData navRouteData, string connectionId = null)
        {
            await SendAsync("NavRouteData", navRouteData, connectionId);
        }

        public async Task SendPreviousNavRoute(NavigationData navRouteData, string connectionId = null) {
            await SendAsync("PreviousNavRoute", navRouteData, connectionId);
        }

        public async Task SendJumpUpdate(JumpData jumpData, string connectionId = null) {
            await SendAsync("JumpData", jumpData, connectionId);
        }

        public Task SendRouteTargetUpdate(RouteTargetData data, string connectionId = null) => SendAsync("RouteTargetData", data, connectionId);

        public async Task SendReceivedTextUpdate(ReceivedTextData receivedTextData, string connectionId = null)
        {
            await SendAsync("ReceivedTextData", receivedTextData, connectionId);
        }

        public Task SendStationUpdate(StationData stationData, string connectionId = null) => SendAsync("StationData", stationData, connectionId);

        public Task SendExplorationUpdate(ExplorationData explorationData, string connectionId = null) => SendAsync("ExplorationData", explorationData, connectionId);

        public Task SendLoadoutUpdate(LoadoutData loadoutData, string connectionId = null) => SendAsync("LoadoutData", loadoutData, connectionId);

        public Task SendMissionUpdate(MissionData missionData, string connectionId = null) => SendAsync("MissionData", missionData, connectionId);

        public Task SendMissionCollectionUpdate(MissionCollectionData data, string connectionId = null) => SendAsync("MissionCollectionData", data, connectionId);

        public Task SendDockingUpdate(DockingData data, string connectionId = null) => SendAsync("DockingData", data, connectionId);

        public Task SendMissionLifecycleUpdate(MissionLifecycleData data, string connectionId = null) => SendAsync("MissionLifecycleData", data, connectionId);
        public Task SendCargoUpdate(CargoData data, string connectionId = null) => SendAsync("CargoData", data, connectionId);
        public Task SendMaterialsUpdate(MaterialsData data, string connectionId = null) => SendAsync("MaterialsData", data, connectionId);
        public Task SendCombatUpdate(CombatData data, string connectionId = null) => SendAsync("CombatData", data, connectionId);
        public Task SendSystemUpdate(SystemData data, string connectionId = null) => SendAsync("SystemData", data, connectionId);

        private Task SendAsync<T>(string eventType, T data, string connectionId = null)
        {
            var envelope = new PanelDataEnvelope<T> {
                EventType = eventType,
                Sequence = Interlocked.Increment(ref sequence),
                Timestamp = DateTime.UtcNow,
                Data = data
            };
            var clients = connectionId == null ? _hubContext.Clients.All : _hubContext.Clients.Client(connectionId);
            return clients.SendAsync(eventType, envelope);
        }
    }
}
