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
        private TcpClient client;
        private NetworkStream stream;
        private Thread listenThread;
        private bool isMatchmaking = false;

        public event Action<string> LogMessage;
        public event Action<string[]> Matched; // Group endpoints

        public void Connect(string ip, int port)
        {
            client = new TcpClient();
            client.Connect(ip, port);
            stream = client.GetStream();

            listenThread = new Thread(ListenForResponses);
            listenThread.Start();

            LogMessage?.Invoke($"Connected to server at {ip}:{port}");
        }

        public void Disconnect()
        {
            if (isMatchmaking)
                CancelMatchmaking();

            client?.Close();
            listenThread?.Join();
            LogMessage?.Invoke("Disconnected");
        }

        public void JoinMatchmaking()
        {
            if (isMatchmaking) return;
            SendMessage("JOIN");
            isMatchmaking = true;
            LogMessage?.Invoke("Joined matchmaking queue");
        }

        public void CancelMatchmaking()
        {
            if (!isMatchmaking) return;
            SendMessage("CANCEL");
            isMatchmaking = false;
        }

        private void SendMessage(string message)
        {
            try
            {
                byte[] data = Encoding.ASCII.GetBytes(message);
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Send error: {ex.Message}");
            }
        }

        private void ListenForResponses()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (true)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    HandleServerResponse(response);
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Connection error: {ex.Message}");
            }
            finally
            {
                isMatchmaking = false;
            }
        }

        private void HandleServerResponse(string response)
        {
            LogMessage?.Invoke($"Server: {response}");

            if (response.StartsWith("MATCHED"))
            {
                isMatchmaking = false;
                // Format: "MATCHED endpoint1,endpoint2,..."
                string[] parts = response.Split(new char[] { ' ' }, 2);
                if (parts.Length == 2)
                {
                    string[] endpoints = parts[1].Split(',');
                    Matched?.Invoke(endpoints);
                }
            }
            else if (response == "CANCELLED")
            {
                isMatchmaking = false;
            }
        }
    }
}
