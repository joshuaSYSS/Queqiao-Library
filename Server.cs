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
    public class Server
    {
        TcpListener server = null;
        Int32 port;
        IPAddress localAddr = IPAddress.Parse("127.0.0.1");
        bool startedServer = false;

        // Buffer for reading data
        Byte[] bytes = new Byte[256];
        String data = null;

        List<String> matchmakingQueue = new List<String>();
        List<List<String>> matchmakedGroups = new List<List<String>>();

        public void StartServer(Int32 p)
        {
            if(startedServer) return;
            port = p;
            server = new TcpListener(localAddr, port);
            server.Start();
        }

        public void StopServer()
        {
            if(!startedServer) return;
            startedServer = false;
            server.Stop();
            matchmakingQueue.Clear();
            matchmakedGroups.Clear();
        }

        void ReceiveConnection()
        {
            while (true)
            {
                Console.Write("Waiting for a connection... ");

                // Perform a blocking call to accept requests.
                // You could also use server.AcceptSocket() here.
                using TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Connected!");

                data = null;

                // Get a stream object for reading and writing
                NetworkStream stream = client.GetStream();

                int i;

                // Loop to receive all the data sent by the client.
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    // Translate data bytes to a ASCII string.
                    data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                    Console.WriteLine("Received: {0}", data);

                    // Process the data sent by the client.
                    data = data.ToUpper();

                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                    // Send back a response.
                    stream.Write(msg, 0, msg.Length);
                    Console.WriteLine("Sent: {0}", data);
                }
            } 
        }
    }
}
