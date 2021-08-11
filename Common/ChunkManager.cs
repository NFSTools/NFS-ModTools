using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Common.Entity.Collision;
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
                Offset = (int)_stream.Position,
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
        [Flags]
        public enum ChunkManagerOptions
        {
            Nothing = 0,
            IgnoreUnknownChunks = 1,
            AutoPadding = 2,
            SkipNull = 4
        }

        public List<ChunkManager.Chunk> Chunks { get; } = new List<ChunkManager.Chunk>();

        private readonly GameDetector.Game _game;
        private readonly ChunkManagerOptions _options;

        public ChunkManager(GameDetector.Game game,
            ChunkManagerOptions options = ChunkManagerOptions.Nothing)
        {
            _game = game;
            _options = options;
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
                    if (Chunks[Chunks.Count - 1].Id == 0)
                    {
                        cd.PrePadding = Chunks[Chunks.Count - 1].Size;
                    }
                }

                while (_br.BaseStream.Position < _br.BaseStream.Length && _br.ReadUInt32() == 0x11111111)
                {
                    padding += 4;
                }

                _br.BaseStream.Position -= 4;

                cd.Padding = padding;

                cd.Id = chunk.Type;
                cd.Size = (uint)(chunk.Length - padding);
                cd.Data = new byte[0];
                cd.Offset = (uint)chunk.Offset;
                cd.SubChunks = new List<Chunk>();

                switch (chunk.Type)
                {
                    case ObjectPackChunk:
                        switch (_game)
                        {
                            case GameDetector.Game.Underground:
                            {
                                cd.Resource = new UndergroundSolids().ReadSolidList(_br, (uint)chunk.Length);
                                break;
                            }
                            case GameDetector.Game.Underground2:
                            {
                                cd.Resource = new Underground2Solids().ReadSolidList(_br, (uint)chunk.Length);
                                break;
                            }
                            case GameDetector.Game.MostWanted:
                            {
                                cd.Resource = new MostWantedSolids().ReadSolidList(_br, (uint)chunk.Length);
                                break;
                            }
                            case GameDetector.Game.Carbon:
                            {
                                cd.Resource = new CarbonSolids().ReadSolidList(_br, (uint)chunk.Length);
                                break;
                            }
                            case GameDetector.Game.Undercover:
                            {
                                cd.Resource = new UndercoverSolids().ReadSolidList(_br, (uint) chunk.Length);
                                break;
                            }
                            case GameDetector.Game.World:
                            {
                                cd.Resource = new World15Solids().ReadSolidList(_br, (uint)chunk.Length);
                                break;
                            }
                        }
                        break;
                    case TexturePackChunk:
                    {
                        cd.Resource = new DelegateTpk().ReadTexturePack(_br, (uint) chunk.Length);
                        break;
                    }
                    case 0x80034100:
                    {
                        switch (_game)
                        {
                            case GameDetector.Game.Underground2:
                                cd.Resource = new Underground2Scenery().ReadScenery(_br, (uint) chunk.Length);
                                break;
                            case GameDetector.Game.MostWanted:
                                cd.Resource = new MostWantedScenery().ReadScenery(_br, (uint) chunk.Length);
                                break;
                            case GameDetector.Game.Carbon:
                                cd.Resource = new CarbonScenery().ReadScenery(_br, (uint) chunk.Length);
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
                //if ((_options & ChunkManagerOptions.AutoPadding) == ChunkManagerOptions.AutoPadding)
                //{
                //    while (_br.ReadUInt32() == 0x11111111)
                //    {
                //        padding += 4;
                //    }
                //}

                //if (chunk.Type == 0x3b801)
                //{
                //    if (_game == GameDetector.Game.MostWanted)
                //    {
                //        Chunks.Add(new Chunk
                //        {
                //            Id = 0x3b801,
                //            Offset = (uint)chunk.Offset,
                //            Resource = new MostWantedCollision().ReadCollisionPack(_br),
                //            SubChunks = new List<Chunk>()
                //        });
                //    }
                //}
                //else if (chunk.Type == ObjectPackChunk)
                //{
                //    if (_game == GameDetector.Game.World)
                //    {
                //        _br.BaseStream.Position = chunk.Offset;

                //        Chunks.Add(new Chunk
                //        {
                //            Id = ObjectPackChunk,
                //            Offset = (uint)chunk.Offset,
                //            Resource = new World15Solids().ReadSolidList(_br, (uint)chunk.Length),
                //            SubChunks = new List<Chunk>()
                //        });
                //    }
                //    else if (_game == GameDetector.Game.MostWanted)
                //    {
                //        _br.BaseStream.Position = chunk.Offset;

                //        Chunks.Add(new Chunk
                //        {
                //            Id = ObjectPackChunk,
                //            Offset = (uint)chunk.Offset,
                //            Resource = new MostWantedSolids().ReadSolidList(_br, (uint)chunk.Length),
                //            SubChunks = new List<Chunk>()
                //        });
                //    }
                //    else if (_game == GameDetector.Game.Carbon)
                //    {
                //        _br.BaseStream.Position = chunk.Offset;

                //        Chunks.Add(new Chunk
                //        {
                //            Id = ObjectPackChunk,
                //            Offset = (uint)chunk.Offset,
                //            Resource = new CarbonSolids().ReadSolidList(_br, (uint)chunk.Length),
                //            SubChunks = new List<Chunk>()
                //        });
                //    }
                //    else if (_game == GameDetector.Game.ProStreet || _game == GameDetector.Game.ProStreetTest)
                //    {
                //        _br.BaseStream.Position = chunk.Offset;

                //        Chunks.Add(new Chunk
                //        {
                //            Id = ObjectPackChunk,
                //            Offset = (uint)chunk.Offset,
                //            Resource = new ProStreetSolids(_game == GameDetector.Game.ProStreetTest)
                //                .ReadSolidList(_br, (uint)chunk.Length),
                //            SubChunks = new List<Chunk>()
                //        });
                //    }
                //    else if (_game == GameDetector.Game.Undercover)
                //    {
                //        _br.BaseStream.Position = chunk.Offset;

                //        Chunks.Add(new Chunk
                //        {
                //            Id = ObjectPackChunk,
                //            Offset = (uint)chunk.Offset,
                //            Resource = new UndercoverSolids().ReadSolidList(_br, (uint)chunk.Length),
                //            SubChunks = new List<Chunk>()
                //        });
                //    }
                //    else if (_game == GameDetector.Game.Underground2)
                //    {
                //        _br.BaseStream.Position = chunk.Offset;

                //        Chunks.Add(new Chunk
                //        {
                //            Id = ObjectPackChunk,
                //            Offset = (uint)chunk.Offset,
                //            Resource = new Underground2Solids().ReadSolidList(_br, (uint)chunk.Length),
                //            SubChunks = new List<Chunk>()
                //        });
                //    }
                //    else if (_game == GameDetector.Game.Underground)
                //    {
                //        _br.BaseStream.Position = chunk.Offset;

                //        Chunks.Add(new Chunk
                //        {
                //            Id = ObjectPackChunk,
                //            Offset = (uint)chunk.Offset,
                //            Resource = new UndergroundSolids().ReadSolidList(_br, (uint)chunk.Length),
                //            SubChunks = new List<Chunk>()
                //        });
                //    }
                //}
                //else if (chunk.Type == TexturePackChunk)
                //{
                //    if (_game == GameDetector.Game.Undercover)
                //    {
                //        Chunks.Add(new Chunk
                //        {
                //            Id = TexturePackChunk,
                //            Offset = (uint)chunk.Offset,
                //            Resource = new UndercoverTpk().ReadTexturePack(_br, (uint)chunk.Length),
                //            SubChunks = new List<Chunk>()
                //        });
                //    }
                //    else
                //    {
                //        Chunks.Add(new Chunk
                //        {
                //            Id = TexturePackChunk,
                //            Offset = (uint)chunk.Offset,
                //            Resource = new DelegateTpk().ReadTexturePack(_br, (uint)chunk.Length),
                //            SubChunks = new List<Chunk>()
                //        });
                //    }
                //}
                //else
                //{
                //    if (chunk.IsParent)
                //    {
                //        if ((_options & ChunkManagerOptions.IgnoreUnknownChunks)
                //            != ChunkManagerOptions.IgnoreUnknownChunks)
                //        {
                //            var master = new Chunk
                //            {
                //                Id = chunk.Type,
                //                Offset = (uint)chunk.Offset,
                //                Size = (uint)chunk.Length,
                //                SubChunks = new List<Chunk>()
                //            };

                //            _br.BaseStream.Position = chunk.Offset + 8;
                //            ReadSubChunks(master.SubChunks, (uint)chunk.Length);

                //            Chunks.Add(master);
                //        }
                //    }
                //    else
                //    {
                //        if (chunk.Type == 0
                //        && (_options & ChunkManagerOptions.SkipNull) != ChunkManagerOptions.SkipNull)
                //        {
                //            Chunks.Add(new Chunk
                //            {
                //                Id = 0,
                //                Data = _br.ReadBytes(chunk.Length).Skip(padding).ToArray(),
                //                Size = (uint)(chunk.Length - padding),
                //                Offset = (uint)chunk.Offset,
                //                SubChunks = new List<Chunk>()
                //            });
                //        }
                //        else
                //        {
                //            if ((_options & ChunkManagerOptions.IgnoreUnknownChunks)
                //                != ChunkManagerOptions.IgnoreUnknownChunks)
                //            {
                //                if ((_options & ChunkManagerOptions.SkipNull) == ChunkManagerOptions.SkipNull)
                //                {
                //                    if (chunk.Type != 0)
                //                    {
                //                        Chunks.Add(new Chunk
                //                        {
                //                            Id = chunk.Type,
                //                            Data = _br.ReadBytes(chunk.Length).Skip(padding).ToArray(),
                //                            Size = (uint)(chunk.Length - padding),
                //                            Offset = (uint)chunk.Offset,
                //                            SubChunks = new List<Chunk>()
                //                        });
                //                    }
                //                }
                //                else
                //                {
                //                    Chunks.Add(new Chunk
                //                    {
                //                        Id = chunk.Type,
                //                        Data = _br.ReadBytes(chunk.Length).Skip(padding).ToArray(),
                //                        Size = (uint)(chunk.Length - padding),
                //                        Offset = (uint)chunk.Offset,
                //                        SubChunks = new List<Chunk>()
                //                    });
                //                }
                //            }
                //        }
                //    }
                //}

                //SkipChunk(chunk);
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
                    var master = new Chunk();
                    master.Id = chunk.Type;
                    master.Offset = (uint)chunk.Offset;
                    master.Size = (uint)chunk.Length;
                    master.SubChunks = new List<Chunk>();

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

                    var child = new Chunk();
                    child.Padding = padding;
                    child.Id = chunk.Type;
                    child.Offset = (uint)chunk.Offset;
                    child.Size = (uint)(chunk.Length - padding);
                    child.SubChunks = new List<Chunk>();
                    child.Data = _br.ReadBytes(chunk.Length).ToArray();

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
