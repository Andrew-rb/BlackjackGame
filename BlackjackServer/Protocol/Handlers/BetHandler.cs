using BlackjackServer.Models;
using BlackjackServer.Networking;
using BlackjackServer.Rooms;

namespace BlackjackServer.Protocol.Handlers
{
    public class BetHandler
    {
        public void Handle(Player player, string[] args, WebSocketConnection ws)
        {
            if (player == null) { ws.SendMessage("ERROR|Сначала выполните JOIN"); return; }
            if (args.Length < 1) { ws.SendMessage("ERROR|Сумма ставки: BET|число"); return; }
            if (!int.TryParse(args[0], out int amount)) { ws.SendMessage("ERROR|Неверное число"); return; }

            var room = RoomManager.GetPlayerRoom(player.Id);
            if (room == null) { ws.SendMessage("ERROR|Вы не в комнате"); return; }
            room.PlaceBet(player, amount);
        }
    }
}