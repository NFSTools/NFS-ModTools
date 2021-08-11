using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Common.Scenery.Data;

namespace Common.Scenery
{
    public class Underground2Scenery : SceneryManager
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
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string Name;

            public uint SolidMeshKey1;
            public uint SolidMeshKey2;
            public uint SolidMeshKey3;
            public ushort SomeFlag1;
            public ushort SomeFlag2;
            public int Pointer1;
            public int Pointer2;
            public int Pointer3;
            public float Radius;
            public uint HierarchyKey;
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
        public struct SceneryInstanceInternal // 0x00034103
        {
            public Vector3 BBoxMin;
            public Vector3 BBoxMax;
            public ushort SceneryInfoNumber;
            public ushort InstanceFlags;
            public int PrecullerInfoIndex;
            public Vector3 Position;
            public Matrix3x3Packed Rotation;
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
            Debug.Assert(size % 0x44 == 0);
            var count = (int)size / 0x44;
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
            size -= BinaryUtil.AutoAlign(br, 0x10);
            Debug.Assert(size % 0x40 == 0);
            var count = (int)size / 0x40;
            _scenerySection.Instances = new List<SceneryInstance>(count);

            for (int i = 0; i < count; ++i)
            {
                var instance = new SceneryInstance();
                var internalInstance = BinaryUtil.ReadStruct<SceneryInstanceInternal>(br);

                instance.InfoIndex = internalInstance.SceneryInfoNumber;
                instance.Position = new Vector3(internalInstance.Position.X, internalInstance.Position.Y, internalInstance.Position.Z);
                var vRight = new Vector3(internalInstance.Rotation.Value11, internalInstance.Rotation.Value12,
                    internalInstance.Rotation.Value13);
                var vForward = new Vector3(internalInstance.Rotation.Value21, internalInstance.Rotation.Value22,
                    internalInstance.Rotation.Value23);
                var vUpwards = new Vector3(internalInstance.Rotation.Value31, internalInstance.Rotation.Value32,
                    internalInstance.Rotation.Value33);
                vRight *= 0.0001220703125f; // vRight /= 0x2000
                vUpwards *= 0.0001220703125f; // vUpwards /= 0x2000
                vForward *= 0.0001220703125f; // vForward /= 0x2000

                instance.Scale = new Vector3(vRight.Length(), vUpwards.Length(), vForward.Length());
                vRight = Vector3.Normalize(vRight);
                vUpwards = Vector3.Normalize(vUpwards);
                vForward = Vector3.Normalize(vForward);

                if (Vector3.Dot(Vector3.Cross(vForward, vUpwards), vRight) < 0)
                {
                    vRight = -vRight;
                    instance.Scale = new Vector3(-instance.Scale.X, instance.Scale.Y, instance.Scale.Z);
                }

                instance.Rotation = Quaternion.CreateFromRotationMatrix(new Matrix4x4(
                    vRight.X,
                    vRight.Y,
                    vRight.Z,
                    0,

                    vForward.X,
                    vForward.Y,
                    vForward.Z,
                    0,
                    
                    vUpwards.X,
                    vUpwards.Y,
                    vUpwards.Z,
                    0,
                    
                    0,
                    0,
                    0,
                    0
                ));

                _scenerySection.Instances.Add(instance);
            }
            //Debug.Log($"Loaded {_scenerySection.SceneryInstances.Count} scenery instances for ScenerySection {_scenerySection.SectionNumber}");
        }
    }
}