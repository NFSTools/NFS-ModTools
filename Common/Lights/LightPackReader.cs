using System;
using System.Collections.Generic;
using System.IO;
using Common.Lights.Data;
using Common.Lights.Structures;

namespace Common.Lights;

public class LightPackReader
{
    private LightPack _lightPack;

    public LightPack ReadLights(BinaryReader br, uint containerSize)
    {
        _lightPack = new LightPack();
        ReadChunks(br, containerSize);
        return _lightPack;
    }

    private void ReadChunks(BinaryReader br, uint containerSize)
    {
        var endPos = br.BaseStream.Position + containerSize;

        while (br.BaseStream.Position < endPos)
        {
            var chunkId = br.ReadUInt32();
            var chunkSize = br.ReadUInt32();
            var chunkEndPos = br.BaseStream.Position + chunkSize;

            chunkSize -= BinaryUtil.AlignReader(br, 0x10);

            switch (chunkId)
            {
                case 0x135001:
                {
                    ReadHeader(br);
                    break;
                }
                case 0x135003:
                {
                    ReadLightList(br, chunkSize);
                    break;
                }
            }

            br.BaseStream.Position = chunkEndPos;
        }
    }

    private void ReadHeader(BinaryReader br)
    {
        var header = BinaryUtil.ReadStruct<LightPackHeader>(br);

        _lightPack.ScenerySectionNumber = header.ScenerySectionNumber;
        _lightPack.Lights = new List<Light>(header.NumLights);
    }

    private void ReadLightList(BinaryReader br, uint chunkSize)
    {
        if (chunkSize % 0x60 != 0)
            throw new Exception("Light list chunk is weirdly sized");
        for (var i = 0; i < chunkSize / 0x60; i++)
        {
            var light = BinaryUtil.ReadStruct<LightData>(br);

            _lightPack.Lights.Add(new Light
            {
                NameHash = light.NameHash,
                Name = light.Name,
                Color = light.Color,
                Position = light.Position,
                Size = light.Size,
                Intensity = light.Intensity,
                FarStart = light.FarStart,
                FarEnd = light.FarEnd,
                Falloff = light.FarEnd
            });
        }
    }
}