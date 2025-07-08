using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;


namespace ClassicChatServer
{
    public delegate void PacketProcessor(Player client, byte[] data);
    public class Packet
    {
        public int Length;
        public PacketProcessor Processor;

        public static Dictionary<int, Packet> Packets = new Dictionary<int, Packet>();
        public static void SetPacket(int id, int length, PacketProcessor processor = null)
        {
            var packet = new Packet() { Length = length, Processor = processor };
            if (Packets.ContainsKey(id)) Packets[id] = packet; else Packets.Add(id, packet);
        }

        public static void Init()
        {
            // Player Identification
            SetPacket(0x00, 131, (Player client, byte[] data) =>
            {
                var name = Util.ReadString(data, 2);
                var verificationkey = Util.ReadString(data, 66);
                Console.WriteLine(name);
                Console.WriteLine(verificationkey);
                if (Server.Players.Count >= Server.MaxPlayers)
                {
                    client.Kick("Too many players!!!");
                    return;
                }

                if (Server.BannedUsernames.Contains(name))
                {
                    client.Kick("You are banned!");
                    return;
                }

                if (Server.PlayerList.Any(x => x.Name == name))
                {
                    client.Kick("Someone with the same name is already connected!");
                    return;
                }
                byte[] correctkey = new byte[] { };
                using (MD5 md5 = MD5.Create())
                    correctkey = md5.ComputeHash(Encoding.ASCII.GetBytes( Convert.ToHexString(Server.Salt) + name));

                Console.WriteLine(Convert.ToHexString(correctkey).ToLower());
                Console.WriteLine(verificationkey);

                if ((Server.Public && Server.Verify) && (Convert.ToHexString(correctkey).ToLower() != verificationkey.ToLower()))
                {
                    client.Kick("Verification failed! Try to refresh server list!");
                    return;
                }

                client.Name = name;

                if (data[130] == 0x42) // CPE
                {
                    client.CPE = true;
                    client.SendBytes(Packet.CPEExtInfo(Server.Software, (short)Server.SupportedCPE.Count));
                    foreach (var cpe in Server.SupportedCPE)
                        client.SendBytes(Packet.CPEExtEntry(cpe.Key, cpe.Value));
                }

                Server.PlayerConnect(client);
            });

            SetPacket(0x05, 9, (Player client, byte[] data) =>
            {
                short x = Util.ReadShort(data, 1);
                short y = Util.ReadShort(data, 3);
                short z = Util.ReadShort(data, 5);
                byte mode = data[7];
                byte block = data[8];

                Server.Level.SetBlock(x, y, z, mode == 0x00 ? (byte)0 : block);

            });// Player set block

            SetPacket(0x08, 10, (Player client, byte[] data) => {
                short x = Util.ReadShort(data, 2);
                short y = Util.ReadShort(data, 4);
                short z = Util.ReadShort(data, 6);
                byte yaw = data[8];
                byte pitch = data[9];
                client.Position = new short[] { x, y, z };
                client.Yaw = yaw;
                client.Pitch = pitch;
                client.UpdatePos = true;
            }); // Pos / orientation

            //Player Message
            SetPacket(0x0d, 66, (Player client, byte[] data) => {

                var msg = Util.ReadString(data, 2);
                if (!client.CPE || !client.SupportedCPE.Contains("LongerMessages"))
                {
                    Server.PlayerMessage(client, msg);
                    return;
                }

                if (client.TypingMessage.Length < Server.MaxMessageLength) client.TypingMessage += msg;
                if (data[1] != 0x0) return;
                Server.PlayerMessage(client, client.TypingMessage.TrimEnd().Substring(0, ((client.TypingMessage.Length < Server.MaxMessageLength) ? client.TypingMessage.Length : Server.MaxMessageLength)));
                client.TypingMessage = string.Empty;
            });


            // Ext Info
            SetPacket(0x10, 67, (Player client, byte[] data) => { client.AppName = Util.ReadString(data, 1); });

            // Ext Entry
            SetPacket(0x11, 69, (Player client, byte[] data) =>
            {
                var name = Util.ReadString(data, 1);
                var version = Util.ReadInt(data, 65);
                if (!Server.SupportedCPE.ContainsKey(name)) return;
                client.SupportedCPE.Add(name);
            });
        }

        public static bool TryReadPacket(NetworkStream stream, out byte Id, out byte[] Packet)
        {
            int id = stream.ReadByte();
            Id = (byte)id;
            Packet = new byte[] { };
            if (id == -1 || !Packets.ContainsKey(id)) return false;
            Packet = new byte[Packets[id].Length];
            Packet[0] = (byte)id;
            int i = 1;
            while (i < Packet.Length)
            {
                var count = stream.Read(Packet, i, Packet.Length - i);
                if (count == -1 || count == 0) return false;
                i += count;
            }
            return true;
        }
        public static byte[] MessagePacket(byte sender, string message)
        {
            byte[] packet = new byte[66];
            packet[0] = 0x0d;
            packet[1] = sender;
            Array.Copy(Util.EncodeString(message), 0, packet, 2, 64);
            return packet;
        }

        public static byte[] ServerIdentifyPacket(string name, string motd, byte protocolversion = 0x07, byte usertype = 0x0)
        {
            byte[] packet = new byte[131];
            packet[0] = 0x00; packet[1] = protocolversion; packet[130] = usertype;
            Array.Copy(Util.EncodeString(name), 0, packet, 2, 64);
            Array.Copy(Util.EncodeString(motd), 0, packet, 66, 64);
            return packet;
        }
        public static byte[] DisconnectPacket(string reason)
        {
            byte[] packet = new byte[65];
            packet[0] = 0x0e;
            Array.Copy(Util.EncodeString(reason), 0, packet, 1, 64);
            return packet;
        }

        public static byte[] LevelDataStart() { return new byte[] { 0x02 }; }
        public static byte[] LevelDataChunkPacket(byte[] chunk, int length, byte percentcomplete)
        {
            byte[] packet = new byte[1028];
            packet[0] = 0x03;
            Util.WriteShort((short)length, packet, 1);
            Array.Copy(chunk, 0, packet, 3  , length);
            packet[1027] = percentcomplete;
            return packet;
        }

        public static byte[] LevelDataFinalise(short width, short height, short length)
        {
            byte[] packet = new byte[7];
            packet[0] = 0x04;
            Util.WriteShort(width, packet, 1);
            Util.WriteShort(height, packet, 3);
            Util.WriteShort(length, packet, 5);
            return packet;
        }

        public static byte[] SetBlock(short x, short y, short z, byte block)
        {
            byte[] packet = new byte[8];
            packet[0] = 0x06;
            Util.WriteShort(x, packet, 1);
            Util.WriteShort(y, packet, 3);
            Util.WriteShort(z, packet, 5);
            packet[7] = block;
            return packet;
        }
        
        public static byte[] PlayerSpawn(byte id, string name, short x, short y, short z, byte yaw, byte pitch)
        {
            byte[] packet = new byte[74];
            packet[0] = 0x07;
            packet[1] = id;
            Array.Copy(Util.EncodeString(name), 0, packet, 2, 64);
            Util.WriteShort(x, packet, 66);
            Util.WriteShort(y, packet, 68);
            Util.WriteShort(z, packet, 70);
            packet[72] = yaw;
            packet[73] = pitch;
            return packet;
        }

        public static byte[] PlayerDespawn(byte id) { return new byte[2] { 0x0c, id }; }
        public static byte[] PlayerTeleport(byte id, short x, short y, short z, byte yaw, byte pitch)
        {
            byte[] packet = new byte[10];
            packet[0] = 0x08;
            packet[1] = id;
            Util.WriteShort(x, packet, 2);
            Util.WriteShort(y, packet, 4);
            Util.WriteShort(z, packet, 6);
            packet[8] = yaw;
            packet[9] = pitch;
            return packet;
        }

        public static byte[] CPEExtInfo(string serversoftware, short amount)
        {
            byte[] packet = new byte[67];
            packet[0] = 0x10;
            Array.Copy(Util.EncodeString(serversoftware), 0, packet, 1, 64);
            Util.WriteShort(amount, packet, 65);
            return packet;
        }

        public static byte[] CPEExtEntry(string cpeentry, int version)
        {
            byte[] packet = new byte[69];
            packet[0] = 0x11;
            Array.Copy(Util.EncodeString(cpeentry), 0, packet, 1, 64);
            Util.WriteInt(version, packet, 64);
            return packet;
        }
    }

}
