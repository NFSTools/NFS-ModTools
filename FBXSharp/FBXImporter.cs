using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using FBXSharp.Core;
using FBXSharp.Elementary;
using FBXSharp.Objective;
using FBXSharp.ValueTypes;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace FBXSharp
{
    public class FBXImporter
    {
        public struct LoadSettings
        {
            public string DataPath { get; set; }
            public Stream Stream { get; set; }
            public long Position { get; set; }
            public long Length { get; set; }
            public LoadFlags Flags { get; set; }

            public LoadSettings(Stream stream) : this(stream, stream.Position, stream.Length - stream.Position, null)
            {
            }

            public LoadSettings(Stream stream, long position, long length, string dataPath,
                LoadFlags flags = LoadFlags.None)
            {
                DataPath = dataPath;
                Stream = stream;
                Position = position;
                Length = length;
                Flags = flags;
            }
        }

        private struct Cursor
        {
            public readonly long Start;
            public readonly long End;
            public readonly BinaryReader Reader;

            public Cursor(long start, long end, BinaryReader br)
            {
                Start = start;
                End = end;
                Reader = br;
            }
        }

        private ref struct VertexData
        {
            public Geometry Target;
            public Vector3[] Vertices;
            public int[] Indices;
            public bool Triangulate;
        }

        private struct ChannelBuilder<TRead, TRes>
        {
            public delegate bool ToValue(IElementAttribute attribute, out TRead value);

            public delegate bool ToArray(IElementAttribute attribute, out TRead[] array);

            public delegate TRes ToFinal(in TRead value, double weight);

            public Geometry Target;
            public IElement Element;
            public int[] Remapper;
            public int[] Reindexer;

            public Geometry.ChannelType Type;
            public Geometry.ComponentType Size;
            public string Accessor;
            public string Weighter;
            public string Indexers;
            public bool DoExtended;
            public double DefaultW;
            public ToValue ValueGet;
            public ToArray ArrayGet;
            public ToFinal FinalGet;

            public ChannelBuilder<TRead, TRes> New(Geometry geometry, IElement element, int[] remapper, int[] reindexer)
            {
                var result = this;
                result.Target = geometry;
                result.Element = element;
                result.Remapper = remapper;
                result.Reindexer = reindexer;
                return result;
            }
        }

        private static readonly ChannelBuilder<Vector3, Vector4> ms_normalElement = new ChannelBuilder<Vector3, Vector4>
        {
            Accessor = "Normals",
            Weighter = "NormalsW",
            Indexers = "NormalsIndex",
            DoExtended = true,
            DefaultW = 0.0,
            Type = Geometry.ChannelType.Normal,
            Size = Geometry.ComponentType.Double4,
            ValueGet = ElementaryFactory.ToVector3,
            ArrayGet = ElementaryFactory.ToVector3Array,
            FinalGet = ElementaryFactory.ExtendVector
        };

        private static readonly ChannelBuilder<Vector3, Vector4> ms_tangentElement =
            new ChannelBuilder<Vector3, Vector4>
            {
                Accessor = "Tangents",
                Weighter = "TangentsW",
                Indexers = "TangentsIndex",
                DoExtended = true,
                DefaultW = 1.0,
                Type = Geometry.ChannelType.Tangent,
                Size = Geometry.ComponentType.Double4,
                ValueGet = ElementaryFactory.ToVector3,
                ArrayGet = ElementaryFactory.ToVector3Array,
                FinalGet = ElementaryFactory.ExtendVector
            };

        private static readonly ChannelBuilder<Vector3, Vector4> ms_binormalElement =
            new ChannelBuilder<Vector3, Vector4>
            {
                Accessor = "Binormals",
                Weighter = "BinormalsW",
                Indexers = "BinormalsIndex",
                DoExtended = true,
                DefaultW = 1.0,
                Type = Geometry.ChannelType.Binormal,
                Size = Geometry.ComponentType.Double4,
                ValueGet = ElementaryFactory.ToVector3,
                ArrayGet = ElementaryFactory.ToVector3Array,
                FinalGet = ElementaryFactory.ExtendVector
            };

        private static readonly ChannelBuilder<Vector4, Vector4> ms_colorElement = new ChannelBuilder<Vector4, Vector4>
        {
            Accessor = "Colors",
            Indexers = "ColorIndex",
            Type = Geometry.ChannelType.Color,
            Size = Geometry.ComponentType.Double4,
            ValueGet = ElementaryFactory.ToVector4,
            ArrayGet = ElementaryFactory.ToVector4Array
        };

        private static readonly ChannelBuilder<Vector2, Vector2> ms_uvElement = new ChannelBuilder<Vector2, Vector2>
        {
            Accessor = "UV",
            Indexers = "UVIndex",
            Type = Geometry.ChannelType.TexCoord,
            Size = Geometry.ComponentType.Double2,
            ValueGet = ElementaryFactory.ToVector2,
            ArrayGet = ElementaryFactory.ToVector2Array
        };

        private static void BuildGeometryVertexData(in VertexData data, out int[] remapped, out int[] reindexed)
        {
            data.Target.InternalSetVertices(data.Vertices);

            if (data.Triangulate) // inline optimized triangulation
            {
                int[][] indexer;
                var totalCounter = 0;
                var inPolygonIdx = 0;

                for (var i = 0; i < data.Indices.Length; ++i)
                {
                    var reset = false;
                    var index = data.Indices[i];

                    if (index < 0) reset = true;

                    if (inPolygonIdx <= 2)
                        ++totalCounter;
                    else
                        totalCounter += 3;

                    inPolygonIdx = reset ? 0 : inPolygonIdx + 1;
                }

                indexer = new int[totalCounter / 3][];
                remapped = new int[totalCounter];
                reindexed = new int[indexer.Length];

                totalCounter = 0;
                inPolygonIdx = 0;
                var indexer1 = 0;
                var indexer2 = 0;

                for (int i = 0, k = 0, r = 0; i < data.Indices.Length; ++i)
                {
                    var reset = false;
                    var index = data.Indices[i];

                    if (index < 0)
                    {
                        index = ~index;
                        reset = true;
                    }

                    switch (inPolygonIdx)
                    {
                        case 0:
                            indexer1 = index;
                            remapped[totalCounter++] = i;
                            break;

                        case 1:
                            indexer2 = index;
                            remapped[totalCounter++] = i;
                            break;

                        case 2:
                            reindexed[k] = r;
                            indexer[k++] = new[] { indexer1, indexer2, index };
                            remapped[totalCounter++] = i;
                            break;

                        default:
                            reindexed[k] = r;
                            indexer[k++] = new[] { data.Indices[i - inPolygonIdx], data.Indices[i - 1], index };
                            remapped[totalCounter++] = i - inPolygonIdx;
                            remapped[totalCounter++] = i - 1;
                            remapped[totalCounter++] = i;
                            break;
                    }

                    if (reset)
                    {
                        inPolygonIdx = 0;
                        ++r;
                    }
                    else
                    {
                        ++inPolygonIdx;
                    }
                }

                data.Target.InternalSetIndices(indexer);
            }
            else
            {
                var counter = 0;

                for (var i = 0; i < data.Indices.Length; ++i)
                    if (data.Indices[i] < 0)
                        ++counter;

                var indexer = new int[counter][];

                for (int i = 0, k = 0, a = 0; i < data.Indices.Length; ++i)
                    if (data.Indices[i] < 0)
                    {
                        var poly = new int[i - k + 1];

                        for (var m = 0; m < poly.Length - 1; ++m) poly[m] = data.Indices[k + m];

                        poly[i - k] = ~data.Indices[i];

                        indexer[a++] = poly;

                        k = i + 1;
                    }

                data.Target.InternalSetIndices(indexer);

                remapped = new int[data.Indices.Length];
                reindexed = new int[indexer.Length];

                for (var i = 0; i < remapped.Length; ++i) remapped[i] = i;

                for (var i = 0; i < reindexed.Length; ++i) reindexed[i] = i;
            }
        }

        private static void BuildGeometryMaterialData(Geometry geometry, IElement element, int[] reindexed)
        {
            var layerElement = element.FindChild("LayerElementMaterial");

            if (layerElement is null) return;

            var mapping = layerElement.FindChild("MappingInformationType");
            var reference = layerElement.FindChild("ReferenceInformationType");

            if (mapping is null || mapping.Attributes.Length == 0 ||
                mapping.Attributes[0].Type != IElementAttributeType.String)
                throw new Exception($"Geometry {geometry.Name} has no material mapping information type");

            if (reference is null || reference.Attributes.Length == 0 ||
                mapping.Attributes[0].Type != IElementAttributeType.String)
                throw new Exception($"Geometry {geometry.Name} has no material reference information type");

            if (mapping.Attributes[0].GetElementValue().ToString() == "ByPolygon")
            {
                if (reference.Attributes[0].GetElementValue().ToString() != "IndexToDirect")
                    throw new NotSupportedException(
                        $"Geometry {geometry.Name} has not supported {reference.Attributes[0].GetElementValue()} material reference type");

                var materials = layerElement.FindChild("Materials");

                if (materials is null || materials.Attributes.Length == 0)
                    throw new Exception($"Geometry {geometry.Name} has invalid mapping without material indicies");

                if (materials.Attributes[0].Type == IElementAttributeType.ArrayInt32)
                {
                    var temp = materials.Attributes[0].GetElementValue() as int[];

                    if (temp.Length == 0) return; // ok but lmao moment?

                    var array = new int[reindexed.Length];

                    for (var i = 0; i < array.Length; ++i) array[i] = temp[reindexed[i]];

                    var value = -1;
                    var count = 0;

                    for (var i = 0; i < array.Length; ++i)
                        if (array[i] != value)
                        {
                            ++count;
                            value = array[i];
                        }

                    var subMeshes = new Geometry.SubMesh[count];
                    var currIndex = 0;

                    value = array[0];
                    count = 0;

                    for (var i = 0; i < array.Length; ++i)
                        if (array[i] != value)
                        {
                            subMeshes[count++] = new Geometry.SubMesh(currIndex, i - currIndex, value);
                            value = array[i];
                            currIndex = i;
                        }

                    subMeshes[count] = new Geometry.SubMesh(currIndex, array.Length - currIndex, value);

                    geometry.InternalSetSubMeshes(subMeshes);
                }
                else if (materials.Attributes[0].Type == IElementAttributeType.ArrayInt64)
                {
                    var temp = materials.Attributes[0].GetElementValue() as long[];

                    if (temp.Length == 0) return; // ok but lmao moment?

                    var array = new int[reindexed.Length];

                    for (var i = 0; i < array.Length; ++i) array[i] = (int)temp[reindexed[i]];

                    var value = -1;
                    var count = 0;

                    for (var i = 0; i < array.Length; ++i)
                        if (array[i] != value)
                        {
                            ++count;
                            value = array[i];
                        }

                    var subMeshes = new Geometry.SubMesh[count];
                    var currIndex = 0;

                    value = array[0];
                    count = 0;

                    for (var i = 0; i < array.Length; ++i)
                        if (array[i] != value)
                        {
                            subMeshes[count++] = new Geometry.SubMesh(currIndex, i - currIndex, value);
                            value = array[i];
                            currIndex = i;
                        }

                    subMeshes[count] = new Geometry.SubMesh(currIndex, array.Length - currIndex, value);

                    geometry.InternalSetSubMeshes(subMeshes);
                }
                else
                {
                    throw new Exception(
                        $"Geometry {geometry.Name} has invalid {materials.Attributes[0].Type} index mapping type");
                }

                return;
            }

            if (mapping.Attributes[0].GetElementValue().ToString() == "AllSame")
            {
                if (reference.Attributes[0].GetElementValue().ToString() != "IndexToDirect")
                    throw new NotSupportedException(
                        $"Geometry {geometry.Name} has not supported {reference.Attributes[0].GetElementValue()} material reference type");

                var materials = layerElement.FindChild("Materials");

                if (materials is null || materials.Attributes.Length == 0)
                {
                    geometry.InternalSetSubMeshes(new[]
                    {
                        new Geometry.SubMesh(0, geometry.Indices.Length, 0)
                    });
                }
                else
                {
                    int index;

                    switch (materials.Attributes[0].Type)
                    {
                        case IElementAttributeType.Int16:
                            index = (short)materials.Attributes[0].GetElementValue();
                            break;

                        case IElementAttributeType.Int32:
                            index = (int)materials.Attributes[0].GetElementValue();
                            break;

                        case IElementAttributeType.Int64:
                            index = (int)(long)materials.Attributes[0].GetElementValue();
                            break;

                        case IElementAttributeType.ArrayInt32:
                            index = (materials.Attributes[0].GetElementValue() as int[])[0];
                            break;

                        case IElementAttributeType.ArrayInt64:
                            index = (int)(materials.Attributes[0].GetElementValue() as long[])[0];
                            break;

                        default:
                            throw new Exception(
                                $"Material indexing type is of invalid {materials.Attributes[0].Type} type");
                    }

                    geometry.InternalSetSubMeshes(new[]
                    {
                        new Geometry.SubMesh(0, geometry.Indices.Length, index)
                    });
                }

                return;
            }

            throw new NotSupportedException(
                $"Geometry {geometry.Name} has not supported {mapping.Attributes[0].GetElementValue()} material mapping type");
        }

        private static void BuildGeometryChannelData<TRead, TRes>(in ChannelBuilder<TRead, TRes> builder)
        {
            if (builder.Element.Attributes.Length == 0)
                throw new Exception($"Geometry {builder.Target.Name} has no {builder.Accessor} layer specifier");

            var layer = Convert.ToInt32(builder.Element.Attributes[0].GetElementValue());
            var naming = builder.Element.FindChild("Name");
            var mapping = builder.Element.FindChild("MappingInformationType");
            var reference = builder.Element.FindChild("ReferenceInformationType");

            if (mapping is null || mapping.Attributes.Length == 0 ||
                mapping.Attributes[0].Type != IElementAttributeType.String)
                throw new Exception(
                    $"Geometry {builder.Target.Name} has no {builder.Accessor} mapping information type");

            if (reference is null || reference.Attributes.Length == 0 ||
                mapping.Attributes[0].Type != IElementAttributeType.String)
                throw new Exception(
                    $"Geometry {builder.Target.Name} has no {builder.Accessor} reference information type");

            var mappingType = mapping.Attributes[0].GetElementValue().ToString();
            var referenceType = reference.Attributes[0].GetElementValue().ToString();

            if (referenceType == "Direct")
            {
                var direct = builder.Element.FindChild(builder.Accessor);
                var extent = builder.Element.FindChild(builder.Weighter);

                if (direct is null || direct.Attributes.Length == 0)
                    throw new Exception($"Geometry {builder.Target.Name} has no {builder.Accessor} direct accessor");

                if (mappingType == "ByPolygonVertex")
                {
                    if (!builder.ArrayGet.Invoke(direct.Attributes[0], out var array))
                        throw new Exception($"Unable to parse vector array in geometry {builder.Target.Name}");

                    if (array.Length == 0) return;

                    var buffer = new TRead[builder.Remapper.Length];

                    for (var i = 0; i < buffer.Length; ++i) buffer[i] = array[builder.Remapper[i]];

                    if (!builder.DoExtended || extent is null)
                    {
                        FinalizeChannel(builder, buffer, null);
                    }
                    else
                    {
                        if (!ElementaryFactory.ToDoubleArray(extent.Attributes[0], out var fills))
                            throw new Exception($"Unable to parse weight array in geometry {builder.Target.Name}");

                        var weight = new double[builder.Remapper.Length];

                        for (var i = 0; i < weight.Length; ++i) weight[i] = fills[builder.Remapper[i]];

                        FinalizeChannel(builder, buffer, weight);
                    }

                    return;
                }

                if (mappingType == "ByVertice" || mappingType == "ByVertex")
                {
                    if (!builder.ArrayGet.Invoke(direct.Attributes[0], out var array))
                        throw new Exception($"Unable to parse vector array in geometry {builder.Target.Name}");

                    if (array.Length == 0) return;

                    var buffer = new TRead[builder.Remapper.Length];

                    for (int i = 0, r = 0; i < builder.Target.Indices.Length; ++i)
                    {
                        var indices = builder.Target.Indices[i];

                        for (var k = 0; k < indices.Length; ++k) buffer[r++] = array[indices[k]];
                    }

                    if (!builder.DoExtended || extent is null)
                    {
                        FinalizeChannel(builder, buffer, null);
                    }
                    else
                    {
                        if (!ElementaryFactory.ToDoubleArray(extent.Attributes[0], out var fills))
                            throw new Exception($"Unable to parse weight array in geometry {builder.Target.Name}");

                        var weight = new double[builder.Remapper.Length];

                        for (int i = 0, r = 0; i < builder.Target.Indices.Length; ++i)
                        {
                            var indices = builder.Target.Indices[i];

                            for (var k = 0; k < indices.Length; ++k) weight[r++] = fills[indices[k]];
                        }

                        FinalizeChannel(builder, buffer, weight);
                    }

                    return;
                }

                if (mappingType == "ByPolygon")
                {
                    if (!builder.ArrayGet.Invoke(direct.Attributes[0], out var array))
                        throw new Exception($"Unable to parse vector array in geometry {builder.Target.Name}");

                    if (array.Length == 0) return;

                    var buffer = new TRead[builder.Remapper.Length];

                    for (int i = 0, r = 0; i < builder.Target.Indices.Length; ++i)
                    {
                        var length = builder.Target.Indices[i].Length;
                        var vector = array[builder.Reindexer[i]];

                        for (var k = 0; k < length; ++k) buffer[r++] = vector;
                    }

                    if (!builder.DoExtended || extent is null)
                    {
                        FinalizeChannel(builder, buffer, null);
                    }
                    else
                    {
                        if (!ElementaryFactory.ToDoubleArray(extent.Attributes[0], out var fills))
                            throw new Exception($"Unable to parse weight array in geometry {builder.Target.Name}");

                        var weight = new double[builder.Remapper.Length];

                        for (int i = 0, r = 0; i < builder.Target.Indices.Length; ++i)
                        {
                            var length = builder.Target.Indices[i].Length;
                            var filler = fills[builder.Reindexer[i]];

                            for (var k = 0; k < length; ++k) weight[r++] = filler;
                        }

                        FinalizeChannel(builder, buffer, weight);
                    }

                    return;
                }

                if (mappingType == "AllSame")
                {
                    if (!builder.ValueGet.Invoke(direct.Attributes[0], out var vector))
                        throw new Exception($"Unable to get first vector value in geometry {builder.Target.Name}");

                    var buffer = new TRead[builder.Remapper.Length];

                    for (var i = 0; i < buffer.Length; ++i) buffer[i] = vector;

                    if (!builder.DoExtended || extent is null)
                    {
                        FinalizeChannel(builder, buffer, null);
                    }
                    else
                    {
                        if (!ElementaryFactory.ToDouble(extent.Attributes[0], out var filler))
                            throw new Exception($"Unable to parse weight array in geometry {builder.Target.Name}");

                        var weight = new double[builder.Remapper.Length];

                        for (var i = 0; i < weight.Length; ++i) weight[i] = filler;

                        FinalizeChannel(builder, buffer, weight);
                    }

                    return;
                }

                throw new NotSupportedException(
                    $"Geometry {builder.Target.Name} uses unsupported {mappingType} mapping type with Direct reference type");
            }

            if (referenceType == "IndexToDirect")
            {
                var direct = builder.Element.FindChild(builder.Accessor);
                var indexs = builder.Element.FindChild(builder.Indexers);
                var extent = builder.Element.FindChild(builder.Weighter);

                if (direct is null || direct.Attributes.Length == 0)
                    throw new Exception($"Geometry {builder.Target.Name} has no {builder.Accessor} direct accessor");

                if (indexs is null || indexs.Attributes.Length == 0)
                    throw new Exception($"Geometry {builder.Target.Name} has no {builder.Indexers} indexer accessor");

                if (mappingType == "ByPolygonVertex")
                {
                    if (!builder.ArrayGet.Invoke(direct.Attributes[0], out var array))
                        throw new Exception($"Unable to parse vector array in geometry {builder.Target.Name}");

                    if (!ElementaryFactory.ToInt32Array(indexs.Attributes[0], out var indexer))
                        throw new Exception($"Unable to parse index array in geometry {builder.Target.Name}");

                    if (array.Length == 0 || indexer.Length == 0) return;

                    var buffer = new TRead[builder.Remapper.Length];

                    for (var i = 0; i < buffer.Length; ++i) buffer[i] = array[indexer[builder.Remapper[i]]];

                    if (!builder.DoExtended || extent is null)
                    {
                        FinalizeChannel(builder, buffer, null);
                    }
                    else
                    {
                        if (!ElementaryFactory.ToDoubleArray(extent.Attributes[0], out var fills))
                            throw new Exception($"Unable to parse weight array in geometry {builder.Target.Name}");

                        var weight = new double[builder.Remapper.Length];

                        for (var i = 0; i < buffer.Length; ++i) weight[i] = fills[indexer[builder.Remapper[i]]];

                        FinalizeChannel(builder, buffer, weight);
                    }

                    return;
                }

                if (mappingType == "AllSame")
                {
                    if (!builder.ArrayGet.Invoke(direct.Attributes[0], out var array))
                        throw new Exception($"Unable to parse vector array in geometry {builder.Target.Name}");

                    if (!ElementaryFactory.ToInt32(indexs.Attributes[0], out var index))
                        throw new Exception($"Unable to get first index value in geometry {builder.Target.Name}");

                    if (index < 0 || index >= array.Length) return;

                    var buffer = new TRead[builder.Remapper.Length];

                    for (var i = 0; i < buffer.Length; ++i) buffer[i] = array[index];

                    if (!builder.DoExtended || extent is null)
                    {
                        FinalizeChannel(builder, buffer, null);
                    }
                    else
                    {
                        if (!ElementaryFactory.ToDoubleArray(extent.Attributes[0], out var fills))
                            throw new Exception($"Unable to parse weight array in geometry {builder.Target.Name}");

                        var weight = new double[builder.Remapper.Length];

                        for (var i = 0; i < weight.Length; ++i) weight[i] = fills[index];

                        FinalizeChannel(builder, buffer, weight);
                    }
                }

                throw new NotSupportedException(
                    $"Geometry {builder.Target.Name} uses unsupported {mappingType} mapping type with IndexToDirect reference type");
            }

            throw new NotSupportedException(
                $"Geometry {builder.Target.Name} has unsupported {referenceType} {builder.Accessor} reference type");

            void FinalizeChannel(in ChannelBuilder<TRead, TRes> channel, TRead[] input, double[] weights)
            {
                var name = string.Empty;

                if (!(naming is null) && naming.Attributes.Length > 0 &&
                    naming.Attributes[0].Type == IElementAttributeType.String)
                    name = naming.Attributes[0].GetElementValue().ToString();

                if (!channel.DoExtended)
                {
                    channel.Target.InternalSetChannel(new Geometry.Channel(layer, name, channel.Type, channel.Size,
                        input));
                }
                else
                {
                    var expand = new TRes[input.Length];

                    if (weights is null)
                        for (var i = 0; i < expand.Length; ++i)
                            expand[i] = channel.FinalGet.Invoke(input[i], channel.DefaultW);
                    else
                        for (var i = 0; i < expand.Length; ++i)
                            expand[i] = channel.FinalGet.Invoke(input[i], weights[i]);

                    channel.Target.InternalSetChannel(new Geometry.Channel(layer, name, channel.Type, channel.Size,
                        expand));
                }
            }
        }

        private static void RemapAndMergeSubMeshes(Geometry geometry)
        {
            var subMeshIndices = new (int index, int material)[geometry.SubMeshes.Length];

            for (var i = 0; i < geometry.SubMeshes.Length; ++i)
                subMeshIndices[i] = (i, geometry.SubMeshes[i].MaterialIndex);

            Array.Sort(subMeshIndices, (x, y) =>
            {
                var comparer = x.material.CompareTo(y.material);

                if (comparer == 0) return x.index.CompareTo(y.index);

                return comparer;
            });

            var sortedIndices = new int[geometry.Indices.Length][];
            var sortedSubMesh = new Geometry.SubMesh[geometry.SubMeshes.Length];

            for (int i = 0, pos = 0; i < subMeshIndices.Length; ++i)
            {
                var subinfo = subMeshIndices[i];
                var subMesh = geometry.SubMeshes[subinfo.index];

                Array.Copy(geometry.Indices, subMesh.PolygonStart, sortedIndices, pos, subMesh.PolygonCount);

                sortedSubMesh[i] = new Geometry.SubMesh(pos, subMesh.PolygonCount, subinfo.material);

                pos += subMesh.PolygonCount;
            }

            var grouppings = sortedSubMesh.GroupBy(_ => _.MaterialIndex).ToArray();
            var newSubMesh = new Geometry.SubMesh[grouppings.Length];
            var newChannel = new Geometry.Channel[geometry.Channels.Count];

            for (int i = 0, k = 0; i < newSubMesh.Length; ++i)
            {
                var count = grouppings[i].Sum(_ => _.PolygonCount);

                newSubMesh[i] = new Geometry.SubMesh(k, count, grouppings[i].Key);

                k += count;
            }

            for (var i = 0; i < geometry.Channels.Count; ++i)
            {
                var channel = geometry.Channels[i];
                var temparr = new object[channel.Buffer.Length];

                Array.Copy(channel.Buffer, temparr, temparr.Length);

                for (var j = 0; j < sortedSubMesh.Length; ++j)
                {
                    var subMeshDst = sortedSubMesh[j];
                    var subMeshSrc = geometry.SubMeshes[subMeshIndices[j].index];

                    var startDst = sortedIndices.SumTo(subMeshDst.PolygonStart);
                    var startSrc = geometry.Indices.SumTo(subMeshSrc.PolygonStart);

#if DEBUG
                    Debug.Assert(subMeshDst.PolygonCount == subMeshSrc.PolygonCount);
#endif

                    for (int k = 0, pos = 0; k < subMeshDst.PolygonCount; ++k)
                    {
                        var length = sortedIndices[subMeshDst.PolygonStart + k].Length;

#if DEBUG
                        var copair = geometry.Indices[subMeshSrc.PolygonStart + k].Length;

                        Debug.Assert(length == copair);
#endif

                        for (var f = 0; f < length; ++f, ++pos)
                            channel.Buffer.SetValue(temparr[startSrc + pos], startDst + pos);
                    }
                }
            }

            geometry.InternalSetIndices(sortedIndices);
            geometry.InternalSetSubMeshes(newSubMesh);
        }

        private static float GetRealFrameRate(FrameRate rate, float custom)
        {
            switch (rate)
            {
                case FrameRate.RateDefault: return 14.0f;
                case FrameRate.Rate120: return 120.0f;
                case FrameRate.Rate100: return 100.0f;
                case FrameRate.Rate60: return 60.0f;
                case FrameRate.Rate50: return 50.0f;
                case FrameRate.Rate48: return 48.0f;
                case FrameRate.Rate30: return 30.0f;
                case FrameRate.Rate30Drop: return 30.0f;
                case FrameRate.RateNTSCDropFrame: return 29.9700262f;
                case FrameRate.RateNTSCFullFrame: return 29.9700262f;
                case FrameRate.RatePAL: return 25.0f;
                case FrameRate.RateCinema: return 24.0f;
                case FrameRate.Rate1000: return 1000.0f;
                case FrameRate.RateCinemaND: return 23.976f;
                case FrameRate.RateCustom: return custom;
                default: return 0.0f;
            }
        }

        private static void InternalPrepareGeometry(IElement element, Geometry geometry, LoadFlags flags)
        {
            var vertices = element.FindChild("Vertices");

            if (vertices is null || vertices.Attributes.Length == 0) return;

            var indices = element.FindChild("PolygonVertexIndex");

            if (indices is null || indices.Attributes.Length == 0)
                throw new Exception($"Geometry {geometry.Name} is missing index buffer");

            if (!ElementaryFactory.ToVector3Array(vertices.Attributes[0], out var vertexBuffer))
                throw new Exception($"Unable to parse vertex buffer for geometry {geometry.Name}");

            if (!ElementaryFactory.ToInt32Array(indices.Attributes[0], out var indexBuffer))
                throw new Exception($"Unable to parse index buffer for geometry {geometry.Name}");

            BuildGeometryVertexData(new VertexData
            {
                Target = geometry,
                Vertices = vertexBuffer,
                Indices = indexBuffer,
                Triangulate = (flags & LoadFlags.Triangulate) != 0
            }, out var remapped, out var reindexed);

            BuildGeometryMaterialData(geometry, element, reindexed);

            for (var i = 0; i < element.Children.Length; ++i)
            {
                var child = element.Children[i];

                switch (child.Name)
                {
                    case "LayerElementNormal":
                        BuildGeometryChannelData(ms_normalElement.New(geometry, child, remapped, reindexed));
                        break;
                    case "LayerElementTangent":
                        BuildGeometryChannelData(ms_tangentElement.New(geometry, child, remapped, reindexed));
                        break;
                    case "LayerElementBinormal":
                        BuildGeometryChannelData(ms_binormalElement.New(geometry, child, remapped, reindexed));
                        break;
                    case "LayerElementColor":
                        BuildGeometryChannelData(ms_colorElement.New(geometry, child, remapped, reindexed));
                        break;
                    case "LayerElementUV":
                        BuildGeometryChannelData(ms_uvElement.New(geometry, child, remapped, reindexed));
                        break;
                }
            }

            geometry.InternalSortChannels();

            if ((flags & LoadFlags.RemapSubmeshes) != 0 && geometry.SubMeshes.Length > 1)
                RemapAndMergeSubMeshes(geometry);
        }

        private static void InternalPreloadClipData(IScene scene, LoadFlags flags, string fbxPath)
        {
            if ((flags & LoadFlags.LoadVideoFiles) == 0) return;

            if (string.IsNullOrWhiteSpace(fbxPath)) return;

            if (Path.HasExtension(fbxPath)) fbxPath = Path.GetDirectoryName(fbxPath);

            foreach (var clip in scene.GetObjectsOfType<Clip>())
                if (clip.Content.Length == 0)
                {
                    var file = Path.GetFullPath(Path.Combine(fbxPath, clip.RelativePath));

                    if (File.Exists(file)) clip.SetContent(File.ReadAllBytes(file));
                }
        }

        private static void ParseTemplates(Element root, IScene scene)
        {
            var definitions = root.FindChild("Definitions");

            if (definitions is null) return;

            for (var i = 0; i < definitions.Children.Length; ++i)
            {
                var child = definitions.Children[i];

                if (child.Name != "ObjectType") continue;

                var property = child.FindChild("PropertyTemplate");

                if (child.Attributes.Length == 0 || child.Attributes[0].Type != IElementAttributeType.String ||
                    property is null) continue;

                if (Enum.TryParse(child.Attributes[0].GetElementValue().ToString(), out FBXClassType type))
                    scene.CreateEmptyTemplate(type, TemplateCreationType.NewOverrideAnyExisting).Initialize(property);
            }
        }

        private static void ParseTakeInfos(Element root, IScene scene)
        {
            var takes = root.FindChild("Takes");

            if (takes is null) return;

            for (var i = 0; i < takes.Children.Length; ++i)
            {
                var take = takes.Children[i];

                if (take is null) throw new Exception($"Take at index {i} is null");

                if (take.Name != "Take") continue;

                if (take.Attributes.Length == 0) throw new Exception($"Take at index {i} has 0 properties");

                var nameAttribute = take.Attributes[0];

                if (nameAttribute.Type != IElementAttributeType.String)
                    throw new Exception($"Take {i} property is not of string type");

                var nameProp = nameAttribute.GetElementValue().ToString();
                var filename = string.Empty;

                var localFrom = 0.0;
                var localTo = 0.0;

                var refFrom = 0.0;
                var refTo = 0.0;

                var fileElement = take.FindChild("FileName");
                var localTime = take.FindChild("LocalTime");
                var refTime = take.FindChild("ReferenceTime");

                if (!(filename is null) && fileElement.Attributes.Length >= 1)
                {
                    var attribute = fileElement.Attributes[0];

                    if (attribute.Type != IElementAttributeType.String)
                        throw new Exception($"Take {i} has invalid filename");

                    filename = attribute.GetElementValue().ToString();
                }

                if (!(localTime is null) && localTime.Attributes.Length >= 2)
                {
                    var attribute1 = localTime.Attributes[0];
                    var attribute2 = localTime.Attributes[1];

                    if (attribute1.Type != IElementAttributeType.Int64)
                        throw new Exception($"Take {i} has invalid local time from");

                    if (attribute2.Type != IElementAttributeType.Int64)
                        throw new Exception($"Take {i} has invalid local time to");

                    localFrom = MathExtensions.FBXTimeToSeconds((long)attribute1.GetElementValue());
                    localTo = MathExtensions.FBXTimeToSeconds((long)attribute2.GetElementValue());
                }

                if (!(refTime is null) && refTime.Attributes.Length >= 2)
                {
                    var attribute1 = refTime.Attributes[0];
                    var attribute2 = refTime.Attributes[1];

                    if (attribute1.Type != IElementAttributeType.Int64)
                        throw new Exception($"Take {i} has invalid ref time from");

                    if (attribute2.Type != IElementAttributeType.Int64)
                        throw new Exception($"Take {i} has invalid ref time to");

                    refFrom = MathExtensions.FBXTimeToSeconds((long)attribute1.GetElementValue());
                    refTo = MathExtensions.FBXTimeToSeconds((long)attribute2.GetElementValue());
                }

                scene.AddTakeInfo(new TakeInfo(nameProp, filename, localFrom, localTo, refFrom, refTo));
            }
        }

        private static IEnumerable<Connection> ParseConnections(Element root)
        {
            var connections = root.FindChild("Connections");

            if (connections is null) yield break;

            for (var i = 0; i < connections.Children.Length; ++i)
            {
                var connection = connections.Children[i];

                if (connection is null || connection.Attributes.Length < 3)
                    throw new Exception($"Connection {i} is null or has less than 3 valid properties");

                var p1st = connection.Attributes[0];
                var p2nd = connection.Attributes[1];
                var p3rd = connection.Attributes[2];

                if (p1st is null || p2nd is null || p3rd is null)
                    throw new Exception($"Connection {i} property is null or invalid");

                if (p1st.Type != IElementAttributeType.String ||
                    p2nd.Type != IElementAttributeType.Int64 ||
                    p3rd.Type != IElementAttributeType.Int64)
                    throw new Exception($"Connection {i} property is of invalid type");

                var start = (long)p2nd.GetElementValue();
                var final = (long)p3rd.GetElementValue();

                switch (p1st.GetElementValue().ToString())
                {
                    case "":
                    {
                        break;
                    }

                    case "OO":
                    {
                        yield return new Connection(Connection.ConnectionType.Object, start, final);
                        break;
                    }

                    case "OP":
                    {
                        if (connection.Attributes.Length < 4)
                            throw new Exception($"Encountered OP connection {i} without fourth property");

                        yield return new Connection(Connection.ConnectionType.Property, start, final,
                            connection.Attributes[3]);
                        break;
                    }

                    default:
                        throw new Exception($"Not supported connection {i} type {p1st.GetElementValue()}");
                }
            }
        }

        private static T ParseObjects<T>(Element root, Connection[] connections, LoadFlags flags)
            where T : IScene, new()
        {
            var scene = new T();

            var allObjs = root.FindChild("Objects");

            if (allObjs is null) return scene;

            var element = new Dictionary<long, IElement>
            {
                [0] = root
            };
            var objects = new Dictionary<long, FBXObject>
            {
                [0] = scene.Root
            };

            for (var i = 0; i < allObjs.Children.Length; ++i)
            {
                var child = allObjs.Children[i];

                if (child is null || child.Attributes.Length == 0)
                    throw new Exception($"Root child {i} is either null or has 0 properties");

                var attribute = child.Attributes[0];

                if (attribute.Type != IElementAttributeType.Int64)
                    throw new Exception($"Root child {i} does not have ID as the first property");

                element[(long)attribute.GetElementValue()] = child;
            }

            foreach (var pair in element)
            {
                if (ReferenceEquals(pair.Value, root)) continue;

                if (Enum.TryParse(pair.Value.Name, out FBXClassType classType))
                {
                    if (classType == FBXClassType.Geometry && (flags & LoadFlags.IgnoreGeometry) != 0) continue;

                    if (classType == FBXClassType.Deformer && (flags & LoadFlags.IgnoreBlendShapes) != 0) continue;

                    if (pair.Value.Attributes.Length < 3 ||
                        pair.Value.Attributes[2].Type != IElementAttributeType.String) continue;

                    if (!Enum.TryParse(pair.Value.Attributes[2].GetElementValue().ToString(),
                            out FBXObjectType objectType))
                        if (!Enum.TryParse(pair.Value.Name, out objectType))
                            continue;

                    var @object = scene.CreateFBXObject(classType, objectType, pair.Value);

                    if (@object is null) continue;

                    if (@object is Geometry geometry)
                        InternalPrepareGeometry(pair.Value, geometry, flags); // special case for inbuilt geometry class

                    objects[pair.Key] = @object;
                }
            }

            foreach (var connection in connections)
                if (objects.TryGetValue(connection.Source, out var src) &&
                    objects.TryGetValue(connection.Destination, out var dst))
                {
                    dst.NumConnections++;
                    dst.ResolveLink(src, connection.Property);
                }

            foreach (var bindPose in
                     scene.GetObjectsOfType<BindPose>())
                bindPose.InternalResolveAllBinds(objects); // special bind resolve for inbuilt poses

            var settings = root.FindChild("GlobalSettings");

            if (!(settings is null)) scene.Settings.InternalFillWithElement(settings);

            return scene;
        }

        private static long ReadElementOffset(in Cursor cursor, uint version)
        {
            return version >= 7500 ? cursor.Reader.ReadInt64() : cursor.Reader.ReadInt32();
        }

        private static T[] ReadArrayAttribute<T>(in Cursor cursor) where T : unmanaged
        {
            var length = cursor.Reader.ReadInt32();
            var encode = cursor.Reader.ReadInt32();
            var buffer = cursor.Reader.ReadBytes(cursor.Reader.ReadInt32());
            var result = new T[length];

            if (length == 0) return result;

            if (encode == 0)
                unsafe
                {
                    fixed (T* ptr = &result[0])
                    {
                        Marshal.Copy(buffer, 0, new IntPtr(ptr), buffer.Length);
                    }
                }
            else
                unsafe
                {
                    var inflater = new Inflater();
                    var inflated = new byte[length * sizeof(T)];

                    inflater.SetInput(buffer);

                    var output = inflater.Inflate(inflated);

                    if (output == 0) throw new Exception("Unable to decompress a zlib-compressed byte buffer");

                    fixed (T* ptr = &result[0])
                    {
                        Marshal.Copy(inflated, 0, new IntPtr(ptr), inflated.Length);
                    }
                }

            return result;
        }

        private static IElementAttribute ReadAttribute(in Cursor cursor)
        {
            var etype = (IElementAttributeType)cursor.Reader.ReadByte();

            switch (etype)
            {
                case IElementAttributeType.Byte:
                    return new ByteAttribute(cursor.Reader.ReadByte());

                case IElementAttributeType.Int16:
                    return new Int16Attribute(cursor.Reader.ReadInt16());

                case IElementAttributeType.Int32:
                    return new Int32Attribute(cursor.Reader.ReadInt32());

                case IElementAttributeType.Int64:
                    return new Int64Attribute(cursor.Reader.ReadInt64());

                case IElementAttributeType.Single:
                    return new SingleAttribute(cursor.Reader.ReadSingle());

                case IElementAttributeType.Double:
                    return new DoubleAttribute(cursor.Reader.ReadDouble());

                case IElementAttributeType.String:
                    return new StringAttribute(cursor.Reader.ReadNullTerminated(cursor.Reader.ReadInt32()));

                case IElementAttributeType.Binary:
                    return new BinaryAttribute(cursor.Reader.ReadBytes(cursor.Reader.ReadInt32()));

                case IElementAttributeType.ArrayBoolean:
                    return new ArrayBooleanAttribute(ReadArrayAttribute<bool>(cursor));

                case IElementAttributeType.ArrayInt32:
                    return new ArrayInt32Attribute(ReadArrayAttribute<int>(cursor));

                case IElementAttributeType.ArrayInt64:
                    return new ArrayInt64Attribute(ReadArrayAttribute<long>(cursor));

                case IElementAttributeType.ArraySingle:
                    return new ArraySingleAttribute(ReadArrayAttribute<float>(cursor));

                case IElementAttributeType.ArrayDouble:
                    return new ArrayDoubleAttribute(ReadArrayAttribute<double>(cursor));

                default:
                    throw new Exception(
                        $"Unknown element property type {(byte)etype} at offset 0x{cursor.Reader.BaseStream.Position - 1:X8}");
            }
        }

        private static Element ReadElement(in Cursor cursor, uint version)
        {
            var endOffset = ReadElementOffset(cursor, version);

            if (endOffset == 0) return null;

            var propCount = (int)ReadElementOffset(cursor, version);
#pragma warning disable IDE0059 // Unnecessary assignment of a value
            var propLength = (int)ReadElementOffset(cursor, version);
#pragma warning restore IDE0059 // Unnecessary assignment of a value

            var name = cursor.Reader.ReadNullTerminated(cursor.Reader.ReadByte());

            var attributes = new IElementAttribute[propCount];

            for (var i = 0; i < propCount; ++i) attributes[i] = ReadAttribute(cursor);

            if (cursor.Reader.BaseStream.Position - cursor.Start >= endOffset)
                return new Element(name, null, attributes);

            var blockLength = version >= 7500 ? 25 : 13;
            var allChildren = new List<Element>();

            while (cursor.Reader.BaseStream.Position - cursor.Start < endOffset - blockLength)
            {
                var child = ReadElement(cursor, version);

                if (child is null)
                    break;
                allChildren.Add(child);
            }

            cursor.Reader.BaseStream.Position += blockLength;

            return new Element(name, allChildren.ToArray(), attributes);
        }

        private static Element TokenizeBinary(in Cursor cursor, out uint version)
        {
            var header = cursor.Reader.ReadManaged<Header>();

            version = header.Version;

            var elements = new List<IElement>();

            while (cursor.Reader.BaseStream.Position < cursor.End)
            {
                var element = ReadElement(cursor, version);

                if (element is null)
                    break;
                elements.Add(element);
            }

            return new Element("Root", elements.ToArray(), null);
        }

        private static Element TokenizeAscii()
        {
            return null;
        }

        private static bool IsBinaryFBX(in Cursor cursor)
        {
            const int kKaydaraLength = 18;

            if (cursor.End - cursor.Start >= kKaydaraLength)
            {
                var data = new byte[kKaydaraLength];
                cursor.Reader.Read(data, 0, kKaydaraLength);
                cursor.Reader.BaseStream.Position -= kKaydaraLength;
                var name = Encoding.ASCII.GetString(data, 0, kKaydaraLength);

                if (name == "Kaydara FBX Binary") return true;
            }

            return false;
        }

#if DEBUG
        private static void PrintAllProperties(TemplateObject template)
        {
            foreach (var property in template.Properties)
            {
                var p1st = string.IsNullOrEmpty(property.Primary) ? "String.Empty" : $"\"{property.Primary}\"";
                var p2nd = string.IsNullOrEmpty(property.Secondary) ? "String.Empty" : $"\"{property.Secondary}\"";
                var name = string.IsNullOrEmpty(property.Name) ? "String.Empty" : $"\"{property.Name}\"";

                var type = property.GetPropertyType().Name;
                var flag = "IEPF.Imported";
                var real = property.GetPropertyValue()?.ToString() ?? "null";

                switch (Type.GetTypeCode(property.GetPropertyType()))
                {
                    case TypeCode.Boolean:
                        type = "bool";
                        break;
                    case TypeCode.SByte:
                        type = "sbyte";
                        break;
                    case TypeCode.Byte:
                        type = "byte";
                        break;
                    case TypeCode.Int16:
                        type = "short";
                        break;
                    case TypeCode.UInt16:
                        type = "ushort";
                        break;
                    case TypeCode.Int32:
                        type = "int";
                        break;
                    case TypeCode.UInt32:
                        type = "uint";
                        break;
                    case TypeCode.Int64:
                        type = "long";
                        break;
                    case TypeCode.UInt64:
                        type = "ulong";
                        break;
                    case TypeCode.Single:
                        type = "float";
                        break;
                    case TypeCode.Double:
                        type = "double";
                        break;
                    case TypeCode.String:
                        type = "string";
                        break;
                }

                if ((property.Flags & IElementPropertyFlags.Animatable) != 0) flag += " | IEPF.Animatable";

                if ((property.Flags & IElementPropertyFlags.Animated) != 0) flag += " | IEPF.Animated";

                if ((property.Flags & IElementPropertyFlags.UserDefined) != 0) flag += " | IEPF.UserDefined";

                if ((property.Flags & IElementPropertyFlags.Hidden) != 0) flag += " | IEPF.Hidden";

                if ((property.Flags & IElementPropertyFlags.NotSavable) != 0) flag += " | IEPF.NotSavable";

                Console.WriteLine(
                    $"template.AddProperty(new FBXProperty<{type}>({p1st}, {p2nd}, {name}, {flag}, {real}));");
            }
        }
#endif

        public T Load<T>(in LoadSettings settings) where T : IScene, new()
        {
            if (settings.Stream is null)
            {
                if (Path.HasExtension(settings.DataPath) && File.Exists(settings.DataPath))
                    using (var fs = File.OpenRead(settings.DataPath))
                    {
                        return Load<T>(new LoadSettings
                        {
                            DataPath = settings.DataPath,
                            Stream = fs,
                            Position = 0,
                            Length = fs.Length,
                            Flags = settings.Flags
                        });
                    }

                throw new Exception("Stream or file to read from are not given");
            }

            if (!settings.Stream.CanRead) throw new Exception("Cannot read from the stream provided");

            using (var br = new BinaryReader(settings.Stream, Encoding.UTF8, true))
            {
                var cursor = new Cursor(settings.Position, settings.Position + settings.Length, br);

                br.BaseStream.Position = cursor.Start;

                Element root = null;

                if (IsBinaryFBX(cursor))
                {
                    root = TokenizeBinary(cursor, out var version);

                    if (version < 6200)
                        throw new Exception("Unsupported FBX file format version. Minimum supported version is 6.2");
                }
                else
                {
                    root = TokenizeAscii();
                }

                var connections = ParseConnections(root).ToArray();
                var scene = ParseObjects<T>(root, connections, settings.Flags);

                ParseTakeInfos(root, scene);
                ParseTemplates(root, scene);

                InternalPreloadClipData(scene, settings.Flags, settings.DataPath);

                br.BaseStream.Position = cursor.End;

                return scene;
            }
        }
    }
}