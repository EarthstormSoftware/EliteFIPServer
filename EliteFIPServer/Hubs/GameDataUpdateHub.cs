using Microsoft.AspNetCore.SignalR;

namespace EliteFIPServer.Hubs
{
    public class GameDataUpdateHub : Hub
    {
        public override async Task OnConnectedAsync() {
            //await Groups.AddToGroupAsync(Context.ConnectionId, "SignalR Users");
            await base.OnConnectedAsync();
        }
    }
}
