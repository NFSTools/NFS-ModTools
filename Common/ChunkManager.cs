using System;
using System.Collections.Generic;
using System.IO;
using Common.Geometry;
using Common.Geometry.Data;
using Common.Textures;
using Common.Textures.Data;

namespace Common
{
    public class ChunkManager
    {
        public class BasicResource { }

        public class Chunk
        {
            public uint Id { get; set; }

            public uint Size { get; set; }

            public uint Offset { get; set; }

            public byte[] Data { get; set; }

            public BasicResource Resource { get; set; }
        }

        private readonly GameDetector.Game _game;

        public readonly List<Chunk> Chunks = new List<Chunk>();

        public ChunkManager(GameDetector.Game game)
        {
            _game = game;
        }

        public void Reset()
        {
            this.Chunks.Clear();
        }

        public void Read(BinaryReader br)
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

                var fullData = br.ReadBytes((int)chunkSize);
                br.BaseStream.Position = chunkPos;

                switch (chunkId)
                {
                    case 0xb3300000:
                        Chunks.Add(new Chunk
                        {
                            Id = chunkId,
                            Offset = (uint)(chunkPos - 8),
                            Size = chunkSize,
                            Resource = new DelegateTpk().ReadTexturePack(br, chunkSize),
                            Data = fullData
                        });
                        break;
                    case 0x0003a100: // COMPRESSED tpk
                        {
                            if (br.ReadUInt32() != 0x5a4c444a)
                            {
                                break;
                            }

                            br.BaseStream.Position -= 4;
                            br.BaseStream.Position += 0x8;

                            var outSize = br.ReadUInt32();
                            var outData = new byte[outSize];

                            Compression.Decompress(fullData, outData);

                            using (var fs = File.OpenWrite($"decomp_tpk_{br.BaseStream.Position:X8}.bin"))
                            {
                                fs.Write(outData, 0, outData.Length);
                            }

                            using (var br2 = new BinaryReader(new MemoryStream(outData)))
                            {
                                Chunks.Add(new Chunk
                                {
                                    Id = chunkId,
                                    Offset = (uint)(chunkPos - 8),
                                    Size = outSize,
                                    Resource = new DelegateTpk().ReadTexturePack(br2, outSize),
                                    Data = fullData
                                });
                            }
                            //Chunks.Add(new Chunk
                            //{
                            //    Id = chunkId,
                            //    Offset = (uint)(chunkPos - 8),
                            //    Size = chunkSize,
                            //    Resource = new DelegateTpk().ReadTexturePack(br, chunkSize),
                            //    Data = fullData
                            //});

                            //var outData = new byte[chunkSize];
                            //Compression.Decompress(fullData, outData);

                            break;
                        }
                    case 0x80134000:
                        SolidList sl = null;

                        if (_game != GameDetector.Game.Unknown)
                        {
                            SolidListManager lm = null;

                            switch (_game)
                            {
                                case GameDetector.Game.MostWanted:
                                    lm = new MostWantedSolids();
                                    break;
                                case GameDetector.Game.World:
                                    lm = new World15Solids();
                                    break;
                                case GameDetector.Game.ProStreet:
                                    lm = new ProStreetSolids();
                                    break;
                                case GameDetector.Game.Carbon:
                                    lm = new CarbonSolids();
                                    break;
                                case GameDetector.Game.Unknown:
                                default:
                                    break;
                            }

                            if (lm != null)
                            {
                                sl = lm.ReadSolidList(br, chunkSize);
                            }
                        }
                        Chunks.Add(new Chunk
                        {
                            Id = chunkId,
                            Offset = (uint)(chunkPos - 8),
                            Size = chunkSize,
                            Resource = sl ?? new SolidList
                            {
                                PipelinePath = "unsupported_solids.bin",
                                ClassType = "UNKNOWN",
                                ObjectCount = 0
                            },
                            Data = fullData
                        });
                        break;
                    default:
                        Chunks.Add(new Chunk
                        {
                            Id = chunkId,
                            Offset = (uint)(chunkPos - 8),
                            Size = chunkSize,
                            Data = fullData
                        });
                        break;
                }

                br.BaseStream.Position = chunkEndPos;
            }
        }

        /// <summary>
        /// Read chunks from a file.
        /// </summary>
        /// <param name="path"></param>
        public void Read(string path)
        {
            using (var fs = File.OpenRead(path))
            using (var br = new BinaryReader(fs))
            {
                Read(br);
            }
        }
    }
}
