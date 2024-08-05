using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Common.Geometry.Data;

namespace Common.Geometry
{
    public class CarbonMaterial : SolidObjectMaterial, IEffectBasedMaterial
    {
        public uint EffectId { get; set; }
    }

    public class CarbonSolidListReader : SolidListReader
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
                    ProcessStreamingTable(solidList, binaryReader, chunkSize);
                    return
                        false; // We need to stop after processing the streaming table, otherwise we'll run into bad stuff
                default:
                    throw new InvalidDataException($"Unexpected header chunk: 0x{chunkId:X}");
            }
        }

        private void ProcessStreamingTable(SolidList solidList, BinaryReader binaryReader, uint chunkSize)
        {
            var chunkEndPos = binaryReader.BaseStream.Position + chunkSize;
            Debug.Assert(chunkSize % 24 == 0, "chunkSize % 24 == 0");
            while (binaryReader.BaseStream.Position < chunkEndPos)
            {
                var och = BinaryUtil.ReadUnmanagedStruct<SolidObjectOffset>(binaryReader);
                var curPos = binaryReader.BaseStream.Position;
                binaryReader.BaseStream.Position = och.Offset;

                Debug.Assert(och.Length >= 8, "och.Length >= 8");

                if (och.Length == och.LengthCompressed)
                {
                    // Assume that the object is uncompressed.
                    // If this ever turns out to be false, I don't know what I'll do.
                    binaryReader.BaseStream.Position += 8;
                    solidList.Objects.Add(CreateObjectReader().Read(binaryReader, och.Length - 8));
                }
                else
                {
                    // Load decompressed object
                    using var ms = new MemoryStream();
                    Compression.DecompressCip(binaryReader.BaseStream, ms, och.LengthCompressed);

                    ms.Position = 8;
                    using var dcr = new BinaryReader(ms);
                    solidList.Objects.Add(CreateObjectReader().Read(dcr, och.Length - 8));
                }

                binaryReader.BaseStream.Position = curPos;
            }
        }

        protected override SolidReader CreateObjectReader()
        {
            return new CarbonSolidReader();
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

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 0x18)]
        private struct SolidObjectOffset
        {
            public uint Hash;
            public uint Offset;
            public readonly uint LengthCompressed;
            public readonly uint Length;
            public uint Flags;
            private uint blank;
        }
    }
}