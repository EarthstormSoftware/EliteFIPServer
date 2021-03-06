using EliteFIPProtocol;
using EliteFIPServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace EliteFIPServer
{
    class GameDataUpdateController 
    {

        private readonly IHubContext<GameDataUpdateHub> _hubContext;

        public GameDataUpdateController(IHubContext<GameDataUpdateHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public void SendStatusUpdate(StatusData statusData)
        {
            string statusJSON = JsonSerializer.Serialize(statusData);
            _hubContext.Clients.All.SendAsync("StatusData", statusJSON);
        }

        public void SendTargetUpdate(ShipTargetedData targetData)
        {
            string targetJSON = JsonSerializer.Serialize(targetData);
            _hubContext.Clients.All.SendAsync("TargetData", targetJSON);
        }
    }
}
