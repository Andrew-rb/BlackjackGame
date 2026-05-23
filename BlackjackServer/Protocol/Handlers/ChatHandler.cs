using BlackjackServer.Models;
using BlackjackServer.Networking;
using BlackjackServer.Rooms;

namespace BlackjackServer.Protocol.Handlers
{
    public class ChatHandler
    {
        public void Handle(Player player, string[] args, WebSocketConnection ws)
        {
            if (player == null || args.Length == 0) return;
            string msg = string.Join("|", args);
            var room = RoomManager.GetPlayerRoom(player.Id);
            room?.SendChat(player, msg);
        }
    }
}