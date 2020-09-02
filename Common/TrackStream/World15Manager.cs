using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Common.TrackStream.Data;

namespace Common.TrackStream
{
    public class World15Manager : GameBundleManager
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 208)]
        private struct World15Section
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
            public string ModelGroupName;

            public uint StreamChunkNumber;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public uint[] Blank;

            public uint Hash;

            public uint FragmentFileId;

            public uint Unknown1;

            public uint StreamChunkNumber2;

            public float X;

            public float Y;

            public float Z;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
            public byte[] Blank2;

            public int Unknown2;

            public uint Type; // == 1 if FragmentFileId == 0

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 28)]
            public uint[] Parameters;
        }

        public override void ReadFrom(string gameDirectory)
        {
            var tracksHighExists = Directory.Exists(Path.Combine(gameDirectory, "TracksHigh"));
            var tracksDirectory = Path.Combine(gameDirectory, tracksHighExists ? "TracksHigh" : "Tracks");

            foreach (var bundleFile in Directory.EnumerateFiles(tracksDirectory, "L5R?.BUN"))
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

                var sectionStructSize = Marshal.SizeOf<World15Section>();

                if (sectionsSize <= 0) return locationBundle;

                if (sectionsSize % sectionStructSize != 0)
                {
                    throw new Exception("Section structure is incorrect. You shouldn't ever get this.");
                }

                var numSections = sectionsSize / sectionStructSize;

                for (var i = 0; i < numSections; i++)
                {
                    var section = BinaryUtil.ReadStruct<World15Section>(br);
                    var worldSection = new WorldStreamSection
                    {
                        Name = section.ModelGroupName,
                        Hash = section.Hash,
                        Position = new Vector3(section.X, section.Y, section.Z),
                        Number = section.StreamChunkNumber,
                        Type = section.Type,
                        FragmentFileId = section.FragmentFileId,
                        PermSize = 0,
                        Size = 0,
                        Offset = 0,
                        UnknownSectionNumber = section.StreamChunkNumber2
                    };

                    var scnDiff = section.StreamChunkNumber2 - section.StreamChunkNumber;

                    //if (section.Type == 1)
                    //{
                    //    // if type 1, params[0..2] = size1, size2, size3
                    //    worldSection.Size = section.Parameters[0];
                    //    worldSection.PermSize = section.Parameters[2];

                    //    worldSection.ParamSize1 = section.Parameters[0];
                    //    worldSection.ParamSize2 = section.Parameters[1];
                    //    worldSection.ParamSize3 = section.Parameters[2];
                    //}
                    //else
                    //{
                    //    worldSection.Size = section.Parameters[9];
                    //    worldSection.PermSize = section.Parameters[11];
                    //}

                    worldSection.Size = section.Parameters[(4 * 3 * (section.Type - 1)) >> 2];
                    worldSection.PermSize = section.Parameters[((4 * 3 * (section.Type - 1)) >> 2) + 2];

                    locationBundle.Sections.Add(worldSection);

                    //locationBundle.Sections.Add(new WorldStreamSection
                    //{
                    //    Name = section.ModelGroupName,
                    //    Hash = section.Hash,
                    //    Position = new Vector3(section.X, section.Y, section.Z),
                    //    Size = section.Size1,
                    //    OtherSize = section.Size3,
                    //    Number = section.StreamChunkNumber,
                    //    Offset = section.MasterStreamChunkOffset
                    //});
                }
            }

            var cm = new ChunkManager(GameDetector.Game.World, ChunkManager.ChunkManagerOptions.SkipNull);
            cm.Read(bundlePath);
            locationBundle.Chunks = cm.Chunks;

            return locationBundle;
        }

        public override void WriteLocationBundle(string outPath, LocationBundle bundle, List<StreamSection> sections)
        {
            using (var bw = new BinaryWriter(File.Open(outPath, FileMode.Create, FileAccess.Write, FileShare.Read)))
            using (var cs = new ChunkStream(bw))
            {
                // We have to skip null chunks in order to avoid weirdness.
                foreach (var chunk in bundle.Chunks.Where(c => c.Id != 0))
                {
                    if (chunk.PrePadding > 8) // 0x10 - 8 = 8; greater than 8 = align 0x80
                    {
                        cs.PaddingAlignment2(0x80);
                    }
                    else if (chunk.PrePadding > 0)
                    {
                        cs.PaddingAlignment2(0x10);
                    }

                    if (chunk.Id == 0x00034110)
                    {
                        // Write sections list
                        cs.BeginChunk(0x00034110);

                        foreach (var bundleSection in sections.Cast<WorldStreamSection>())
                        {
                            var secStruct = new World15Section();

                            secStruct.Hash = bundleSection.Hash;
                            secStruct.ModelGroupName = bundleSection.Name;
                            secStruct.Blank = new uint[3];
                            secStruct.Blank2 = new byte[36];
                            secStruct.StreamChunkNumber = bundleSection.Number;
                            secStruct.StreamChunkNumber2 = bundleSection./*UnknownSectionNumber*/Number;
                            secStruct.FragmentFileId = bundleSection.FragmentFileId;
                            secStruct.Unknown1 = 2;
                            secStruct.X = bundleSection.Position.X;
                            secStruct.Y = bundleSection.Position.Y;
                            secStruct.Z = bundleSection.Position.Z;
                            secStruct.Type = bundleSection.Type;

                            unchecked
                            {
                                secStruct.Unknown2 = (int) 0xffffffff;
                            }

                            secStruct.Parameters = new uint[28];
                            secStruct.Parameters[(4 * 3 * (secStruct.Type - 1)) >> 2] = bundleSection.Size;
                            secStruct.Parameters[((4 * 3 * (secStruct.Type - 1)) >> 2) + 1] = bundleSection.Size;
                            secStruct.Parameters[((4 * 3 * (secStruct.Type - 1)) >> 2) + 2] = bundleSection.PermSize;

                            //worldSection.Size = section.Parameters[(4 * 3 * (section.Type - 1)) >> 2];
                            //worldSection.PermSize = section.Parameters[((4 * 3 * (section.Type - 1)) >> 2) + 2];

                            //if (section.Type == 1)
                            //{
                            //    // if type 1, params[0..2] = size1, size2, size3
                            //    worldSection.Size = section.Parameters[0];
                            //    worldSection.OtherSize = section.Parameters[2];

                            //    worldSection.ParamSize1 = section.Parameters[0];
                            //    worldSection.ParamSize2 = section.Parameters[1];
                            //    worldSection.ParamSize3 = section.Parameters[2];
                            //}
                            //else
                            //{
                            //    worldSection.ParamTpkDataOff = section.Parameters[2];
                            //    worldSection.ParamTpkNullOff = section.Parameters[0];

                            //    worldSection.TextureContainerOffsets.Add(section.Parameters[3]);
                            //    worldSection.TextureContainerOffsets.Add(section.Parameters[6]);
                            //    worldSection.TextureContainerOffsets.Add(section.Parameters[9]);
                            //    //worldSection.TextureContainerOffsets.Add(section.Parameters[12]);
                            //    worldSection.ParamSize1 = worldSection.Size = section.Parameters[12];
                            //}

                            cs.WriteStruct(secStruct);
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
            throw new NotSupportedException("NFS:World sections are already in their own files");
        }

        public override void CombineSections(List<StreamSection> sections, string outFile)
        {
            throw new NotImplementedException();
        }
    }
}
