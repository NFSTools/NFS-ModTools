using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Common.TrackStream.Data;

namespace Common.TrackStream
{
    public class MostWantedManager : GameBundleManager
    {
        internal class StreamChunkInfo
        {
            public long Offset;

            public uint Size;

            public byte[] Data;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 92)]
        private struct MostWantedSection
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
            public string ModelGroupName; // 8

            public uint StreamChunkNumber; // 12

            public uint Unknown1; // 16

            public uint MasterStreamChunkNumber; // 20

            public uint MasterStreamChunkOffset; // 24

            public uint Size1; // 28

            public uint Size2; // 32

            public uint Size3; // 36

            public uint Unknown2; // 40

            public float X; // 44

            public float Y; // 48

            public float Z; // 52

            public uint Hash; // 56
        }

        public override void ReadFrom(string gameDirectory)
        {
            var tracksDirectory = Path.Combine(gameDirectory, "TRACKS");

            foreach (var bundleFile in Directory.EnumerateFiles(tracksDirectory, "L2R*.BUN"))
            {
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
                Sections = new List<StreamSection>(),
                Chunks = new List<ChunkManager.Chunk>()
            };

            var streamPath = Path.Combine(
                Path.GetDirectoryName(bundlePath),
                $"STREAM{locationBundle.Name}.BUN");
            var masterStream = new FileStream(streamPath, FileMode.Open, FileAccess.Read);

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

                var sectionStructSize = Marshal.SizeOf<MostWantedSection>();

                if (sectionsSize <= 0) return locationBundle;

                if (sectionsSize % sectionStructSize != 0)
                {
                    throw new Exception("Section structure is incorrect. You shouldn't ever get this.");
                }

                var numSections = sectionsSize / sectionStructSize;

                for (var i = 0; i < numSections; i++)
                {
                    var section = BinaryUtil.ReadStruct<MostWantedSection>(br);

                    var streamSection = new StreamSection
                    {
                        Name = section.ModelGroupName,
                        Hash = section.Hash,
                        Position = new Vector3(section.X, section.Y, section.Z),
                        Size = section.Size1,
                        PermSize = section.Size3,
                        Number = section.StreamChunkNumber,
                        UnknownValue = section.Unknown1,
                        UnknownSectionNumber = section.Unknown2,
                        Offset = section.MasterStreamChunkOffset,
                        Data = new byte[section.Size1]
                    };

                    masterStream.Position = section.MasterStreamChunkOffset;
                    masterStream.Read(streamSection.Data, 0, streamSection.Data.Length);

                    locationBundle.Sections.Add(streamSection);
                }

                br.BaseStream.Position = 0;
            }

            masterStream.Dispose();

            var cm = new ChunkManager(GameDetector.Game.MostWanted);
            cm.Read(bundlePath);
            locationBundle.Chunks = cm.Chunks;

            return locationBundle;
        }

        public override void WriteLocationBundle(string outPath, LocationBundle bundle, List<StreamSection> sections)
        {
            // Write master stream
            var streamPath = Path.Combine(
                Path.GetDirectoryName(outPath),
                $"STREAM{bundle.Name}.BUN");

            //File.Delete(streamPath);

            var offsetTable = new List<long>();

            using (var mfs = File.Open(streamPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                for (var index = 0; index < sections.Count; index++)
                {
                    var bundleSection = sections[index];
                    offsetTable.Add(mfs.Position);
                    mfs.Write(bundleSection.Data, 0, bundleSection.Data.Length);

                    if (mfs.Position % 0x800 != 0 && index != sections.Count - 1)
                    {
                        var align = 0x800 - mfs.Position % 0x800;

                        align -= 8;

                        mfs.Write(new byte[] {0x00, 0x00, 0x00, 0x00}, 0, 4);
                        mfs.Write(BitConverter.GetBytes(align), 0, 4);
                        mfs.Write(new byte[align], 0, (int) align);
                    }
                }
            }

            //File.Delete(outPath);

            // Write location bundle
            using (var bw = new BinaryWriter(File.Open(outPath, FileMode.Create, FileAccess.Write, FileShare.Read)))
            using (var cs = new ChunkStream(bw))
            {
                // We have to skip null chunks in order to avoid weirdness.
                foreach (var chunk in bundle.Chunks.Where(c => c.Id != 0))
                {
                    if (chunk.PrePadding > 8) // 0x10 - 8 = 8; greater than 8 = align 0x80
                    {
                        cs.PaddingAlignment2(0x80);
                    } else if (chunk.PrePadding > 0)
                    {
                        cs.PaddingAlignment2(0x10);
                    }

                    if (chunk.Id == 0x00034110)
                    {
                        // Write sections list
                        cs.BeginChunk(0x00034110);

                        for (var index = 0; index < sections.Count; index++)
                        {
                            var bundleSection = sections[index];
                            var sectionStruct = new MostWantedSection();

                            sectionStruct.ModelGroupName = bundleSection.Name;
                            sectionStruct.Hash = bundleSection.Hash;
                            sectionStruct.MasterStreamChunkOffset = (uint) offsetTable[index];
                            sectionStruct.MasterStreamChunkNumber = 1;
                            sectionStruct.StreamChunkNumber = bundleSection.Number;
                            sectionStruct.Size1 = sectionStruct.Size2 = bundleSection.Size;
                            //sectionStruct.Unknown2 = bundleSection.UnknownSectionNumber;
                            sectionStruct.Unknown2 = 0;
                            sectionStruct.X = bundleSection.Position.X;
                            sectionStruct.Y = bundleSection.Position.Y;
                            sectionStruct.Z = bundleSection.Position.Z;
                            cs.WriteStruct(sectionStruct);
                        }

                        cs.EndChunk();
                    }
                    else
                    {
                        cs.WriteChunk(chunk);
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

                    Array.Resize(ref data, (int)streamSection.Size);
                    br.Read(data, 0, data.Length);

                    using (var os = File.OpenWrite(Path.Combine(outDirectory, $"STREAM{bundle.Name}_{streamSection.Number}.BUN")))
                    {
                        os.Write(data, 0, data.Length);
                    }
                }

                data = null;
            }

            GC.Collect();
        }

        public override void CombineSections(List<StreamSection> sections, string outFile)
        {
            throw new NotImplementedException();
        }
    }
}
