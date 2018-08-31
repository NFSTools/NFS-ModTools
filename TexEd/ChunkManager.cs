using System;
using System.Collections.Generic;
using System.IO;
using TexEd.Data;
using TexEd.Games;

namespace TexEd
{
    public class ChunkManager
    {
        public struct Chunk
        {
            public uint Id { get; set; }

            public uint Size { get; set; }

            public uint Offset { get; set; }

            public int TpkIndex { get; set; } 
        }

        public readonly List<Chunk> Chunks = new List<Chunk>();
        public readonly List<TexturePack> TexturePacks = new List<TexturePack>();

        /// <summary>
        /// Read chunks from a file.
        /// </summary>
        /// <param name="path"></param>
        public void Read(string path)
        {
            using (var fs = File.OpenRead(path))
            using (var br = new BinaryReader(fs))
            {
                if (br.BaseStream.Length % 2 != 0)
                {
                    throw new Exception("Invalid chunk bundle");
                }

                var endPos = br.BaseStream.Length;

                while (br.BaseStream.Position < endPos)
                {
                    var chunkId = br.ReadUInt32();
                    var chunkSize = br.ReadUInt32();
                    var chunkEndPos = br.BaseStream.Position + chunkSize;
                    var chunkPos = br.BaseStream.Position;

                    if (chunkId == 0xb3300000)
                    {
                        TexturePacks.Add(new DelegateTpk().ReadTexturePack(br, chunkSize));
                        Chunks.Add(new Chunk
                        {
                            Id = chunkId,
                            Offset = (uint) (chunkPos - 8),
                            Size = chunkSize,
                            TpkIndex = TexturePacks.Count - 1
                        });
                    }
                    else
                    {
                        Chunks.Add(new Chunk
                        {
                            Id = chunkId,
                            Offset = (uint)(chunkPos - 8),
                            Size = chunkSize,
                            TpkIndex = -1
                        });
                    }

                    br.BaseStream.Position = chunkEndPos;
                }
            }
        }
    }
}
