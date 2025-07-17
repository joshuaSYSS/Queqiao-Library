using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Queqiao
{
    public class MatchmakingClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private bool _isMatched;
        private readonly object _lock = new object();

        public event Action<string[]> OnMatched;

        public void Connect(string ip, int port)
        {
            _client = new TcpClient();
            _client.Connect(ip, port);
            _stream = _client.GetStream();
        }

        public void JoinQueue()
        {
            if (_isMatched) return;

            SendCommand("JOIN");
            Console.WriteLine("Joined matchmaking queue");

            // Start listening for server responses
            new Thread(ListenForResponses).Start();
        }

        public void CancelMatchmaking()
        {
            lock (_lock)
            {
                if (_isMatched || _client == null) return;

                SendCommand("LEAVE");
                _client.Close();
                Console.WriteLine("Cancelled matchmaking");
            }
        }

        private void SendCommand(string command)
        {
            byte[] data = System.Text.Encoding.ASCII.GetBytes(command);
            _stream.Write(data, 0, data.Length);
        }

        private void ListenForResponses()
        {
            try
            {
                byte[] buffer = new byte[1024];
                while (true)
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    string response = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    if (response.StartsWith("MATCHED:"))
                    {
                        lock (_lock)
                        {
                            _isMatched = true;
                            string[] ips = response.Substring(8).Split(',');
                            OnMatched?.Invoke(ips);
                        }
                        break;
                    }
                }
            }
            catch
            {
                // Connection closed
            }
        }
    }
}
