using ClassicChatServer;
public partial class Program
{
    public static void Main(string[] args)
    {
        Packet.Init();
        Rank.Load();
        Server.Init(25565);
        Server.Level = Level.Load("main");

        while (Server.ServerRunning)
        {
            var cmd = Console.ReadLine();
            if (cmd != null)
                Server.PlayerMessage(Player.Console, cmd);
        }
        Server.Shutdown();
    }
}