using System.IO;

namespace VltEd.VLT
{
    public interface IFileAccess
    {
        void Read(BinaryReader br);
        void Write(BinaryWriter bw);
    }
}
