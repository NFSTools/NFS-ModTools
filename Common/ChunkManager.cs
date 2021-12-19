using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Common.Geometry;
using Common.Scenery;
using Common.Textures;

namespace Common
{
    public abstract class RealFile
    {
        protected const uint TexturePackChunk = 0xb3300000;
        protected const uint ObjectPackChunk = 0x80134000;

        protected Stream _stream;
        protected BinaryReader _br;
        protected BinaryWriter _bw;
        protected Stack _chunkStack;

        protected void NextAlignment(int alignment)
        {
            if (_stream.Position % alignment != 0)
            {
                _stream.Position += alignment - _stream.Position % alignment;
            }
        }

        protected void SkipChunk(RealChunk chunk)
        {
            chunk.Skip(_stream);
        }

        protected RealChunk NextChunk()
        {
            var chunk = new RealChunk();
            chunk.Read(_br);
            return chunk;
        }


        protected RealChunk BeginChunk(uint type)
        {
            var chunk = new RealChunk
            {
                Offset = _stream.Position,
                Type = type
            };

            _chunkStack.Push(chunk);
            _stream.Seek(0x8, SeekOrigin.Current);
            return chunk;
        }

        protected void EndChunk()
        {
            if (_chunkStack.Pop() is RealChunk chunk)
            {
                chunk.EndOffset = (int)_stream.Position;
                chunk.Write(_bw);
            }
        }

        public void Open(string filename)
        {
            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            Open(fs);
        }

        public void Save(string filename)
        {
            FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
            _stream = fs;
            _bw = new BinaryWriter(fs);
            _chunkStack = new Stack();
            ProcessSave();
            Close();
        }

        public void Open(Stream stream)
        {
            if (_stream != null)
                Close();

            _stream = stream;
            _stream.Seek(0, SeekOrigin.Begin);
            _br = new BinaryReader(_stream, Encoding.Default, true);
            ProcessOpen();
            Close();
        }

        private void Close()
        {
            //_br?.Close();
            //_bw?.Close();
            _br?.Dispose();
            _bw?.Dispose();
            _chunkStack = null;
            _br = null;
            _bw = null;
            _stream.Dispose();
        }

        protected abstract void ProcessOpen();
        protected abstract void ProcessSave();
    }

    public partial class ChunkManager : RealFile
    {
        public List<Chunk> Chunks { get; } = new();

        private readonly GameDetector.Game _game;

        public ChunkManager(GameDetector.Game game)
        {
            _game = game;
        }

        protected override void ProcessOpen()
        {
            while (_stream.Position < _stream.Length)
            {
                var chunk = NextChunk();

                var padding = 0u;
                var cd = new Chunk();

                if (Chunks.Count > 0)
                {
                    if (Chunks[^1].Id == 0)
                    {
                        cd.PrePadding = Chunks[^1].Size;
                    }
                }

                while (_br.BaseStream.Position < _br.BaseStream.Length && _br.ReadUInt32() == 0x11111111)
                {
                    padding += 4;
                }

                _br.BaseStream.Position -= 4;

                cd.Padding = padding;

                cd.Id = chunk.Type;
                cd.Size = chunk.Length - padding;
                cd.Data = Array.Empty<byte>();
                cd.Offset = chunk.Offset;
                cd.SubChunks = new List<Chunk>();

                switch (chunk.Type)
                {
                    case ObjectPackChunk:
                        switch (_game)
                        {
                            case GameDetector.Game.Underground:
                                {
                                    cd.Resource = new UndergroundSolids().ReadSolidList(_br, chunk.Length);
                                    break;
                                }
                            case GameDetector.Game.Underground2:
                                {
                                    cd.Resource = new Underground2Solids().ReadSolidList(_br, chunk.Length);
                                    break;
                                }
                            case GameDetector.Game.MostWanted:
                                {
                                    cd.Resource = new MostWantedSolids().ReadSolidList(_br, chunk.Length);
                                    break;
                                }
                            case GameDetector.Game.Carbon:
                                {
                                    cd.Resource = new CarbonSolids().ReadSolidList(_br, chunk.Length);
                                    break;
                                }
                            case GameDetector.Game.ProStreet:
                                {
                                    cd.Resource = new ProStreetSolids().ReadSolidList(_br, chunk.Length);
                                    break;
                                }
                            case GameDetector.Game.Undercover:
                                {
                                    cd.Resource = new UndercoverSolids().ReadSolidList(_br, chunk.Length);
                                    break;
                                }
                            case GameDetector.Game.World:
                                {
                                    cd.Resource = new World15Solids().ReadSolidList(_br, chunk.Length);
                                    break;
                                }
                        }
                        break;
                    case TexturePackChunk:
                        {
                            switch (_game)
                            {
                                case GameDetector.Game.Underground:
                                case GameDetector.Game.Underground2:
                                case GameDetector.Game.MostWanted:
                                    cd.Resource = new MostWantedTpk().ReadTexturePack(_br, chunk.Length);
                                    break;
                                case GameDetector.Game.Carbon:
                                case GameDetector.Game.ProStreet:
                                case GameDetector.Game.ProStreetTest:
                                    cd.Resource = new CarbonTpk().ReadTexturePack(_br, chunk.Length);
                                    break;
                                case GameDetector.Game.Undercover:
                                    cd.Resource = new UndercoverTpk().ReadTexturePack(_br, chunk.Length);
                                    break;
                                case GameDetector.Game.World:
                                    cd.Resource = new WorldTpk().ReadTexturePack(_br, chunk.Length);
                                    break;
                                default:
                                    throw new Exception($"Cannot process TPK chunk for game: {_game}");
                            }
                            //cd.Resource = new DelegateTpk().ReadTexturePack(_br, (uint) chunk.Length);
                            break;
                        }
                    case 0x80034100:
                        {
                            switch (_game)
                            {
                                case GameDetector.Game.Underground:
                                    cd.Resource = new UndergroundScenery().ReadScenery(_br, chunk.Length);
                                    break;
                                case GameDetector.Game.Underground2:
                                    cd.Resource = new Underground2Scenery().ReadScenery(_br, chunk.Length);
                                    break;
                                case GameDetector.Game.MostWanted:
                                    cd.Resource = new MostWantedScenery().ReadScenery(_br, chunk.Length);
                                    break;
                                case GameDetector.Game.Carbon:
                                    cd.Resource = new CarbonScenery().ReadScenery(_br, chunk.Length);
                                    break;
                                case GameDetector.Game.ProStreet:
                                    cd.Resource = new ProStreetScenery().ReadScenery(_br, chunk.Length);
                                    break;
                                case GameDetector.Game.Undercover:
                                    cd.Resource = new UndercoverScenery().ReadScenery(_br, chunk.Length);
                                    break;
                                case GameDetector.Game.World:
                                    cd.Resource = new WorldScenery().ReadScenery(_br, chunk.Length);
                                    break;
                            }

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
                            _br.Read(cd.Data, 0, cd.Data.Length);
                        }

                        break;
                }

                Chunks.Add(cd);

                SkipChunk(chunk);
            }
        }

        private void ReadSubChunks(ICollection<Chunk> chunkList, uint length)
        {
            var endPos = _stream.Position + length;

            while (_stream.Position < endPos)
            {
                var chunk = NextChunk();

                if (chunk.IsParent)
                {
                    var master = new Chunk
                    {
                        Id = chunk.Type,
                        Offset = chunk.Offset,
                        Size = chunk.Length,
                        SubChunks = new List<Chunk>()
                    };

                    var padding = 0u;

                    while (_br.ReadUInt32() == 0x11111111)
                    {
                        padding += 4;
                    }

                    _br.BaseStream.Position -= 4;

                    master.Padding = padding;
                    master.Size -= padding;

                    ReadSubChunks(master.SubChunks, master.Size);

                    chunkList.Add(master);
                }
                else
                {
                    var padding = 0u;

                    while (_br.ReadUInt32() == 0x11111111)
                    {
                        padding += 4;
                    }

                    _br.BaseStream.Position -= 4;

                    var child = new Chunk
                    {
                        Padding = padding,
                        Id = chunk.Type,
                        Offset = chunk.Offset,
                        Size = chunk.Length - padding,
                        SubChunks = new List<Chunk>(),
                        Data = _br.ReadBytes(chunk.Length).ToArray()
                    };

                    chunkList.Add(child);
                }

                SkipChunk(chunk);
            }
        }

        protected override void ProcessSave()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Read chunks from a file.
        /// </summary>
        /// <param name="path"></param>
        public void Read(string path)
        {
            Open(path);
        }

        /// <summary>
        /// Read chunks from a binary stream.
        /// </summary>
        /// <param name="br"></param>
        public void Read(BinaryReader br)
        {
            Open(br.BaseStream);
        }

        /// <summary>
        /// Resets the chunk manager.
        /// </summary>
        public void Reset() => Chunks.Clear();
    }
}
