using ClassicChatServer;
public partial class Program
{
    public static void Main(string[] args)
    {
        Packet.Init();
        Server.Init(25565);
        Server.Level.Load();
        while (Server.ServerRunning)
        {
            var cmd = Console.ReadLine();
            if (cmd != null)
                Server.PlayerMessage(Player.Console, cmd);
        }
        Server.Shutdown();
    }
}