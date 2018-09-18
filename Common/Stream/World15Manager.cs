using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Common.Stream.Data;

namespace Common.Stream
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

            foreach (var bundleFile in Directory.EnumerateFiles(tracksDirectory, "L5R*.BUN"))
            {
                Console.WriteLine(bundleFile);
                Bundles.Add(ReadLocationBundle(bundleFile));
            }
        }

        protected override LocationBundle ReadLocationBundle(string bundlePath)
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
                        OtherSize = 0,
                        Size = 0,
                        Offset = 0
                    };

                    if (section.Type == 1)
                    {
                        // if type 1, params[0..2] = size1, size2, size3
                        worldSection.Size = section.Parameters[0];
                        worldSection.OtherSize = section.Parameters[2];

                        worldSection.ParamSize1 = section.Parameters[0];
                        worldSection.ParamSize2 = section.Parameters[1];
                        worldSection.ParamSize3 = section.Parameters[2];
                    }
                    else
                    {
                        worldSection.ParamTpkDataOff = section.Parameters[2];
                        worldSection.ParamTpkNullOff = section.Parameters[0];

                        worldSection.TextureContainerOffsets.Add(section.Parameters[3]);
                        worldSection.TextureContainerOffsets.Add(section.Parameters[6]);
                        worldSection.TextureContainerOffsets.Add(section.Parameters[9]);
                        //worldSection.TextureContainerOffsets.Add(section.Parameters[12]);
                        worldSection.ParamSize1 = worldSection.Size = section.Parameters[12];
                    }

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

            return locationBundle;
        }

        public override void WriteLocationBundle(string outPath, LocationBundle bundle, string sectionsPath)
        {
            using (var fs = new FileStream(outPath, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                // write BCHUNK_TRACKSTREAMER_2
                bw.Write(0x00034112);
                bw.Write(0x00000000);

                // write BCHUNK_TRACKPATH
                bw.Write(0x80034147);
                bw.Write(0x8);
                bw.Write(0x0003414a);
                bw.Write(0x00);

                // write BCHUNK_TRACKPOSITIONMARKERS
                bw.Write(0x00034146);
                bw.Write(0x00);

                // write BCHUNK_TRACKSTREAMER_0 (sections)
                bw.Write(0x00034110);
                bw.Write(Marshal.SizeOf<World15Section>() * bundle.Sections.Count);
                foreach (var section in bundle.Sections)
                {
                    var sectionFile = Path.Combine(sectionsPath, $"STREAM{bundle.Name}_{section.Number}.BUN");
                    var fileInfo = new FileInfo(sectionFile);

                    var sectionStruct = new World15Section
                    {
                        Hash = section.Hash,
                        StreamChunkNumber = section.Number,
                        StreamChunkNumber2 = section.Number, // ???
                        ModelGroupName = section.Name,
                        Type = 1,
                        X = section.Position.X,
                        Y = section.Position.Y,
                        Z = section.Position.Z,
                        Parameters = new uint[28],
                        Unknown2 = unchecked((int) 0xFFFFFFFF)
                    };

                    sectionStruct.Parameters[0] = (uint) fileInfo.Length;
                    sectionStruct.Parameters[1] = (uint) fileInfo.Length;
                    sectionStruct.Parameters[2] = (uint) fileInfo.Length;

                    BinaryUtil.WriteStruct(bw, sectionStruct);
                }

                // write BCHUNK_TRACKSTREAMER_1
                bw.Write(0x00034111);
                bw.Write(0x4);
                bw.Write(new byte[0x4]);

                // write BCHUNK_SCENERY (non-container)
                bw.Write(0x00034108);
                bw.Write(0x00);

                // write BCHUNK_TRACKPATH (non-container)
                bw.Write(0x0003414d);
                bw.Write(0x00);
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
