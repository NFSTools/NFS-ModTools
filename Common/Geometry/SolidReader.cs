using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Common.Geometry.Data;

namespace Common.Geometry;

public abstract class SolidReader
{
    protected uint NumVertices { get; set; }

    protected List<byte[]> VertexBuffers { get; } = new();

    public abstract SolidObject Read(BinaryReader binaryReader, uint containerSize);
}

public abstract class SolidReader<TSolid, TMaterial> : SolidReader
    where TSolid : SolidObject where TMaterial : SolidObjectMaterial
{
    protected TSolid Solid { get; init; }

    public override TSolid Read(BinaryReader binaryReader, uint containerSize)
    {
        var readStateStack = new Stack<(ReadState state, long end)>();
        var endPos = binaryReader.BaseStream.Position + containerSize;

        readStateStack.Push((ReadState.Solid, endPos));

        while (binaryReader.BaseStream.Position < endPos)
        {
            var curReadState = readStateStack.Peek();

            if (binaryReader.BaseStream.Position == curReadState.end)
            {
                readStateStack.Pop();
                curReadState = readStateStack.Peek();
            }
            else if (binaryReader.BaseStream.Position > curReadState.end)
            {
                throw new Exception(
                    $"Top of reader stack ({curReadState.state}) ended at 0x{curReadState.end:X}, but we're at {binaryReader.BaseStream.Position:X}");
            }

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
                switch (curReadState.state)
                {
                    case ReadState.Solid:
                        ProcessSolidChunk(binaryReader, chunkId, chunkSize);
                        break;
                    case ReadState.Plat:
                        ProcessPlatChunk(binaryReader, chunkId, chunkSize);
                        break;
                    default:
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
                    case 0x80134100:
                        Debug.Assert(curReadState.state == ReadState.Solid);
                        readStateStack.Push((ReadState.Plat, chunkEndPos));
                        break;
                    default:
                        throw new InvalidDataException(
                            $"Not sure what to make of parent chunk: 0x{chunkId:X8} @ 0x{chunkPos:X}");
                }
            }
        }

        PostProcessSolid();

        return Solid;
    }

    private void PostProcessSolid()
    {
        var vertexBuffersCount = VertexBuffers.Count;

        // Bookkeeping array: how many vertices are in each vertex buffer?
        var vbCounts = new uint[vertexBuffersCount];

        // Bookkeeping array: how many vertices have been consumed from each vertex buffer?
        var vbOffsets = new uint[vertexBuffersCount];

        // Vertex buffer data readers
        var vbReaders = new BinaryReader[vertexBuffersCount];

        // Vertex arrays
        var vbArrays = new SolidMeshVertex[vertexBuffersCount][];

        // Set up vertex buffer readers
        for (var i = 0; i < vbReaders.Length; i++) vbReaders[i] = new BinaryReader(new MemoryStream(VertexBuffers[i]));

        // Filling in vbCounts...
        if (vertexBuffersCount == 1)
        {
            vbCounts[0] = NumVertices;
        }
        else if (vertexBuffersCount > 1)
        {
            Debug.Assert(Solid.Materials.Any(m => m.NumVerts > 0));
            foreach (var t in Solid.Materials) vbCounts[t.VertexSetIndex] += t.NumVerts;
        }
        else
        {
            return;
        }

        // Diagnostic: find buffers no material claims, and log the stride of ones that are claimed
        /*for (var i = 0; i < vertexBuffersCount; i++)
        {
            if (vbCounts[i] == 0)
            {
                if (VertexBuffers[i].Length > 0)
                    File.AppendAllText("orphaned_vertex_buffers.txt",
                        $"{Solid.Name} | vertexSetIndex={i} | bufferBytes={VertexBuffers[i].Length} | referencedByAnyMaterial=false\n");
                continue;
            }

            var claimedStride = VertexBuffers[i].Length % vbCounts[i] == 0
                ? VertexBuffers[i].Length / vbCounts[i]
                : -1;
            //File.AppendAllText("vertex_buffer_strides.txt",
            //    $"{Solid.Name} | vertexSetIndex={i} | vertCount={vbCounts[i]} | bufferBytes={VertexBuffers[i].Length} | stride={claimedStride}\n");
        }*/

        // Verifying the integrity of our data...
        for (var i = 0; i < vbReaders.Length; i++)
        {
            // To avoid a division-by-zero exception, we allow a vertex buffer to have
            // EITHER 0 vertices OR exactly enough data for a given number of vertices.
            Debug.Assert(vbCounts[i] == 0 || VertexBuffers[i].Length % vbCounts[i] == 0);

            vbArrays[i] = new SolidMeshVertex[vbCounts[i]];
        }

        // Reading vertices from buffers...
        foreach (var solidObjectMaterial in Solid.Materials)
        {
            var vbIndex = solidObjectMaterial.VertexSetIndex;
            var vbStream = vbReaders[vbIndex];
            var vbVertexCount = vbCounts[vbIndex];
            var vbOffset = vbOffsets[vbIndex];
            var vbStride = (int)(vbStream.BaseStream.Length / vbVertexCount);

            var numVerts = solidObjectMaterial.NumVerts == 0
                ? vbVertexCount
                : solidObjectMaterial.NumVerts;

            if (vbOffset < vbVertexCount)
            {
                var vbStartPos = vbStream.BaseStream.Position;

                for (var i = 0; i < numVerts; i++)
                {
                    Debug.Assert(vbStream.BaseStream.Position - vbStartPos == vbStride * i,
                        "vbStream.BaseStream.Position - vbStartPos == vbStride * i");
                    vbArrays[vbIndex][vbOffset + i] = GetVertex(vbStream, (TMaterial)solidObjectMaterial, vbStride);
                }

                vbOffsets[vbIndex] += numVerts;
            }
            else if (vbOffset != vbVertexCount)
            {
                throw new Exception(
                    $"Vertex buffer read is in a weird state. vbOffset={vbOffset} vbVertexCount={vbVertexCount}");
            }
        }

        for (var i = 0; i < vbArrays.Length; i++) ProcessVertices(ref vbArrays[i], i);

#if DEBUG
        foreach (var solidObjectMaterial in Solid.Materials)
        {
            if (!solidObjectMaterial.Indices.Any()) continue;

            var vertexStreamIndex = solidObjectMaterial.VertexSetIndex;
            var meshVertices = vbArrays[vertexStreamIndex];
            var maxReferencedVertex = solidObjectMaterial.Indices.Max();

            Debug.Assert(maxReferencedVertex < meshVertices.Length, "maxReferencedVertex < meshVertices.Length");
        }
#endif

        foreach (var vertexArray in vbArrays) Solid.VertexSets.Add(new List<SolidMeshVertex>(vertexArray));

        // Clean up vertex buffers, which are no longer needed
        VertexBuffers.Clear();
    }

    protected void ReadSolidTextures(BinaryReader binaryReader, uint chunkSize)
    {
        Debug.Assert(chunkSize % 8 == 0, "chunkSize % 8 == 0");
        for (var j = 0; j < chunkSize / 8; j++)
        {
            Solid.TextureHashes.Add(binaryReader.ReadUInt32());
            binaryReader.BaseStream.Position += 4;
        }
    }

    protected void ReadSolidLightMaterials(BinaryReader binaryReader, uint chunkSize)
    {
        // TODO: Do something with this data
        Debug.Assert(chunkSize % 8 == 0, "chunkSize % 8 == 0");
        for (var j = 0; j < chunkSize / 8; j++)
        {
            binaryReader.ReadUInt32();
            binaryReader.BaseStream.Position += 4;
        }
    }


    protected void ReadSolidSmoothVerticesPlat(BinaryReader binaryReader, uint chunkSize)
    {
        Debug.Assert(chunkSize % 12 == 0, "chunkSize % 12 == 0");
    }

    protected void ReadSolidSmoothVertices(BinaryReader binaryReader, uint chunkSize)
    {
        Debug.Assert(chunkSize % 8 == 0, "chunkSize % 8 == 0");
    }

    protected void ReadSolidNormalSmoother(BinaryReader binaryReader)
    {
        binaryReader.BaseStream.Position += 12;
    }

    protected void ReadSolidPositionMarkers(BinaryReader binaryReader, uint chunkSize)
    {
        // TODO: Do something with this data
        chunkSize -= BinaryUtil.AlignReader(binaryReader, 0x10);
        Debug.Assert(chunkSize % 80 == 0, "chunkSize % 80 == 0");
        binaryReader.BaseStream.Position += chunkSize;
    }

    protected abstract void ProcessSolidChunk(BinaryReader binaryReader, uint chunkId,
        uint chunkSize);

    protected abstract void ProcessPlatChunk(BinaryReader binaryReader, uint chunkId,
        uint chunkSize);

    protected abstract SolidMeshVertex GetVertex(BinaryReader reader, TMaterial material, int stride);

    protected abstract void ProcessVertices(ref SolidMeshVertex[] vertices, int vertexStreamId);

    private enum ReadState
    {
        Solid,
        Plat
    }
}