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
        private TcpListener listener;
        private readonly object queueLock = new object();
        private List<TcpClient> matchmakingQueue = new List<TcpClient>();
        private int groupSize = 2;
        private bool isRunning = false;
        private Thread acceptThread;
        private Thread groupCheckThread;

        public int GroupSize
        {
            get => groupSize;
            set => groupSize = Math.Max(2, value);
        }

        public event Action<string> LogMessage;
        public event Action<List<string>> GroupFormed;

        public MatchmakingServer(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
        }

        public void Start()
        {
            if (isRunning) return;
            isRunning = true;

            listener.Start();
            LogMessage?.Invoke($"Server started on port {((IPEndPoint)listener.LocalEndpoint).Port}...");

            acceptThread = new Thread(AcceptClients);
            acceptThread.Start();

            groupCheckThread = new Thread(CheckGroups);
            groupCheckThread.Start();
        }

        public void Stop()
        {
            isRunning = false;
            listener.Stop();

            // Close all queued clients
            lock (queueLock)
            {
                foreach (var client in matchmakingQueue)
                {
                    try { client.Close(); } catch { }
                }
                matchmakingQueue.Clear();
            }

            acceptThread?.Join();
            groupCheckThread?.Join();
        }

        private void AcceptClients()
        {
            while (isRunning)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    LogMessage?.Invoke($"Client connected: {client.Client.RemoteEndPoint}");
                    new Thread(() => HandleClient(client)).Start();
                }
                catch (SocketException)
                {
                    // Expected when stopping
                }
                catch (Exception ex)
                {
                    if (isRunning) LogMessage?.Invoke($"Accept error: {ex.Message}");
                }
            }
        }

        private void HandleClient(TcpClient client)
        {
            string clientId = client.Client.RemoteEndPoint.ToString();
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) return;

                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                if (message == "JOIN")
                {
                    lock (queueLock)
                    {
                        matchmakingQueue.Add(client);
                    }
                    LogMessage?.Invoke($"Added to queue: {clientId}");
                    SendResponse(stream, "ADDED_TO_QUEUE");
                }
                else if (message == "CANCEL")
                {
                    lock (queueLock)
                    {
                        if (matchmakingQueue.Remove(client))
                        {
                            LogMessage?.Invoke($"Cancelled matchmaking: {clientId}");
                            SendResponse(stream, "CANCELLED");
                        }
                    }
                    client.Close();
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Client error ({clientId}): {ex.Message}");
                lock (queueLock) matchmakingQueue.Remove(client);
                client.Close();
            }
        }

        private void CheckGroups()
        {
            while (isRunning)
            {
                lock (queueLock)
                {
                    while (matchmakingQueue.Count >= groupSize)
                    {
                        List<TcpClient> group = new List<TcpClient>();
                        List<string> groupEndpoints = new List<string>();

                        for (int i = 0; i < groupSize; i++)
                        {
                            TcpClient client = matchmakingQueue[0];
                            group.Add(client);
                            groupEndpoints.Add(client.Client.RemoteEndPoint.ToString());
                            matchmakingQueue.RemoveAt(0);
                        }

                        // Notify about the group
                        GroupFormed?.Invoke(groupEndpoints);

                        // Notify each client in the group and close their connections
                        foreach (TcpClient client in group)
                        {
                            try
                            {
                                string groupInfo = string.Join(",", groupEndpoints);
                                SendResponse(client.GetStream(), $"MATCHED {groupInfo}");
                                client.Close();
                            }
                            catch (Exception ex)
                            {
                                LogMessage?.Invoke($"Error notifying client: {ex.Message}");
                            }
                        }
                    }
                }
                Thread.Sleep(1000);
            }
        }

        private void SendResponse(NetworkStream stream, string message)
        {
            try
            {
                byte[] data = Encoding.ASCII.GetBytes(message);
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Send response error: {ex.Message}");
            }
        }
    }
}
