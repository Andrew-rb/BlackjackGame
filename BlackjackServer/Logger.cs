namespace BlackjackServer
{
    public static class Logger
    {
        private static string logFile;
        private static readonly object lockObj = new object();

        public static void Init(string filePath)
        {
            logFile = filePath;
            File.WriteAllText(logFile, $"=== Лог сервера Blackjack {DateTime.Now} ===\n");
        }

        public static void Log(string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            Console.WriteLine(line);
            lock (lockObj)
            {
                File.AppendAllText(logFile, line + "\n");
            }
        }

        public static void Error(string message, Exception ex = null)
        {
            string errMsg = $"[ОШИБКА] {message}";
            if (ex != null)
                errMsg += $": {ex.Message}";
            Log(errMsg);
        }

        public static void Debug(string message)
        {
            string line = $"[DEBUG] {message}";
            lock (lockObj)
            {
                File.AppendAllText(logFile, line + "\n");
            }
        }
    }
}