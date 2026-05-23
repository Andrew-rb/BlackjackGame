using BlackjackServer.Models;
using BlackjackServer.Networking;
using BlackjackServer.Rooms;

namespace BlackjackServer.Protocol.Handlers
{
    public class HitHandler
    {
        public void Handle(Player player, string[] args, WebSocketConnection ws)
        {
            if (player == null) { ws.SendMessage("ERROR|Сначала JOIN"); return; }
            var room = RoomManager.GetPlayerRoom(player.Id);
            if (room == null) return;
            room.PlayerHit(player);
        }
    }
}