using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using BlackjackServer.Protocol;
using BlackjackServer.Models;
using BlackjackServer.Rooms;

namespace BlackjackServer.Networking
{
    public class TcpServer
    {
        private Socket listenSocket;
        private string ip;
        private int port;
        private static ConcurrentDictionary<string, WebSocketConnection> clients = new ConcurrentDictionary<string, WebSocketConnection>();
        private static CommandRouter router;

        public TcpServer(string address, int port)
        {
            this.ip = address;
            this.port = port;
            router = new CommandRouter();
        }

        public void Start()
        {
            IPAddress ipAddr = IPAddress.Any;
            if (ip != "0.0.0.0")
                ipAddr = IPAddress.Parse(ip);
            listenSocket = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(new IPEndPoint(ipAddr, port));
            listenSocket.Listen(100);
            Logger.Log($"Сервер запущен на {ipAddr}:{port}");
            AcceptClients();
        }

        private async void AcceptClients()
        {
            while (true)
            {
                try
                {
                    Socket clientSocket = await Task.Factory.FromAsync(
                        listenSocket.BeginAccept, listenSocket.EndAccept, null);
                    _ = HandleClient(clientSocket);
                }
                catch (Exception ex)
                {
                    Logger.Error("Ошибка принятия клиента", ex);
                }
            }
        }

        private async Task HandleClient(Socket socket)
        {
            var ws = new WebSocketConnection(socket);
            bool handshakeOk = await ws.PerformHandshake();
            if (!handshakeOk)
            {
                socket.Close();
                return;
            }

            string clientIP = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
            Logger.Log($"Новый WebSocket клиент: {socket.RemoteEndPoint}");

            string tempId = Guid.NewGuid().ToString();
            clients[tempId] = ws;

            while (true)
            {
                string msg = await ws.ReceiveMessage();
                if (msg == null) break;
                Logger.Debug($"Получено от {clientIP}: {msg}");

                Player player = null;
                if (ws.PlayerId != null && RoomManager.GetPlayer(ws.PlayerId) != null)
                    player = RoomManager.GetPlayer(ws.PlayerId);

                router.HandleCommand(player, msg, ws);
            }

            if (ws.PlayerId != null)
            {
                var p = RoomManager.GetPlayer(ws.PlayerId);
                if (p != null) RoomManager.RemovePlayer(p.Id);
            }
            clients.TryRemove(tempId, out _);
            ws.Dispose();
            socket.Close();
            Logger.Log($"Клиент отключён: {clientIP}");
        }

        public static void SendToPlayer(string playerId, string message)
        {
            if (clients.TryGetValue(playerId, out var ws))
            {
                _ = ws.SendMessage(message);
                Logger.Debug($"Отправлено {playerId}: {message}");
            }
        }

        public static void RegisterPlayerConnection(string playerId, WebSocketConnection ws)
        {
            ws.PlayerId = playerId;
            var tempEntry = clients.FirstOrDefault(kvp => kvp.Value == ws);
            if (tempEntry.Key != null)
                clients.TryRemove(tempEntry.Key, out _);
            clients[playerId] = ws;
        }
    }
}