using System;
using System.IO;
using TexEd.Data;

namespace TexEd.Games
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

            throw new NotImplementedException("DelegateTpk does not support this texture pack");
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
                    if (chunkId == 0x33310001 && chunkSize == 124)
                    {
                        _detectedVersion = br.ReadUInt32();
                    }
                }

                br.BaseStream.Position = chunkEndPos;
            }
        }
    }
}
