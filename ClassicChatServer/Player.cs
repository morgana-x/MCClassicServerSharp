using System.Net.Sockets;

namespace ClassicChatServer
{
    public class Player
    {
        public TcpClient TCPClient;
        NetworkStream stream;

        public byte Id = 0xff;
        public string Name = "";
        public string ColouredName => $"{Colour}{Name}";
        public string Colour = "&7";
        public string AppName = "Classic 030";
        public int Rank = 0;
        public bool CPE;
        public List<string> SupportedCPE = new List<string>();

        public string TypingMessage = "";

        public volatile short[] Position = new short[3] { 0, 0, 0 };
        public volatile byte Pitch;
        public volatile byte Yaw;

        public volatile bool UpdatePos = false;
        public Level Level;
        public Player(TcpClient client)
        {
            if (client == null) return;
            TCPClient = client; TCPClient.SendTimeout = 12000;
            stream = TCPClient.GetStream();
            Task.Run(() =>
            {
                while (TCPClient != null && TCPClient.Connected && stream != null)
                {
                    if (!Packet.TryReadPacket(stream, out var id, out var packet))
                        break;

                    if (Packet.Packets[id].Processor != null)
                        Packet.Packets[id].Processor(this, packet);
                }
                Disconnect();
            });
        }

        public static Player Console = new Player(null) { Name = "Server", Rank = 100, Colour = "&d" };

        public void SendBytes(byte[] data) { try { stream.Write(data); } catch (Exception ex) { Disconnect(); } }
        public void SendMessage(string message)
        {
            var lines = Util.SplitByLength(message, 64).ToArray();
            for (int i = 0; i < lines.Length; i++)
                SendBytes(Packet.MessagePacket(0xff, i != 0 ? "&f" + lines[i] : lines[i]));
        }
        public void Kick(string message) { SendBytes(Packet.DisconnectPacket(message)); Disconnect(); }
        public void Ban(string message) { Server.BannedUsernames.Add(Name); Kick(message); }

        public void Disconnect()
        {
            if (TCPClient != null && TCPClient.Connected) TCPClient.Close();
            Server.PlayerDisconnect(this);
        }


        public void SetLevel(Level level)
        {
            Level = level;
            Level.AddPlayer(this);
            Level.SendLevel(this);
           
        }
    }

}
