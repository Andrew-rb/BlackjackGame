using BlackjackServer.Models;
using BlackjackServer.Networking;
using BlackjackServer.Rooms;

namespace BlackjackServer.Protocol.Handlers
{
    public class JoinHandler
    {
        public void Handle(Player player, string[] args, WebSocketConnection ws)
        {
            if (args.Length < 1)
            {
                ws.SendMessage("ERROR|Укажите имя: JOIN|Имя");
                return;
            }
            string name = args[0];
            string playerId = Guid.NewGuid().ToString();

            string ipAddress = ws.RemoteEndPoint?.Address?.ToString() ?? "unknown";

            player = new Player(playerId, name, ipAddress);

            TcpServer.RegisterPlayerConnection(playerId, ws);
            RoomManager.AddPlayer(player);

            ws.SendMessage($"JOIN_OK|{playerId}|{player.Name}");
            ws.SendMessage($"NEW_BALANCE|{player.Balance}");
            Logger.Log($"Игрок {name} присоединился (ID {playerId}, IP: {ipAddress})");
        }
    }
}