using BlackjackServer.Models;
using BlackjackServer.Networking;
using BlackjackServer.Protocol.Handlers;

namespace BlackjackServer.Protocol
{
    public class CommandRouter
    {
        private Dictionary<string, Action<Player, string[], WebSocketConnection>> handlers = new Dictionary<string, Action<Player, string[], WebSocketConnection>>();

        public CommandRouter()
        {
            handlers["JOIN"] = (p, args, ws) => new JoinHandler().Handle(p, args, ws);
            handlers["BET"] = (p, args, ws) => new BetHandler().Handle(p, args, ws);
            handlers["HIT"] = (p, args, ws) => new HitHandler().Handle(p, args, ws);
            handlers["STAND"] = (p, args, ws) => new StandHandler().Handle(p, args, ws);
            handlers["DOUBLE"] = (p, args, ws) => new DoubleHandler().Handle(p, args, ws);
            handlers["SPLIT"] = (p, args, ws) => new SplitHandler().Handle(p, args, ws);
            handlers["CHAT"] = (p, args, ws) => new ChatHandler().Handle(p, args, ws);
            handlers["START"] = (p, args, ws) => new StartHandler().Handle(p, args, ws);
            handlers["LEAVE"] = (p, args, ws) => new LeaveHandler().Handle(p, args, ws);
        }

        public void HandleCommand(Player player, string rawMessage, WebSocketConnection ws)
        {
            var (cmd, args) = CommandParser.Parse(rawMessage);
            if (handlers.ContainsKey(cmd))
                handlers[cmd](player, args, ws);
            else
                ws.SendMessage($"ERROR|Неизвестная команда: {cmd}");
        }
    }
}