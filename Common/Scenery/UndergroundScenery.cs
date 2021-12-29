using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Common.Scenery.Data;
using Common.Scenery.Structures;

namespace Common.Scenery
{
    public class UndergroundScenery : SceneryManager
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ScenerySectionHeader // 0x00034101
        {
            public long Pointer1;
            public int Pointer2;
            public int SectionNumber;
            public int Pointer3;
            public long Pointer4;
            public long Pointer5;
            public long Pointer6;
            public long Pointer7;
            public long Pointer8;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SceneryInfoStruct // 0x00034102
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public uint[] NameHash;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public short[] FarClipSize;

            public short Dummy;
            public short Dummy2;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public uint[] ModelPointers;

            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.I1, SizeConst = 6)]
            public bool[] IsFacadeFlag;

            public byte Dummy3;
            public byte Dummy4;
            public float Radius;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Matrix3x3Packed
        {
            public short Value11;
            public short Value12;
            public short Value13;
            public short Value21;
            public short Value22;
            public short Value23;
            public short Value31;
            public short Value32;
            public short Value33;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Vector3Packed
        {
            public short X, Y, Z;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SceneryInstanceInternal // 0x00034103
        {
            public Vector3Packed BBoxMin;
            public Vector3Packed BBoxMax;
            public ushort SceneryInfoNumber;
            public ushort ExcludeFlags;
            public Vector3 Position;
            public PackedRotationMatrix Rotation;
            public ushort Padding;
        }

        private ScenerySection _scenerySection;

        public override ScenerySection ReadScenery(BinaryReader br, uint containerSize)
        {
            _scenerySection = new ScenerySection();
            ReadChunks(br, containerSize);
            return _scenerySection;
        }

        protected override void ReadChunks(BinaryReader br, uint containerSize)
        {
            var endPos = br.BaseStream.Position + containerSize;

            while (br.BaseStream.Position < endPos)
            {
                var chunkId = br.ReadUInt32();
                var chunkSize = br.ReadUInt32();
                var chunkEndPos = br.BaseStream.Position + chunkSize;
                //if ((chunkId & 0x80000000) != 0x80000000)
                //{

                //}

                var padding = 0u;

                while (br.ReadUInt32() == 0x11111111)
                {
                    padding += 4;

                    if (br.BaseStream.Position >= chunkEndPos)
                    {
                        break;
                    }
                }

                br.BaseStream.Position -= 4;
                chunkSize -= padding;

                switch (chunkId)
                {
                    case 0x00034101:
                    {
                        ReadScenerySectionHeader(br);
                        break;
                    }
                    case 0x00034102:
                    {
                        ReadSceneryInfos(br, chunkSize);
                        break;
                    }
                    case 0x00034103:
                    {
                        ReadSceneryInstances(br, chunkSize);
                        break;
                    }
                    default:
                        //Console.WriteLine($"0x{chunkId:X8} [{chunkSize}] @{br.BaseStream.Position}");
                        break;
                }

                br.BaseStream.Position = chunkEndPos;
            }
        }

        private void ReadScenerySectionHeader(BinaryReader br)
        {
            var header = BinaryUtil.ReadStruct<ScenerySectionHeader>(br);
            _scenerySection.SectionNumber = header.SectionNumber;
            //Debug.Log($"ScenerySection number is {_scenerySection.SectionNumber}");
        }

        private void ReadSceneryInfos(BinaryReader br, uint size)
        {
            Debug.Assert(Marshal.SizeOf<SceneryInfoStruct>() == 0x48);
            Debug.Assert(size % 0x48 == 0);
            var count = (int)size / 0x48;
            _scenerySection.Infos = new List<SceneryInfo>(count);
            for (int i = 0; i < count; ++i)
            {
                var info = BinaryUtil.ReadStruct<SceneryInfoStruct>(br);
                _scenerySection.Infos.Add(new SceneryInfo
                {
                    Name = $"model-0x{info.NameHash[0]:X8}",
                    SolidKey = info.NameHash[0]
                });
            }
            //Debug.Log($"Loaded {_scenerySection.SceneryInfos.Count} scenery definitions for ScenerySection {_scenerySection.SectionNumber}");
        }

        private void ReadSceneryInstances(BinaryReader br, uint size)
        {
            size -= BinaryUtil.AlignReader(br, 0x10);
            Debug.Assert(size % 0x30 == 0);
            var count = (int)size / 0x30;
            _scenerySection.Instances = new List<SceneryInstance>(count);

            for (int i = 0; i < count; ++i)
            {
                var instance = new SceneryInstance();
                var internalInstance = BinaryUtil.ReadStruct<SceneryInstanceInternal>(br);

                instance.InfoIndex = internalInstance.SceneryInfoNumber;
                instance.Transform = Matrix4x4.Multiply(internalInstance.Rotation,
                    Matrix4x4.CreateTranslation(internalInstance.Position));

                _scenerySection.Instances.Add(instance);
            }
            //Debug.Log($"Loaded {_scenerySection.SceneryInstances.Count} scenery instances for ScenerySection {_scenerySection.SectionNumber}");
        }
    }
}