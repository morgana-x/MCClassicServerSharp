using ICSharpCode.SharpZipLib.GZip;
using System.IO;
using System.Xml.Linq;

namespace ClassicChatServer
{
    public class Level
    {
        public short Width;
        public short Height;
        public short Length;
        public volatile byte[] Data = new byte[0];

        public string TexturePack = "";

        public string Name = "level";

        public bool Loading = false;


        public byte Weather;

        public byte LightingMode;

        public short[] Spawn = { 0, 0, 0 };

        public List<Player> PlayerList = new List<Player>();

        DateTime nextAutoSave = DateTime.Now.AddSeconds(300);

        public Level(string name, short width, short height, short length)
        {
            Name = name;
            Width = width;
            Height = height;
            Length = length;
            Data = new byte[Length*Width*Height];

            Spawn = new short[] { (short)((Width << 5) / 2), (short)(((Height << 5) / 2)+32), (short)((Length << 5) / 2) };
            var grassheight = (height / 2)-1;
            for (short x = 0; x< width; x++)
                for (short y = 0; y< height; y++)
                    for (short z = 0; z< length; z++)
                    {
                        if (y == grassheight)
                            SetBlock(x, y, z, 2);
                        else if (y < grassheight)
                            SetBlock(x, y, z, 3);
                        else
                            SetBlock(x, y, z, 0);
                    }
            Console.WriteLine($"Loaded level of size {width} {height} {length}");

        }
        public Level() { }

        public void AddPlayer(Player p)
        {
            p.Level = this;
            PlayerList.Add(p);
            SendLevel(p);
            foreach(var pl in PlayerList)
            {
                if (pl == p) continue;
                pl.SendBytes(Packet.PlayerSpawn(p.Id, p.Name, p.Position[0], p.Position[1], p.Position[2], p.Pitch, p.Yaw));
            }
        }

        public void RemovePlayer(Player p)
        {
            if (!PlayerList.Contains(p)) return;
            foreach (var pl in PlayerList)
                pl.SendBytes(Packet.PlayerDespawn((byte)(pl == p ? 255 : p.Id)));
            PlayerList.Remove(p);
        }
        public void SendPlayers(Player p)
        {
            foreach(var pl in PlayerList)
                p.SendBytes(Packet.PlayerSpawn( (byte)(pl == p ? 255 : pl.Id), pl.Name, pl.Position[0], pl.Position[1], pl.Position[2], pl.Pitch, pl.Yaw));
        }

        public void Tick()
        {
            if (DateTime.Now > nextAutoSave)
            {
                nextAutoSave = DateTime.Now.AddSeconds(300);
                Save();
            }
            foreach(var p in PlayerList)
            {
                if (p.UpdatePos)
                {
                    p.UpdatePos = false;
                    foreach(var pl in PlayerList)
                    {
                        if (pl == p) continue;
                        pl.SendBytes(Packet.PlayerTeleport(p.Id, p.Position[0], p.Position[1], p.Position[2], p.Yaw, p.Pitch));
                    }
                }
            }
        }
        public void SendLevel(Player p)
        {
            Task.Run(() =>
            {
                p.SendBytes(Packet.LevelDataStart());

                byte[] data = new byte[4 + Data.Length];
                var intbuffer = BitConverter.GetBytes((int)Data.Length);
                Array.Reverse(intbuffer);
                Array.Copy(intbuffer, 0, data, 0, 4);
                Array.Copy(Data, 0, data, 4, Data.Length);
                using (MemoryStream instream = new MemoryStream(data))
                {
                    using (MemoryStream compressedstream = new MemoryStream())
                    {
                        GZip.Compress(instream, compressedstream, false, bufferSize: 1024);

                        int offset = 0;
                        byte[] buffer = new byte[1024];

                        compressedstream.Position = 0;
                        while (p.TCPClient.Connected)
                        {
                            buffer = new byte[1024];
                            int bytesread = compressedstream.Read(buffer);
                            if (bytesread < 1) break;
                            offset += bytesread;
                            var progress = (byte)(100f * ((float)compressedstream.Position / (float)compressedstream.Length));
                            p.SendBytes(Packet.LevelDataChunkPacket(buffer, bytesread, progress));
                            Thread.Sleep(5);
                        }
                    }
                    p.SendBytes(Packet.LevelDataFinalise(Width, Height, Length));
                    p.Position[0] = Spawn[0];
                    p.Position[1] = Spawn[1];
                    p.Position[2] = Spawn[2];
                    SendPlayers(p);
                
                    p.SendBytes(Packet.PlayerTeleport(255, p.Position[0], p.Position[1], p.Position[2], p.Yaw, p.Pitch));
                }
            });
        }

        public static string BasePath => $"{Directory.GetCurrentDirectory()}/level";
        public string SavePath => $"{BasePath}/{Name}.lvl";
        public void Save(string path="")
        {
            if (path == "") path = SavePath;
            if (!Directory.Exists(Directory.GetParent(path).FullName))
                Directory.CreateDirectory(Directory.GetParent(path).FullName);
            FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);

            fs.Write(BitConverter.GetBytes(Width));
            fs.Write(BitConverter.GetBytes(Height));
            fs.Write(BitConverter.GetBytes(Length));
            fs.Write(BitConverter.GetBytes(Spawn[0]));
            fs.Write(BitConverter.GetBytes(Spawn[1]));
            fs.Write(BitConverter.GetBytes(Spawn[2]));

            var compressedData = new MemoryStream();
            GZip.Compress(new MemoryStream(Data), compressedData, false);
            fs.Write(compressedData.ToArray());

            fs.Close();
            Console.WriteLine($"Saved the level to {path}");
        }
        public static bool Exists(string name)
        {
            return File.Exists($"{BasePath}/{name}.lvl");
        }

        public static Level Gen(string name, short width, short height, short length)
        {
            Level level = new Level() { Name = name, Width = width, Height = height, Length = length };

            level.Data = new byte[width * height * length];
            level.Spawn = new short[] { (short)((level.Width << 5) / 2), (short)(((level.Height << 5) / 2) + 32), (short)((level.Length << 5) / 2) };
            var grassheight = (height / 2) - 1;
            for (short x = 0; x < width; x++)
                for (short y = 0; y < height; y++)
                    for (short z = 0; z < length; z++)
                    {
                        if (y == grassheight)
                            level.SetBlock(x, y, z, 2);
                        else if (y < grassheight)
                            level.SetBlock(x, y, z, 3);
                        else
                            level.SetBlock(x, y, z, 0);
                    }
            Console.WriteLine($"Generated level of size {width} {height} {length}");
            level.Save();
            return level;
        }
        public static Level Load(string name="")
        {
            if (!Exists(name))
                return new Level(name, 256, 256, 256);
 
            var lvl = new Level();
            lvl.Name = name;

            string path = $"{BasePath}/{name}.lvl";

            if (!File.Exists(path))
                return new Level(name, 256, 256, 256);

            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);
            lvl.Width = br.ReadInt16();
            lvl.Height = br.ReadInt16();
            lvl.Length = br.ReadInt16();

            lvl.Spawn = new short[3] {br.ReadInt16(), br.ReadInt16(), br.ReadInt16()};

            var decompressed = new MemoryStream();
            GZip.Decompress(new MemoryStream(br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position))), decompressed, false);
            lvl.Data = decompressed.ToArray();

            br.Close();
            fs.Close();
            Console.WriteLine($"Loaded level from {name}");
            return lvl;

        }

        public int PackCoords(short x, short y, short z)
        {
            return x + (z * Width) + (y * Width * Length);////(((y) * Length + (z)) * Width + (x));
        }

        public short GetBlock(short x, short y, short z)
        {
            if (Loading) return 0;
            if (x < 0 || y < 0 || z < 0) return 0;
            if (x >= Width || y >= Height || z >= Length) return 0;
            return (short)(Data[PackCoords(x, y, z)]);
        }

        public void SetBlock(int indice, byte block)
        {
            Data[indice] = block;
        }
        public bool ValidPos(short x, short y, short z)
        {
            if (x < 0 || y < 0 || z < 0) return false;
            if (x >= Width || y >= Height || z >= Length) return false;
            return true;
        }
        public bool ValidPos(short[] pos)
        {
            return ValidPos(pos[0], pos[1], pos[2]);
        }
        public void SetBlock(short x, short y, short z, byte block)
        {
            if (Loading) return;
            if (!ValidPos(x, y, z)) return;
            Data[PackCoords(x, y, z)] = block;
            foreach (var p in PlayerList)
                p.SendBytes(Packet.SetBlock(x, y, z, block));
        }
    }
}
