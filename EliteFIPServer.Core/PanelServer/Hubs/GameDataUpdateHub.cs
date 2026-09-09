using Microsoft.AspNetCore.SignalR;

namespace EliteFIPServer
{
    public class GameDataUpdateHub : Hub
    {
        public static event EventHandler<string> ClientConnected;
        public static event EventHandler<string> ClientDisconnected;

        public override async Task OnConnectedAsync()
        {
            ClientConnected?.Invoke(this, Context.ConnectionId);
            await base.OnConnectedAsync();
            await ClientConnect.RequestDataUpdate(Context.ConnectionId);
        }

        public DateTime Ping()
        {
            return DateTime.UtcNow;
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            ClientDisconnected?.Invoke(this, Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
