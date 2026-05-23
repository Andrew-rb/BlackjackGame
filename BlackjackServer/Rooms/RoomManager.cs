using BlackjackServer.Models;

namespace BlackjackServer.Rooms
{
    public static class RoomManager
    {
        private static List<GameRoom> rooms = new List<GameRoom>();
        private static Dictionary<string, GameRoom> playerToRoom = new Dictionary<string, GameRoom>();
        private static Dictionary<string, Player> players = new Dictionary<string, Player>();
        private static readonly object lockObj = new object();

        public static void Init()
        {
            CreateRoom("Main");
        }

        public static GameRoom CreateRoom(string id)
        {
            lock (lockObj)
            {
                var room = new GameRoom(id);
                rooms.Add(room);
                Logger.Log($"Создана комната {id}");
                return room;
            }
        }

        public static void AddPlayer(Player player)
        {
            lock (lockObj)
            {
                players[player.Id] = player;
                var targetRoom = rooms.OrderBy(r => r.Players.Count).FirstOrDefault();
                if (targetRoom == null) targetRoom = CreateRoom("Room_" + rooms.Count);
                targetRoom.AddPlayer(player);
                playerToRoom[player.Id] = targetRoom;
            }
        }

        public static void RemovePlayer(string playerId)
        {
            lock (lockObj)
            {
                if (playerToRoom.TryGetValue(playerId, out var room))
                {
                    room.RemovePlayer(playerId);
                    playerToRoom.Remove(playerId);
                }
                players.Remove(playerId);
            }
        }

        public static GameRoom GetPlayerRoom(string playerId)
        {
            playerToRoom.TryGetValue(playerId, out var room);
            return room;
        }

        public static Player GetPlayer(string playerId)
        {
            players.TryGetValue(playerId, out var p);
            return p;
        }

        public static bool RoomExists(string id) => rooms.Any(r => r.Id == id);
        public static void RemoveRoom(string id)
        {
            var room = rooms.FirstOrDefault(r => r.Id == id);
            if (room != null) rooms.Remove(room);
        }
    }
}