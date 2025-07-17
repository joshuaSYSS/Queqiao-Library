using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Queqiao
{
    public class MatchmakingServer
    {
        private TcpListener _listener;
        private readonly List<TcpClient> _waitingClients = new List<TcpClient>();
        private readonly object _lock = new object();
        private bool _isRunning;
        private int _groupSize = 2;
        private Thread _matchmakingThread;

        public int GroupSize
        {
            get => _groupSize;
            set
            {
                if (value < 2) throw new ArgumentException("Group size must be at least 2");
                _groupSize = value;
            }
        }

        public void Start(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _isRunning = true;

            Console.WriteLine($"Server started on port {port}. Waiting for clients...");

            // Start matchmaking thread
            _matchmakingThread = new Thread(MatchmakingLoop);
            _matchmakingThread.Start();

            // Client acceptance loop
            while (_isRunning)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    new Thread(() => HandleClient(client)).Start();
                }
                catch (SocketException)
                {
                    // Listener stopped
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener.Stop();
            lock (_lock)
            {
                foreach (var client in _waitingClients)
                {
                    client.Close();
                }
                _waitingClients.Clear();
            }
        }

        private void HandleClient(TcpClient client)
        {
            string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string command = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);

                if (command == "JOIN")
                {
                    lock (_lock)
                    {
                        _waitingClients.Add(client);
                    }
                    Console.WriteLine($"Client {clientIp} joined queue");
                    SendResponse(stream, "ADDED");
                }
                else if (command == "LEAVE")
                {
                    lock (_lock)
                    {
                        if (_waitingClients.Remove(client))
                        {
                            Console.WriteLine($"Client {clientIp} left queue");
                            SendResponse(stream, "REMOVED");
                        }
                    }
                }
            }
            catch
            {
                // Handle client disconnect
                lock (_lock) { _waitingClients.Remove(client); }
                client.Close();
            }
        }

        private void MatchmakingLoop()
        {
            while (_isRunning)
            {
                lock (_lock)
                {
                    if (_waitingClients.Count >= _groupSize)
                    {
                        // Create a new group
                        var group = new List<TcpClient>();
                        for (int i = 0; i < _groupSize; i++)
                        {
                            group.Add(_waitingClients[0]);
                            _waitingClients.RemoveAt(0);
                        }

                        // Notify group members
                        List<string> ipAddresses = new List<string>();
                        foreach (var client in group)
                        {
                            string ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                            ipAddresses.Add(ip);
                        }
                        string groupIps = string.Join(",", ipAddresses);

                        foreach (var client in group)
                        {
                            try
                            {
                                SendResponse(client.GetStream(), $"MATCHED:{groupIps}");
                                Console.WriteLine($"Matched client: {((IPEndPoint)client.Client.RemoteEndPoint).Address}");
                            }
                            finally
                            {
                                client.Close();
                            }
                        }
                    }
                }
                Thread.Sleep(1000);
            }
        }

        private void SendResponse(NetworkStream stream, string message)
        {
            byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
            stream.Write(data, 0, data.Length);
        }
    }
}
