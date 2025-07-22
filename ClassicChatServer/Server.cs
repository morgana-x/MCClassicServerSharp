using System.Net.Sockets;
using System.Web;

namespace ClassicChatServer
{
    public class Server
    {
        static TcpListener listener;
        public static Dictionary<int, Player> Players = new Dictionary<int, Player>();
        public static List<Player> PlayerList { get { return Players.Values.ToList(); } }

        public static bool ServerRunning = false;

        public static int MaxMessageLength = 256;

        public static bool Public = false;
        public static bool Verify = true;

        public static string Name = "Terrible Server";
        public static string Motd = "This is a terrible MOTD.";
        public static string Software = "&cBad Server Software";
        public static byte[] Salt = Util.Salt();

        public static int Port = 25565;
        public static int MaxPlayers = 256;

        public static List<string> BannedUsernames = new List<string>();

        static HttpClient HeartbeatClient = new HttpClient() { BaseAddress = new Uri("https://www.classicube.net/server/heartbeat/") };

        public static Level Level = new Level("main", 256, 256 ,256);

        public static Dictionary<string, int> SupportedCPE = new Dictionary<string, int>()
        {
            ["LongerMessages"] = 1,
            ["FullCP437"] = 1,
            ["CustomBlocks"] = 1,
            ["HeldBlock"] = 1,
            ["ChangeModel"] = 1,
        };

        public static int GetFreeId()
        {
            int freeid = 0;
            while (Players.ContainsKey(freeid)) { freeid++; }
            return freeid;
        }

        static DateTime nextPosUpdate = DateTime.Now;
        static DateTime nextHeartbeat = DateTime.Now;
        static DateTime nextPing = DateTime.Now;
        public static void Init(int port = 25565)
        {
            listener = new TcpListener(System.Net.IPAddress.Any, port); listener.Start();
            ServerRunning = true;
            Port = port;

            var task = Task.Run(Connections);
            var pingtask = Task.Run(() => { 
                while (ServerRunning) 
                { 
                    Thread.Sleep(25);
                    if (DateTime.Now > nextPosUpdate)
                    {
                        Level.Tick();
                        nextPosUpdate = DateTime.Now.AddMilliseconds(25);
                    }
                    if (DateTime.Now > nextHeartbeat)
                    {
                        Heartbeat();
                        nextHeartbeat = DateTime.Now.AddSeconds(5);
                    }
                    if (DateTime.Now > nextPing)
                    {
                        foreach (var pl in PlayerList)
                            pl.SendBytes(new byte[] { 0x01 });
                        nextPing = DateTime.Now.AddSeconds(1);
                    }
 

              
                } 
            });
        }

        static async Task Connections()
        {
            while (ServerRunning)
            {
                var client = listener.AcceptTcpClient();
                if (!Packet.TryReadPacket(client.GetStream(), out var id, out var packet))
                {
                    client.Close();
                    continue;
                };
                if (id != 0) continue;
                Packet.Packets[id].Processor(new Player(client), packet);
            }
        }
        static void Heartbeat()
        {
            var encodedargs = $"?name={HttpUtility.UrlEncode(Name)}&port={Port}&users={PlayerList.Count}&max={MaxPlayers}&salt={HttpUtility.UrlEncode(Convert.ToHexString(Salt))}&web={false}&public={Public}&software={HttpUtility.UrlEncode(Software)}";
           // Console.WriteLine(encodedargs);
            HeartbeatClient.GetAsync(encodedargs);
        }
        public static void Broadcast(string message)
        {
            Console.WriteLine(message);
            foreach (var pl in PlayerList)
                pl.SendMessage(message);
        }

        public static void PlayerConnect(Player player)
        {
            player.SendBytes(Packet.ServerIdentifyPacket(Name, Motd));

            player.Id = (byte)GetFreeId();
            Players.Add(player.Id, player);

            Level.AddPlayer(player);
            Broadcast($"{player.ColouredName}&e has &aconnected!");
        }

        public static void PlayerDisconnect(Player player)
        {
            if (player == null) return;
            if (!Players.ContainsKey(player.Id)) return;
            Players.Remove(player.Id);
            if (player.Level != null)
                player.Level.RemovePlayer(player);
            Broadcast($"{player.ColouredName}&e has &cdisconnected!");
        }

        public static void PlayerMessage(Player player, string message)
        {
            if (message.StartsWith('/'))
            {
                Command.ExecuteCommand(player, message.Substring(1));
                return;
            }
            Broadcast($"{player.ColouredName}: &f{message.Replace("%", "&")}");
        }

        public static void SetLevel(Level lvl)
        {
            foreach(var p in Level.PlayerList)
            {
                foreach (var pp in Level.PlayerList)
                    p.SendBytes(Packet.PlayerDespawn(pp.Id));
            }
            Level = lvl;
            foreach (var p in Level.PlayerList)
                Level.AddPlayer(p);
        }

        public static void Shutdown()
        {
            if (!ServerRunning) return;
            foreach (var p in PlayerList)
                p.Kick("Server Shutting Down!!!");
            Level.Save();
            ServerRunning = false;
        }
    }
}
