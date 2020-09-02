using System.Collections.Generic;

namespace Common.TrackStream.Data
{
    public class StreamSection
    {
        public string Name { get; set; }

        public uint Hash { get; set; }

        public uint Number { get; set; }

        public Vector3 Position { get; set; }

        public uint Offset { get; set; }

        public uint Size { get; set; }

        public uint PermSize { get; set; }

        public uint UnknownValue { get; set; }
        public uint UnknownSectionNumber { get; set; }
        public byte[] Data { get; set; }
    }

    public class WorldStreamSection : StreamSection
    {
        public uint FragmentFileId { get; set; }

        public uint Type { get; set; } // this is kind of an unknown, ranges from 1-5???

        public uint ParamSize1 { get; set; }
        public uint ParamSize2 { get; set; }
        public uint ParamSize3 { get; set; }

        public uint ParamTpkNullOff { get; set; }
        public uint ParamTpkDataOff { get; set; }
        public List<uint> TextureContainerOffsets { get; } = new List<uint>();
    }

    public class LocationBundle
    {
        public string Name { get; set; }

        public string File { get; set; }

        public List<StreamSection> Sections { get; set; }

        public List<ChunkManager.Chunk> Chunks { get; set; }
    }
}
