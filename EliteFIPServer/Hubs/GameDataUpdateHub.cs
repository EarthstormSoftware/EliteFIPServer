using Microsoft.AspNetCore.SignalR;
using EliteFIPProtocol;
using System.Text.Json;

namespace EliteFIPServer.Hubs
{
    public class GameDataUpdateHub : Hub
    {
        //public async Task SendStatusUpdate(StatusData statusData)
        //{
        //    string statusJSON = JsonSerializer.Serialize(statusData);
        //    await Clients.All.SendAsync("StatusData", statusJSON);
        //}

        //public async Task SendTargetUpdate(ShipTargetedData targetData)
        //{
        //    string targetJSON = JsonSerializer.Serialize(targetData);
        //    await Clients.All.SendAsync("TargetData", targetJSON);
        //}
    }
}
