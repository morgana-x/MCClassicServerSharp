namespace ClassicChatServer
{
    public class Rank
    {
        public static Dictionary<string, int> Ranks = new Dictionary<string, int>();
        public static int GetRank(string name)
        {
            return Ranks.ContainsKey(name) ? Ranks[name] : 0;
        }
        public static string SaveFilePath => $"{Directory.GetCurrentDirectory()}/ranks.data";
        public static void SetRank(string name, int rank)
        {
            if (!Ranks.ContainsKey(name))
                Ranks.Add(name, rank);
            Ranks[name] = rank;
            Save();
        }

        public static void Load()
        {
            if (!File.Exists(SaveFilePath)) { Save(); return; }

            using FileStream fs = new FileStream(SaveFilePath, FileMode.Open, FileAccess.Read);
            using BinaryReader br = new BinaryReader(fs);

            while (fs.Position < fs.Length)
            {
                string name = br.ReadString();
                int rank = br.ReadInt32();
                if (Ranks.ContainsKey(name))
                    Ranks[name] = rank;
                else
                    Ranks.Add(name, rank);
            }
            br.Dispose();
        }

        public static void Save()
        {

            using FileStream fs = new FileStream(SaveFilePath, FileMode.Create, FileAccess.Write);
            using BinaryWriter br = new BinaryWriter(fs);
            foreach (var pair in Ranks)
            {
                br.Write(pair.Key);
                br.Write(pair.Value);
            }
            br.Dispose();
        }
    }
}
