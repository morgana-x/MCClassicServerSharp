using System.Text;

namespace ClassicChatServer
{
    public class Util
    {
        public static string ReadString(byte[] array, int index = 0)
        {
            byte[] buffer = new byte[64];
            Array.Copy(array, index, buffer, 0, 64);
            return Encoding.ASCII.GetString(buffer).TrimEnd();
        }
        public static byte[] EncodeString(string message)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(message);
            byte[] converted = new byte[64];
            for (int i = 0; i < 64; i++)
                converted[i] = (i < bytes.Length) ? bytes[i] : (byte)0x20;
            return converted;

        }
        public static IEnumerable<string> SplitByLength(string s, int length)
        {
            while (s.Length > length)
            {
                yield return s.Substring(0, length);
                s = s.Substring(length);
            }

            if (s.Length > 0) yield return s;
        }
        public static int ReadInt(byte[] data, int offset = 0)
        {
            byte[] buffer = new byte[4];
            Array.Copy(data, offset, buffer, 0, 4); Array.Reverse(buffer);
            return BitConverter.ToInt32(buffer);
        }
        public static short ReadShort(byte[] data, int offset = 0)
        {
            byte[] buffer = new byte[2];
            Array.Copy(data, offset, buffer, 0, 2); Array.Reverse(buffer);
            return BitConverter.ToInt16(buffer);
        }
        public static void WriteShort(short val, byte[] data, int offset = 0)
        {
            byte[] buffer = BitConverter.GetBytes(val);
            Array.Reverse(buffer); Array.Copy(buffer, 0, data, offset, 2);
        }
        public static void WriteInt(int val, byte[] data, int offset = 0)
        {
            byte[] buffer = BitConverter.GetBytes(val);
            Array.Reverse(buffer); Array.Copy(buffer, 0, data, offset, 4);
        }

        public static byte[] Salt()
        {
            byte[] salt = new byte[16];
            System.Random random = new System.Random();
            for (int i = 0; i < 16; i++)
                salt[i] = (byte)(random.Next(0, 62));
            return salt;
        }

        public static Player? FindPlayer(string search)
        {
            foreach (var p in Server.PlayerList)
                if (p.Name == search) return p;
            foreach (var p in Server.PlayerList)
                if (p.Name.ToLower() == search.ToLower()) return p;
            foreach (var p in Server.PlayerList)
                if (search.StartsWith(p.Name)) return p;
            foreach (var p in Server.PlayerList)
                if (search.ToLower().StartsWith(p.Name.ToLower())) return p;
            return null;
        }

    }

}
