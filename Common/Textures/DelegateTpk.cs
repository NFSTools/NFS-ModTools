using System;
using System.IO;
using Common.Textures.Data;

namespace Common.Textures
{
    public class DelegateTpk : TpkManager
    {
        private uint _detectedVersion;

        public override TexturePack ReadTexturePack(BinaryReader br, uint containerSize)
        {
            var curPos = br.BaseStream.Position;

            ReadChunks(br, containerSize);

            br.BaseStream.Position = curPos;

            if (_detectedVersion == 5) // MW
            {
                return new MostWantedTpk().ReadTexturePack(br, containerSize);
            }

            if (_detectedVersion == 8) // Carbon, ProStreet
            {
                return new CarbonTpk().ReadTexturePack(br, containerSize);
            }

            if (_detectedVersion == 9) // World
            {
                return new WorldTpk().ReadTexturePack(br, containerSize);
            }

            return new TexturePack
            {
                Name = "Unsupported",
                Hash = 0x12345678,
                PipelinePath = "unsupported.bin",
                TpkOffset = 0,
                TpkSize = 0,
                Version = 0
            };
        }

        protected override void ReadChunks(BinaryReader br, uint containerSize)
        {
            var endPos = br.BaseStream.Position + containerSize;

            while (br.BaseStream.Position < endPos)
            {
                var chunkId = br.ReadUInt32();
                var chunkSize = br.ReadUInt32();
                var chunkEndPos = br.BaseStream.Position + chunkSize;

                if ((chunkId & 0x80000000) == 0x80000000)
                {
                    ReadChunks(br, chunkSize);
                }
                else
                {
                    if (chunkId == 0x33310001)
                    {
                        if (chunkSize != 124)
                            throw new Exception("Invalid TPK info chunk size!");
                        _detectedVersion = br.ReadUInt32();
                    }
                }

                br.BaseStream.Position = chunkEndPos;
            }
        }
    }
}
