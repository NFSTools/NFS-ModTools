using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Common.Geometry.Data;

namespace Common.Geometry
{
    public class MostWantedMaterial : EffectBasedMaterial
    {
    }

    public class MostWantedSolidListReader : SolidListReader
    {
        protected override bool ProcessHeaderChunk(SolidList solidList, BinaryReader binaryReader, uint chunkId,
            uint chunkSize)
        {
            switch (chunkId)
            {
                case 0x134002:
                    var info = BinaryUtil.ReadStruct<SolidListInfo>(binaryReader);
                    solidList.Filename = info.Filename;
                    solidList.GroupName = info.GroupName;
                    return true;
                case 0x134003:
                    Debug.Assert(chunkSize % 8 == 0, "chunkSize % 8 == 0");
                    return true;
                case 0x134004:
                    Debug.Assert(chunkSize % 24 == 0, "chunkSize % 24 == 0");
                    return true;
                default:
                    throw new InvalidDataException($"Unexpected header chunk: 0x{chunkId:X}");
            }
        }

        protected override SolidReader CreateObjectReader()
        {
            return new MostWantedSolidReader();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SolidListInfo
        {
            public readonly long Blank;

            public readonly int Marker; // this doesn't change between games for some reason... rather unfortunate

            public readonly int NumObjects;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x38)]
            public readonly string Filename;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public readonly string GroupName;

            public readonly int UnknownOffset;
            public readonly int UnknownSize;
        }
    }
}