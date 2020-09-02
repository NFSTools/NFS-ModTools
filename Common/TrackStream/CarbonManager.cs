using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Common.TrackStream.Data;

namespace Common.TrackStream
{
    public class CarbonManager : GameBundleManager
    {
        internal struct StreamChunkInfo
        {
            public long Offset;

            public uint Size;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 92)]
        private struct CarbonSection
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

            foreach (var bundleFile in Directory.EnumerateFiles(tracksDirectory, "L5R*.BUN"))
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

                var sectionStructSize = Marshal.SizeOf<CarbonSection>();

                if (sectionsSize <= 0) return locationBundle;

                if (sectionsSize % sectionStructSize != 0)
                {
                    throw new Exception("Section structure is incorrect. You shouldn't ever get this.");
                }

                var numSections = sectionsSize / sectionStructSize;

                for (var i = 0; i < numSections; i++)
                {
                    var section = BinaryUtil.ReadStruct<CarbonSection>(br);

                    locationBundle.Sections.Add(new StreamSection
                    {
                        Name = section.ModelGroupName,
                        Hash = section.Hash,
                        Position = new Vector3(section.X, section.Y, section.Z),
                        Size = section.Size1,
                        PermSize = section.Size3,
                        Number = section.StreamChunkNumber,
                        Offset = section.MasterStreamChunkOffset
                    });
                }
            }

            return locationBundle;
        }

        public override void WriteLocationBundle(string outPath, LocationBundle bundle, List<StreamSection> sections)
        {
            throw new NotImplementedException();
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

                    Array.Resize(ref data, (int) streamSection.Size);
                    br.Read(data, 0, data.Length);

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
