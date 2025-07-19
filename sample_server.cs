using System;
using MatchmakingSystem;

class SampleServer
{
    static void Main()
    {
        var server = new MatchmakingServer();
        server.GroupSize = 3; // Groups of 3 players
        server.Start(5000);
        Console.WriteLine("Server running. Press ENTER to stop.");
        Console.ReadLine();
        server.Stop();
    }
}
