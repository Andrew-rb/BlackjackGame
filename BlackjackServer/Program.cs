using BlackjackServer.Networking;
using BlackjackServer.Rooms;

namespace BlackjackServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.Init("server.log");
            Logger.Log("Запуск Blackjack сервера...");

            RoomManager.Init();

            string serverIP = "0.0.0.0";
            int serverPort = 8888;

            TcpServer server = new TcpServer(serverIP, serverPort);
            server.Start();

            Logger.Log($"Сервер запущен на {serverIP}:{serverPort}");
            Logger.Log("Сервер остановлен. Нажмите Enter для выхода.");
            Console.ReadLine();
        }
    }
}