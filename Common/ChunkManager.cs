using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Common.Geometry;
using Common.Lights;
using Common.Scenery;
using Common.Textures;

namespace Common
{
    public abstract class RealFile
    {
        protected const uint TexturePackChunk = 0xb3300000;
        protected const uint ObjectPackChunk = 0x80134000;
        protected BinaryReader Reader;

        protected Stream Stream;

        protected void SkipChunk(RealChunk chunk)
        {
            chunk.Skip(Stream);
        }

        protected RealChunk NextChunk()
        {
            var chunk = new RealChunk();
            chunk.Read(Reader);
            return chunk;
        }

        protected void Open(Stream stream)
        {
            if (Stream != null)
                Close();

            Stream = stream;
            Stream.Seek(0, SeekOrigin.Begin);
            Reader = new BinaryReader(Stream, Encoding.Default, true);
            ProcessOpen();
            Close();
        }

        private void Close()
        {
            Reader?.Dispose();
            Reader = null;
            Stream.Dispose();
        }

        protected abstract void ProcessOpen();
    }

    public class ChunkManager : RealFile
    {
        private readonly GameDetector.Game _game;

        public ChunkManager(GameDetector.Game game)
        {
            _game = game;
        }

        public List<Chunk> Chunks { get; } = new();

        protected override void ProcessOpen()
        {
            while (Stream.Position < Stream.Length)
            {
                var chunk = NextChunk();
                var cd = new Chunk
                {
                    Id = chunk.Type,
                    Size = chunk.Length,
                    Data = Array.Empty<byte>(),
                    Offset = chunk.Offset,
                    SubChunks = new List<Chunk>()
                };

                switch (chunk.Type)
                {
                    case ObjectPackChunk:
                    {
                        SolidListReader solidListReader = _game switch
                        {
                            GameDetector.Game.Underground => new UndergroundSolidListReader(),
                            GameDetector.Game.Underground2 => new Underground2SolidListReader(),
                            GameDetector.Game.MostWanted => new MostWantedSolidListReader(),
                            GameDetector.Game.Carbon => new CarbonSolidListReader(),
                            GameDetector.Game.ProStreet => new ProStreetSolidListReader(),
                            GameDetector.Game.Undercover => new UndercoverSolidListReader(),
                            GameDetector.Game.World => new WorldSolidListReader(),
                            _ => throw new Exception($"Cannot process solid list chunk for game: {_game}")
                        };

                        cd.Resource = solidListReader.ReadSolidList(Reader, chunk.Length);

                        break;
                    }
                    case TexturePackChunk:
                    {
                        TpkManager tpkManager = _game switch
                        {
                            GameDetector.Game.Underground => new Version1Tpk(),
                            GameDetector.Game.Underground2 => new Version2Tpk(),
                            GameDetector.Game.MostWanted => new Version2Tpk(),
                            GameDetector.Game.Carbon => new Version3Tpk(),
                            GameDetector.Game.ProStreet => new Version3Tpk(),
                            GameDetector.Game.ProStreetTest => new Version3Tpk(),
                            GameDetector.Game.Undercover => new Version4Tpk(),
                            GameDetector.Game.World => new Version5Tpk(),
                            _ => throw new Exception($"Cannot process TPK chunk for game: {_game}")
                        };

                        cd.Resource = tpkManager.ReadTexturePack(Reader, chunk.Length);
                        break;
                    }
                    case 0x80034100:
                    {
                        SceneryManager sceneryManager = _game switch
                        {
                            GameDetector.Game.Underground => new UndergroundScenery(),
                            GameDetector.Game.Underground2 => new Underground2Scenery(),
                            GameDetector.Game.MostWanted => new MostWantedScenery(),
                            GameDetector.Game.Carbon => new CarbonScenery(),
                            GameDetector.Game.ProStreet => new ProStreetScenery(),
                            GameDetector.Game.Undercover => new UndercoverScenery(),
                            GameDetector.Game.World => new WorldScenery(),
                            _ => throw new Exception($"Cannot process scenery chunk for game: {_game}")
                        };

                        cd.Resource = sceneryManager.ReadScenery(Reader, chunk.Length);

                        break;
                    }
                    case 0x80135000:
                    {
                        if (_game == GameDetector.Game.World)
                            cd.Resource = new WorldLights().ReadLights(Reader, chunk.Length);
                        else if (_game == GameDetector.Game.Carbon)
                            cd.Resource = new CarbonLights().ReadLights(Reader, chunk.Length);
                        break;
                    }
                    default:
                        // If the chunk is a container chunk, read its sub-chunks.
                        if (chunk.IsParent)
                        {
                            ReadSubChunks(cd.SubChunks, cd.Size);
                        }
                        else
                        {
                            cd.Data = new byte[cd.Size];
                            Reader.Read(cd.Data, 0, cd.Data.Length);
                        }

                        break;
                }

                Chunks.Add(cd);

                SkipChunk(chunk);
            }
        }

        private void ReadSubChunks(ICollection<Chunk> chunkList, uint length)
        {
            var endPos = Stream.Position + length;

            while (Stream.Position < endPos)
            {
                var rawChunk = NextChunk();
                var chunk = new Chunk
                {
                    Id = rawChunk.Type,
                    Offset = rawChunk.Offset,
                    Size = rawChunk.Length,
                    SubChunks = new List<Chunk>()
                };

                if (rawChunk.IsParent)
                    ReadSubChunks(chunk.SubChunks, chunk.Size);
                else
                    chunk.Data = Reader.ReadBytes((int)rawChunk.Length);

                chunkList.Add(chunk);
                SkipChunk(rawChunk);
            }
        }

        /// <summary>
        /// Read chunks from a file.
        /// </summary>
        /// <param name="path"></param>
        public void Read(string path)
        {
            Open(File.OpenRead(path));
        }
    }
}