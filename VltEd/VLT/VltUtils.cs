using System.IO;
using System.Text;

namespace VltEd.VLT
{
    public class NullTerminatedString
    {
        public static string Read(BinaryReader br)
        {
            var sb = new StringBuilder();
            byte b;
            do
            {
                b = br.ReadByte();
                if (b != 0)
                {
                    sb.Append((char)b);
                }
            } while (b != 0);
            return sb.ToString();
        }
        public static void Write(BinaryWriter bw, string value)
        {
            var str = Encoding.ASCII.GetBytes(value);
            bw.Write(str);
            bw.Write((byte)0);
        }
    }
}
