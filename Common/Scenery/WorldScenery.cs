using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Common.Scenery.Data;

namespace Common.Scenery
{
    public class WorldScenery : SceneryManager
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
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
            public string Name;

            public uint SolidMeshKey1;
            public uint SolidMeshKey2;
            public uint SolidMeshKey3;
            public uint SolidMeshKey4;

            public uint SolidMeshPointer1;
            public uint SolidMeshPointer2;
            public uint SolidMeshPointer3;
            public uint SolidMeshPointer4;

            public float Radius;
            public uint Flags;
            public uint HierarchyNameHash;
            public uint HierarchyPointer;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SceneryInstanceInternal // 0x00034103
        {
            public Vector3 BBoxMin;
            public uint InstanceFlags;
            public Vector3 BBoxMax;
            public short PrecullerInfoIndex;
            public short LightingContextNumber;
            public Vector3 Position;
            public Vector3 Rotation0;
            public Vector3 Rotation1;
            public Vector3 Rotation2;
            public uint SceneryGuid;
            public short SceneryInfoNumber;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public byte[] Padding;
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
            Debug.Assert(size % 0x48 == 0);
            var count = (int)size / 0x48;
            _scenerySection.Infos = new List<SceneryInfo>(count);
            for (int i = 0; i < count; ++i)
            {
                var info = BinaryUtil.ReadStruct<SceneryInfoStruct>(br);
                _scenerySection.Infos.Add(new SceneryInfo
                {
                    Name = info.Name,
                    SolidKey = info.SolidMeshKey1
                });
            }
            //Debug.Log($"Loaded {_scenerySection.SceneryInfos.Count} scenery definitions for ScenerySection {_scenerySection.SectionNumber}");
        }

        private void ReadSceneryInstances(BinaryReader br, uint size)
        {
            while (br.ReadUInt32() == 0x11111111)
            {
                size -= 4;
            }

            br.BaseStream.Position -= 4;

            Debug.Assert(size % 0x60 == 0);
            var count = (int)size / 0x60;
            _scenerySection.Instances = new List<SceneryInstance>(count);

            for (int i = 0; i < count; ++i)
            {
                var instance = new SceneryInstance();
                var internalInstance = BinaryUtil.ReadStruct<SceneryInstanceInternal>(br);

                instance.InfoIndex = internalInstance.SceneryInfoNumber;
                var vRight = internalInstance.Rotation0;
                var vForward = internalInstance.Rotation1;
                var vUpwards = internalInstance.Rotation2;

                instance.Transform = new Matrix4x4(vRight.X, vRight.Y, vRight.Z, 0, vForward.X, vForward.Y, vForward.Z,
                    0, vUpwards.X, vUpwards.Y, vUpwards.Z, 0, internalInstance.Position.X, internalInstance.Position.Y,
                    internalInstance.Position.Z, 1);

                _scenerySection.Instances.Add(instance);
            }
            //Debug.Log($"Loaded {_scenerySection.SceneryInstances.Count} scenery instances for ScenerySection {_scenerySection.SectionNumber}");
        }
    }
}