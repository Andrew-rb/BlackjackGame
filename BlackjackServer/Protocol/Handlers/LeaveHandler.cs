using BlackjackServer.Models;
using BlackjackServer.Networking;
using BlackjackServer.Rooms;

namespace BlackjackServer.Protocol.Handlers
{
    public class LeaveHandler
    {
        public void Handle(Player player, string[] args, WebSocketConnection ws)
        {
            if (player == null) return;
            RoomManager.RemovePlayer(player.Id);
            ws.SendMessage("LEAVE_OK");
        }
    }
}