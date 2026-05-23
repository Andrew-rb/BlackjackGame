namespace BlackjackServer.Protocol
{
    public static class CommandParser
    {
        public static (string command, string[] args) Parse(string rawMessage)
        {
            var parts = rawMessage.Split('|');
            if (parts.Length == 0) return ("", null);
            string cmd = parts[0].ToUpper();
            string[] args = parts.Length > 1 ? parts[1..] : new string[0];
            return (cmd, args);
        }
    }
}