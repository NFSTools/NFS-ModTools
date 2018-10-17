using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Common.TrackStream.Data;

namespace Common.TrackStream
{
    public class UndercoverManager : GameBundleManager
    {
        internal struct StreamChunkInfo
        {
            public long Offset;

            public uint Size;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 92)]
        private struct UndercoverSection
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
            public string ModelGroupName;

            public uint StreamChunkNumber;

            public uint Unknown1;

            public uint MasterStreamChunkNumber;

            public uint MasterStreamChunkOffset;

            public uint Size1;

            public uint Size2;

            public uint Size3;

            public uint Unknown2;

            public float X;

            public float Y;

            public float Z;

            public uint Hash;
        }

        public override void ReadFrom(string gameDirectory)
        {
            var tracksDirectory = Path.Combine(gameDirectory, "TRACKS");

            foreach (var bundleFile in Directory.EnumerateFiles(tracksDirectory, "L8R*.BUN"))
            {
                Console.WriteLine(bundleFile);
                Bundles.Add(ReadLocationBundle(bundleFile));
            }
        }

        public override LocationBundle ReadLocationBundle(string bundlePath)
        {
            var fileName = Path.GetFileName(bundlePath);

            if (fileName == null)
            {
                throw new Exception("Please. Don't do this to me!");
            }

            var locationBundle = new LocationBundle
            {
                File = bundlePath,
                Name = fileName.Substring(0, fileName.IndexOf('.')),
                Sections = new List<StreamSection>()
            };

            using (var fs = File.OpenRead(bundlePath))
            using (var br = new BinaryReader(fs))
            {
                var sectionsOffset = 0L;
                var sectionsSize = BinaryUtil.FindChunk(br, 0x00034110, ref sectionsOffset);

                if (sectionsOffset == 0L)
                {
                    throw new Exception($"Cannot find section list in [{bundlePath}]! Location bundle is probably corrupted.");
                }

                br.BaseStream.Position = sectionsOffset + 8;

                var sectionStructSize = Marshal.SizeOf<UndercoverSection>();

                if (sectionsSize <= 0) return locationBundle;

                if (sectionsSize % sectionStructSize != 0)
                {
                    throw new Exception("Section structure is incorrect. You shouldn't ever get this.");
                }

                var numSections = sectionsSize / sectionStructSize;

                for (var i = 0; i < numSections; i++)
                {
                    var section = BinaryUtil.ReadStruct<UndercoverSection>(br);

                    locationBundle.Sections.Add(new StreamSection
                    {
                        Name = section.ModelGroupName,
                        Hash = section.Hash,
                        Position = new Vector3(section.X, section.Y, section.Z),
                        Size = section.Size1,
                        OtherSize = section.Size3,
                        Number = section.StreamChunkNumber,
                        Offset = section.MasterStreamChunkOffset
                    });
                }
            }

            return locationBundle;
        }

        public override void WriteLocationBundle(string outPath, LocationBundle bundle, string sectionsPath)
        {
            var chunkManager = new ChunkManager(GameDetector.Game.Undercover);
            chunkManager.Read(bundle.File);

            var masterStreamPath = Path.Combine(Path.GetDirectoryName(outPath), $"STREAM{bundle.Name}.BUN");
            var sectionInfoMap = new Dictionary<uint, StreamChunkInfo>();
            var sectionDataMap = new Dictionary<uint, byte[]>();

            using (var fs = new FileStream(masterStreamPath, FileMode.Open))
            using (var br = new BinaryReader(fs))
            {
                foreach (var section in bundle.Sections)
                {
                    sectionDataMap[section.Number] = new byte[0];

                    var sectionPath = Path.Combine(sectionsPath, $"STREAM{bundle.Name}_{section.Number}.BUN");

                    if (File.Exists(sectionPath))
                    {
                        sectionDataMap[section.Number] = File.ReadAllBytes(sectionPath);
                    }
                    else
                    {
                        br.BaseStream.Position = section.Offset;

                        sectionDataMap[section.Number] = new byte[section.Size];
                        br.Read(sectionDataMap[section.Number], 0, (int)section.Size);
                    }
                }
            }

            using (var fs = new FileStream(masterStreamPath, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                for (var index = 0; index < bundle.Sections.Count; index++)
                {
                    var section = bundle.Sections[index];

                    sectionInfoMap[section.Number] = new StreamChunkInfo
                    {
                        Offset = bw.BaseStream.Position,
                        Size = (uint)sectionDataMap[section.Number].Length
                    };

                    bw.Write(sectionDataMap[section.Number]);

                    if (index != bundle.Sections.Count - 1)
                    {
                        // calculate and write alignment chunk, align by 0x800 bytes
                        var alignSize = sectionDataMap[section.Number].Length + 8 -
                                        (sectionDataMap[section.Number].Length + 8) % 0x800 + 0x1000;
                        alignSize -= sectionDataMap[section.Number].Length + 8;

                        bw.Write(0x00000000);
                        bw.Write(alignSize);
                        bw.BaseStream.Position += alignSize;
                    }
                }
            }

            using (var fs = new FileStream(outPath, FileMode.Create))
            {
                using (var chunkStream = new ChunkStream(new BinaryWriter(fs)))
                {
                    ChunkManager.Chunk previousChunk = null;

                    foreach (var chunk in chunkManager.Chunks.Where(c => c.Id != 0))
                    {
                        if (previousChunk != null && previousChunk.Size > 0)
                        {
                            chunkStream.PaddingAlignment(0x10);
                        }

                        chunkStream.BeginChunk(chunk.Id);

                        if (chunk.Id == 0x00034110)
                        {
                            // write sections
                            foreach (var bundleSection in bundle.Sections)
                            {
                                var sectionStruct = new UndercoverSection
                                {
                                    Hash = bundleSection.Hash,
                                    ModelGroupName = bundleSection.Name,
                                    MasterStreamChunkNumber = 0,
                                    StreamChunkNumber = bundleSection.Number,
                                    Size1 = sectionInfoMap[bundleSection.Number].Size,
                                    Size2 = sectionInfoMap[bundleSection.Number].Size,
                                    Size3 = sectionInfoMap[bundleSection.Number].Size,
                                    X = bundleSection.Position.X,
                                    Y = bundleSection.Position.Y,
                                    Z = bundleSection.Position.Z,
                                    MasterStreamChunkOffset = (uint)sectionInfoMap[bundleSection.Number].Offset
                                };

                                chunkStream.WriteStruct(sectionStruct);
                            }
                        }
                        else
                        {
                            chunkStream.Write(chunk.Data);
                        }

                        chunkStream.EndChunk();

                        previousChunk = chunk;
                    }
                }
            }
        }

        public override void ExtractBundleSections(LocationBundle bundle, string outDirectory)
        {
            var directory = Path.GetDirectoryName(bundle.File);

            if (directory == null)
            {
                throw new ArgumentException("Invalid bundle object given.");
            }

            var streamPath = Path.Combine(directory, $"STREAM{bundle.Name}.BUN");

            if (!File.Exists(streamPath))
            {
                throw new ArgumentException("Bundle has no associated stream!");
            }

            Directory.CreateDirectory(outDirectory);

            using (var fs = File.OpenRead(streamPath))
            using (var br = new BinaryReader(fs))
            {
                var data = new byte[0];

                foreach (var streamSection in bundle.Sections)
                {
                    br.BaseStream.Position = streamSection.Offset;

                    data = br.ReadBytes((int)streamSection.Size);

                    using (var os = File.OpenWrite(Path.Combine(outDirectory, $"STREAM{bundle.Name}_{streamSection.Number}.BUN")))
                    {
                        os.Write(data, 0, data.Length);
                    }

                    data = new byte[0];
                }
            }

            GC.Collect();
        }

        public override void CombineSections(List<StreamSection> sections, string outFile)
        {
            throw new NotImplementedException();
        }
    }
}
