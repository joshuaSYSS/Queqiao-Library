using System;
using System.Threading;
using Queqiao;

class SampleClient
{
    static void Main()
    {
        var client = new MatchmakingClient();
        client.OnMatched += ips => 
            Console.WriteLine($"MATCH FOUND! Group members: {string.Join(", ", ips)}");
        
        client.Connect("127.0.0.1", 5000);
        client.JoinQueue();

        Console.WriteLine("Press 'C' to cancel matchmaking");
        while (true)
        {
            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.C)
            {
                client.CancelMatchmaking();
                break;
            }
            Thread.Sleep(100);
        }
        
        Console.WriteLine("Press any key to exit");
        Console.ReadKey();
    }
}
