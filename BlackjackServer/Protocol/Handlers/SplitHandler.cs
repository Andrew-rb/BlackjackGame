using BlackjackServer.Models;
using BlackjackServer.Networking;
using BlackjackServer.Rooms;

namespace BlackjackServer.Protocol.Handlers
{
    public class SplitHandler
    {
        public void Handle(Player player, string[] args, WebSocketConnection ws)
        {
            if (player == null) return;
            var room = RoomManager.GetPlayerRoom(player.Id);
            room?.PlayerSplit(player);
        }
    }
}