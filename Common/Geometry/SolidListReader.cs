using System;
using System.Diagnostics;
using System.IO;
using Common.Geometry.Data;

namespace Common.Geometry;

public abstract class SolidListReader
{
    public SolidList ReadSolidList(BinaryReader br, uint containerSize)
    {
        var solidList = new SolidList();

        ReadChunks(solidList, br, containerSize);

        return solidList;
    }

    private void ReadChunks(SolidList solidList, BinaryReader binaryReader, uint containerSize)
    {
        var readState = ReadState.Init;

        var endPos = binaryReader.BaseStream.Position + containerSize;

        while (binaryReader.BaseStream.Position < endPos)
        {
            var chunkId = binaryReader.ReadUInt32();
            var chunkSize = binaryReader.ReadUInt32();
            var chunkPos = binaryReader.BaseStream.Position;
            var chunkEndPos = chunkPos + chunkSize;

            if (chunkId == 0)
            {
                binaryReader.BaseStream.Position = chunkEndPos;
            }
            else if ((chunkId & 0x80000000) == 0)
            {
                if (readState == ReadState.Headers)
                {
                    // We can return false from a header processor to completely stop reading chunks.
                    if (!ProcessHeaderChunk(solidList, binaryReader, chunkId, chunkSize)) return;
                }
                else
                {
                    throw new Exception("Impossible state reached");
                }

                Debug.Assert(chunkPos <= binaryReader.BaseStream.Position,
                    "chunkPos <= binaryReader.BaseStream.Position");
                Debug.Assert(binaryReader.BaseStream.Position <= chunkEndPos,
                    "binaryReader.BaseStream.Position <= chunkEndPos");

                binaryReader.BaseStream.Position = chunkEndPos;
            }
            else
            {
                switch (chunkId)
                {
                    case 0x80134001:
                        Debug.Assert(readState == ReadState.Init, "readState == ReadState.Init");
                        readState = ReadState.Headers;
                        break;
                    case 0x80134008:
                        Debug.Assert(chunkSize == 0, "chunkSize == 0");
                        break;
                    case 0x80134010:
                        Debug.Assert(readState != ReadState.Init, "readState != ReadState.Init");
                        readState = ReadState.Object;
                        var solidObjectReader = CreateObjectReader();
                        solidList.Objects.Add(solidObjectReader.Read(binaryReader, chunkSize));
                        break;
                    default:
                        throw new InvalidDataException(
                            $"Not sure what to make of parent chunk: 0x{chunkId:X8} @ 0x{chunkPos:X}");
                }
            }
        }
    }

    protected abstract bool ProcessHeaderChunk(SolidList solidList, BinaryReader binaryReader, uint chunkId,
        uint chunkSize);

    protected abstract SolidReader CreateObjectReader();

    private enum ReadState
    {
        Init,
        Headers,
        Object
    }
}