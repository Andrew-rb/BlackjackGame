using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BlackjackServer.Networking
{
    public class WebSocketConnection
    {
        private Socket socket;
        private byte[] receiveBuffer = new byte[4096];
        public string PlayerId { get; set; }
        public IPEndPoint RemoteEndPoint { get; private set; }
        private bool isDisposed = false;

        public WebSocketConnection(Socket socket)
        {
            this.socket = socket;
            this.RemoteEndPoint = socket.RemoteEndPoint as IPEndPoint;
        }

        private bool IsConnected()
        {
            return socket != null && socket.Connected && !isDisposed;
        }

        public async Task<bool> PerformHandshake()
        {
            try
            {
                if (!IsConnected()) return false;
                int received = await ReceiveAsync(receiveBuffer);
                string request = Encoding.UTF8.GetString(receiveBuffer, 0, received);
                if (!request.Contains("Sec-WebSocket-Key:"))
                    return false;

                string key = ExtractKey(request);
                string accept = ComputeAccept(key);
                string response = "HTTP/1.1 101 Switching Protocols\r\n" +
                                  "Upgrade: websocket\r\n" +
                                  "Connection: Upgrade\r\n" +
                                  $"Sec-WebSocket-Accept: {accept}\r\n\r\n";
                await SendAsync(Encoding.UTF8.GetBytes(response));
                Logger.Log($"WebSocket handshake успешен для {socket.RemoteEndPoint}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Handshake ошибка", ex);
                return false;
            }
        }

        public async Task<string> ReceiveMessage()
        {
            try
            {
                if (!IsConnected()) return null;
                int read = await ReceiveAsync(receiveBuffer);
                if (read == 0) return null;

                byte[] frame = new byte[read];
                Array.Copy(receiveBuffer, frame, read);
                bool masked = (frame[1] & 0x80) != 0;
                int payloadLen = frame[1] & 0x7F;
                int offset = 2;
                if (payloadLen == 126)
                {
                    payloadLen = (frame[2] << 8) | frame[3];
                    offset = 4;
                }
                else if (payloadLen == 127)
                    throw new NotSupportedException("Large frames not supported");

                byte[] mask = new byte[4];
                if (masked)
                {
                    Array.Copy(frame, offset, mask, 0, 4);
                    offset += 4;
                }
                byte[] payload = new byte[payloadLen];
                Array.Copy(frame, offset, payload, 0, payloadLen);
                if (masked)
                {
                    for (int i = 0; i < payloadLen; i++)
                        payload[i] ^= mask[i % 4];
                }
                string result = Encoding.UTF8.GetString(payload);
                if (string.IsNullOrWhiteSpace(result) && result.Length == 0)
                    return null;
                return result;
            }
            catch (ObjectDisposedException)
            {
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка приёма сообщения", ex);
                return null;
            }
        }

        public async Task SendMessage(string message)
        {
            if (!IsConnected()) return;
            try
            {
                byte[] payload = Encoding.UTF8.GetBytes(message);
                byte[] frame = BuildFrame(payload);
                await SendAsync(frame);
            }
            catch (ObjectDisposedException)
            {
                
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка отправки сообщения", ex);
            }
        }

        private byte[] BuildFrame(byte[] payload)
        {
            int len = payload.Length;
            List<byte> frame = new List<byte>();
            frame.Add(0x81); 
            if (len <= 125)
            {
                frame.Add((byte)len);
                frame.AddRange(payload);
            }
            else if (len <= 65535)
            {
                frame.Add(126);
                frame.Add((byte)(len >> 8));
                frame.Add((byte)(len & 0xFF));
                frame.AddRange(payload);
            }
            else throw new NotSupportedException();
            return frame.ToArray();
        }

        private Task<int> ReceiveAsync(byte[] buffer)
        {
            return Task.Run(() =>
            {
                try
                {
                    return socket.Receive(buffer);
                }
                catch (ObjectDisposedException)
                {
                    return 0;
                }
            });
        }

        private Task SendAsync(byte[] data)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (IsConnected())
                        socket.Send(data);
                }
                catch (ObjectDisposedException) { }
            });
        }

        private string ExtractKey(string request)
        {
            var lines = request.Split(new[] { "\r\n" }, StringSplitOptions.None);
            foreach (var l in lines)
                if (l.StartsWith("Sec-WebSocket-Key:"))
                    return l.Substring("Sec-WebSocket-Key:".Length).Trim();
            return null;
        }

        private string ComputeAccept(string key)
        {
            const string magic = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            string combined = key + magic;
            byte[] sha = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(combined));
            return Convert.ToBase64String(sha);
        }

        public void Dispose()
        {
            isDisposed = true;
            try
            {
                socket?.Close();
                socket?.Dispose();
            }
            catch { }
        }
    }
}