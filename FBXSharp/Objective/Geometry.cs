using System;
using System.Collections.Generic;
using System.Linq;
using FBXSharp.Core;
using FBXSharp.ValueTypes;

namespace FBXSharp.Objective
{
    public class Geometry : FBXObject
    {
        public enum ChannelType
        {
            Normal,
            Tangent,
            Binormal,
            Color,
            TexCoord,
            Material
        }

        public enum ComponentType
        {
            Int,
            Double,
            Double2,
            Double3,
            Double4
        }

        public struct Channel
        {
            public int Layer { get; }
            public string Name { get; }
            public Array Buffer { get; }
            public ChannelType Type { get; }
            public ComponentType Size { get; }

            internal Channel(int layer, string name, ChannelType type, ComponentType size, Array array)
            {
                Layer = layer;
                Name = name ?? string.Empty;
                Type = type;
                Size = size;
                Buffer = array;
            }

            public override string ToString()
            {
                return $"{Type} {Layer} | {Name}";
            }
        }

        public struct SubMesh
        {
            public int PolygonStart { get; }
            public int PolygonCount { get; }
            public int MaterialIndex { get; }

            public SubMesh(int start, int count, int matIndex)
            {
                PolygonStart = start;
                PolygonCount = count;
                MaterialIndex = matIndex;
            }
        }

        private readonly List<BlendShape> m_shapes;
        private readonly List<Channel> m_channels;
        private readonly List<Skin> m_skins;

        public static readonly FBXObjectType FType = FBXObjectType.Mesh;

        public static readonly FBXClassType FClass = FBXClassType.Geometry;

        public override FBXObjectType Type => FType;

        public override FBXClassType Class => FClass;

        public Vector3[] Vertices { get; private set; }

        public int[][] Indices { get; private set; }

        public SubMesh[] SubMeshes { get; private set; }

        public int IndexCount => InternalGetIndexCount();

        public IReadOnlyList<BlendShape> BlendShapes => m_shapes;

        public IReadOnlyList<Skin> Skins => m_skins;

        public IReadOnlyList<Channel> Channels => m_channels;

        internal Geometry(IElement element, IScene scene) : base(element, scene)
        {
            Vertices = Array.Empty<Vector3>();
            Indices = Array.Empty<int[]>();
            SubMeshes = Array.Empty<SubMesh>();
            m_channels = new List<Channel>();
            m_shapes = new List<BlendShape>();
            m_skins = new List<Skin>();
        }

        private int InternalGetIndexCount()
        {
            var result = 0;

            for (var i = 0; i < Indices.Length; ++i) result += Indices[i].Length;

            return result;
        }

        private static int ChannelSorter(Channel a, Channel b)
        {
            var result = a.Type.CompareTo(b.Type);

            return result == 0 ? a.Layer.CompareTo(b.Layer) : result;
        }

        private static void MoveMaterials(IElement[] array, int start, int count)
        {
            var end = start + count - 1;

            for (var i = start; i < end; ++i)
            {
                var element = array[i];

                if (element.Name == "LayerElementMaterial")
                {
                    while (i < end)
                    {
                        var temp = array[i + 1];
                        array[i + 1] = array[i];
                        array[i++] = temp;
                    }

                    return;
                }
            }
        }

        internal void InternalSetVertices(Vector3[] vertices)
        {
            Vertices = vertices;
        }

        internal void InternalSetIndices(int[][] indices)
        {
            Indices = indices;
        }

        internal void InternalSetSubMeshes(SubMesh[] subMeshes)
        {
            SubMeshes = subMeshes;
        }

        internal void InternalSetChannel(in Channel channel)
        {
            m_channels.Add(channel);
        }

        internal void InternalSetBlendShapes(IEnumerable<BlendShape> blendShapes)
        {
            m_shapes.AddRange(blendShapes);
        }

        internal void InternalSetSkins(IEnumerable<Skin> skins)
        {
            m_skins.AddRange(skins);
        }

        internal void InternalSortChannels()
        {
            m_channels.Sort(ChannelSorter);
        }

        private int[] RecalculateEdges()
        {
            var mapper = new Dictionary<long, int>(IndexCount);

            for (int i = 0, k = 0; i < Indices.Length; ++i)
            {
                var indexer = Indices[i];

                for (var j = 0; j < indexer.Length; ++j)
                {
                    long one = indexer[j];
                    long two = indexer[(j + 1) % indexer.Length];

                    var edge = one < two ? (two << 0x20) | one : (one << 0x20) | two;

                    if (!mapper.ContainsKey(edge)) mapper.Add(edge, k + j);
                }

                k += indexer.Length;
            }

            var edges = new int[mapper.Count];

            mapper.Values.CopyTo(edges, 0);

            return edges;
        }

        private IElement ChannelToElement(in Channel channel)
        {
            var hasWs = channel.Size == ComponentType.Double4 && channel.Type != ChannelType.Color ? 1 : 0;
            var names = default((string elemName, string arrayName, string weightName));
            var array = default(double[]);

            switch (channel.Type)
            {
                case ChannelType.Normal:
                    names = ("LayerElementNormal", "Normals", "NormalsW");
                    break;
                case ChannelType.Tangent:
                    names = ("LayerElementTangent", "Tangents", "TangentsW");
                    break;
                case ChannelType.Binormal:
                    names = ("LayerElementBinormal", "Binormals", "BinormalsW");
                    break;
                case ChannelType.Color:
                    names = ("LayerElementColor", "Colors", string.Empty);
                    break;
                case ChannelType.TexCoord:
                    names = ("LayerElementUV", "UV", string.Empty);
                    break;
            }

            if (hasWs != 0)
                array = ElementaryFactory.VtoDArray<Vector4, Vector3>(channel.Buffer as Vector4[]);
            else
                switch (channel.Size)
                {
                    case ComponentType.Double:
                        array = ElementaryFactory.DeepGenericCopy<double, double>(channel.Buffer as double[]);
                        break;
                    case ComponentType.Double2:
                        array = ElementaryFactory.VtoDArray<Vector2, Vector2>(channel.Buffer as Vector2[]);
                        break;
                    case ComponentType.Double3:
                        array = ElementaryFactory.VtoDArray<Vector3, Vector3>(channel.Buffer as Vector3[]);
                        break;
                    case ComponentType.Double4:
                        array = ElementaryFactory.VtoDArray<Vector4, Vector4>(channel.Buffer as Vector4[]);
                        break;
                }

            var elements = new IElement[5 + hasWs];

            elements[0] = Element.WithAttribute("Version", ElementaryFactory.GetElementAttribute(101));
            elements[1] = Element.WithAttribute("Name", ElementaryFactory.GetElementAttribute(channel.Name));
            elements[2] = Element.WithAttribute("MappingInformationType",
                ElementaryFactory.GetElementAttribute("ByPolygonVertex"));
            elements[3] = Element.WithAttribute("ReferenceInformationType",
                ElementaryFactory.GetElementAttribute("Direct"));
            elements[4] = Element.WithAttribute(names.arrayName, ElementaryFactory.GetElementAttribute(array));

            if (hasWs != 0)
            {
                var buffer = channel.Buffer as Vector4[];
                var weight = new double[buffer.Length];

                for (var i = 0; i < weight.Length; ++i) weight[i] = buffer[i].W;

                elements[5] = Element.WithAttribute(names.weightName, ElementaryFactory.GetElementAttribute(weight));
            }

            var attributes = new[]
            {
                ElementaryFactory.GetElementAttribute(channel.Layer)
            };

            return new Element(names.elemName, elements, attributes);
        }

        private IElement MateriaToElement()
        {
            int[] indices;
            string mapping;

            if (SubMeshes.Length == 1)
            {
                mapping = "AllSame";
                indices = new[] { SubMeshes[0].MaterialIndex };
            }
            else
            {
                mapping = "ByPolygon";
                indices = new int[Indices.Length];

                for (var i = 0; i < SubMeshes.Length; ++i)
                {
                    var subMesh = SubMeshes[i];

                    for (var k = 0; k < subMesh.PolygonCount; ++k)
                        indices[k + subMesh.PolygonStart] = subMesh.MaterialIndex;
                }
            }

            var elements = new IElement[5];

            elements[0] = Element.WithAttribute("Version", ElementaryFactory.GetElementAttribute(101));
            elements[1] = Element.WithAttribute("Name", ElementaryFactory.GetElementAttribute(string.Empty));
            elements[2] =
                Element.WithAttribute("MappingInformationType", ElementaryFactory.GetElementAttribute(mapping));
            elements[3] = Element.WithAttribute("ReferenceInformationType",
                ElementaryFactory.GetElementAttribute("IndexToDirect"));
            elements[4] = Element.WithAttribute("Materials", ElementaryFactory.GetElementAttribute(indices));

            return new Element("LayerElementMaterial", elements, new[]
            {
                ElementaryFactory.GetElementAttribute(0)
            });
        }

        public void AddBlendShape(BlendShape blendShape)
        {
            AddBlendShapeAt(blendShape, m_shapes.Count);
        }

        public void RemoveBlendShape(BlendShape blendShape)
        {
            if (blendShape is null || blendShape.Scene != Scene) return;

            _ = m_shapes.Remove(blendShape);
        }

        public void AddBlendShapeAt(BlendShape blendShape, int index)
        {
            if (blendShape is null) return;

            if (blendShape.Scene != Scene) throw new Exception("Blend shape should share same scene with geometry");

            if (index < 0 || index > m_shapes.Count)
                throw new ArgumentOutOfRangeException("Index should be in range 0 to blend shape count inclusively");

            m_shapes.Insert(index, blendShape);
        }

        public void RemoveBlendShapeAt(int index)
        {
            if (index < 0 || index >= m_shapes.Count)
                throw new ArgumentOutOfRangeException("Index should be in 0 to blend shape count range");

            m_shapes.RemoveAt(index);
        }

        public void AddSkin(Skin skin)
        {
            AddSkinAt(skin, m_skins.Count);
        }

        public void RemoveSkin(Skin skin)
        {
            if (skin is null || skin.Scene != Scene) return;

            _ = m_skins.Remove(skin);
        }

        public void AddSkinAt(Skin skin, int index)
        {
            if (skin is null) return;

            if (skin.Scene != Scene) throw new Exception("Skin should share same scene with geometry");

            if (index < 0 || index > m_skins.Count)
                throw new ArgumentOutOfRangeException("Index should be in range 0 to skin count inclusively");

            m_skins.Insert(index, skin);
        }

        public void RemoveSkinAt(int index)
        {
            if (index < 0 || index >= m_skins.Count)
                throw new ArgumentOutOfRangeException("Index should be in 0 to skin count range");

            m_skins.RemoveAt(index);
        }

        public override Connection[] GetConnections()
        {
            if (m_shapes.Count == 0 && m_skins.Count == 0) return Array.Empty<Connection>();

            var currentlyAt = 0;
            var thisHashKey = GetHashCode();
            var connections = new Connection[m_shapes.Count + m_skins.Count];

            for (var i = 0; i < m_shapes.Count; ++i)
                connections[currentlyAt++] = new Connection(Connection.ConnectionType.Object, m_shapes[i].GetHashCode(),
                    thisHashKey);

            for (var i = 0; i < m_skins.Count; ++i)
                connections[currentlyAt++] = new Connection(Connection.ConnectionType.Object, m_skins[i].GetHashCode(),
                    thisHashKey);

            return connections;
        }

        public override void ResolveLink(FBXObject linker, IElementAttribute attribute)
        {
            if (linker.Class == FBXClassType.Deformer)
            {
                if (linker.Type == FBXObjectType.BlendShape) AddBlendShape(linker as BlendShape);

                if (linker.Type == FBXObjectType.Skin) AddSkin(linker as Skin);
            }
        }

        public override IElement AsElement(bool binary)
        {
            if (SubMeshes.Length != 0)
                m_channels.Add(new Channel(0, string.Empty, ChannelType.Material, ComponentType.Int, null));

            var grouping = m_channels.GroupBy(_ => _.Layer).ToArray();
            var elements = new IElement[5 + m_channels.Count + grouping.Length];

            var vertexs = new double[Vertices.Length * 3];
            var indices = new int[IndexCount];
            var edgearr = RecalculateEdges();

            for (int i = 0, k = 0; i < Vertices.Length; ++i)
            {
                var vertex = Vertices[i];

                vertexs[k++] = vertex.X;
                vertexs[k++] = vertex.Y;
                vertexs[k++] = vertex.Z;
            }

            for (int i = 0, k = 0; i < Indices.Length; ++i)
            {
                var indexer = Indices[i];
                var counter = indexer.Length - 1;

                if (counter < 0) continue;

                for (var j = 0; j < counter; ++j) indices[k++] = indexer[j];

                indices[k++] = ~indexer[counter];
            }

            elements[0] = BuildProperties70();
            elements[1] = Element.WithAttribute("Vertices", ElementaryFactory.GetElementAttribute(vertexs));
            elements[2] = Element.WithAttribute("PolygonVertexIndex", ElementaryFactory.GetElementAttribute(indices));
            elements[3] = Element.WithAttribute("Edges", ElementaryFactory.GetElementAttribute(edgearr));
            elements[4] = Element.WithAttribute("GeometryVersion", ElementaryFactory.GetElementAttribute(124));

            for (int l = 0, k = 5; l < grouping.Length; ++l)
            {
                var channels = grouping[l].ToArray();
                var elemento = new IElement[1 + channels.Length];

                elemento[0] = Element.WithAttribute("Version", ElementaryFactory.GetElementAttribute(100));

                for (var c = 0; c < channels.Length; ++c)
                {
                    var channel = channels[c];
                    var element = default(IElement);

                    if (channel.Type == ChannelType.Material)
                        element = MateriaToElement();
                    else
                        element = ChannelToElement(channel);

                    elements[k++] = element;

                    elemento[1 + c] = new Element("LayerElement", new IElement[]
                    {
                        Element.WithAttribute("Type", ElementaryFactory.GetElementAttribute(element.Name)),
                        Element.WithAttribute("TypeIndex", ElementaryFactory.GetElementAttribute(channel.Layer))
                    }, null);
                }

                elements[5 + m_channels.Count + l] = new Element("Layer", elemento, new[]
                {
                    ElementaryFactory.GetElementAttribute(grouping[l].Key)
                });
            }

            if (SubMeshes.Length != 0)
            {
                MoveMaterials(elements, 5, m_channels.Count);
                m_channels.RemoveAt(m_channels.Count - 1);
            }

            return new Element(Class.ToString(), elements, BuildAttributes("Geometry", Type.ToString(), binary));
        }
    }

    public class GeometryBuilder : BuilderBase
    {
        public enum PolyType
        {
            Tri = 3,
            Quad = 4
        }

        private readonly List<Vector3> m_vertices;
        private readonly List<int[]> m_polygons;
        private readonly List<Geometry.Channel> m_channels;
        private readonly List<Geometry.SubMesh> m_subMeshes;
        private readonly List<BlendShape> m_blendShapes;
        private readonly List<Skin> m_skins;

        public IReadOnlyList<Geometry.Channel> Channels => m_channels;

        public IReadOnlyList<Geometry.SubMesh> SubMeshes => m_subMeshes;

        public IReadOnlyList<BlendShape> BlendShapes => m_blendShapes;

        public IReadOnlyList<Skin> Skins => m_skins;

        public GeometryBuilder(Scene scene) : base(scene)
        {
            m_vertices = new List<Vector3>();
            m_polygons = new List<int[]>();
            m_channels = new List<Geometry.Channel>();
            m_subMeshes = new List<Geometry.SubMesh>();
            m_blendShapes = new List<BlendShape>();
            m_skins = new List<Skin>();
        }

        public Geometry BuildGeometry()
        {
            var geometry = m_scene.CreateGeometry();

            geometry.Name = m_name;

            geometry.InternalSetVertices(m_vertices.ToArray());
            geometry.InternalSetIndices(m_polygons.ToArray());
            geometry.InternalSetSubMeshes(m_subMeshes.ToArray());
            geometry.InternalSetBlendShapes(m_blendShapes);
            geometry.InternalSetSkins(m_skins);

            var indexCount = geometry.IndexCount;

            for (var i = 0; i < m_channels.Count; ++i)
            {
                var channel = m_channels[i];

                if (channel.Buffer.Length == indexCount)
                {
                    geometry.InternalSetChannel(channel);
                }
                else
                {
                    Array array;

                    switch (channel.Size)
                    {
                        case Geometry.ComponentType.Int:
                            array = Resize(channel.Buffer as int[], indexCount);
                            break;
                        case Geometry.ComponentType.Double:
                            array = Resize(channel.Buffer as double[], indexCount);
                            break;
                        case Geometry.ComponentType.Double2:
                            array = Resize(channel.Buffer as Vector2[], indexCount);
                            break;
                        case Geometry.ComponentType.Double3:
                            array = Resize(channel.Buffer as Vector3[], indexCount);
                            break;
                        case Geometry.ComponentType.Double4:
                            array = Resize(channel.Buffer as Vector4[], indexCount);
                            break;
                        default:
                            array = null;
                            break;
                    }

                    geometry.InternalSetChannel(new Geometry.Channel(channel.Layer, channel.Name, channel.Type,
                        channel.Size, array));
                }
            }

            for (var i = 0; i < m_properties.Count; ++i) geometry.AddProperty(m_properties[i]);

            geometry.InternalSortChannels();

            return geometry;
        }

        private static T[] Resize<T>(T[] array, int size)
        {
            Array.Resize(ref array, size);

            return array;
        }

        public GeometryBuilder WithName(string name)
        {
            SetObjectName(name);
            return this;
        }

        public GeometryBuilder WithFBXProperty<T>(string name, T value, bool isUser = false)
        {
            SetFBXProperty(name, value, isUser);
            return this;
        }

        public GeometryBuilder WithFBXProperty<T>(string name, T value, IElementPropertyFlags flags)
        {
            SetFBXProperty(name, value, flags);
            return this;
        }

        public GeometryBuilder WithFBXProperty<T>(FBXProperty<T> property)
        {
            SetFBXProperty(property);
            return this;
        }

        public GeometryBuilder WithVertex(in Vector3 vertex)
        {
            m_vertices.Add(vertex);
            return this;
        }

        public GeometryBuilder WithVertices(Vector3[] vertices)
        {
            m_vertices.AddRange(vertices ?? Array.Empty<Vector3>());
            return this;
        }

        public GeometryBuilder WithPolygon(int[] poly)
        {
            if (poly is null || poly.Length < 3) throw new Exception("Poly was null or its index count is less than 3");

            m_polygons.Add(poly);
            return this;
        }

        public GeometryBuilder WithIndices(int[] indices, PolyType type)
        {
            return WithIndices(indices, (int)type);
        }

        public GeometryBuilder WithIndices(int[] indices, int numPerPoly)
        {
            if (numPerPoly < 3) throw new Exception("Minimum number of indices per polygon should be 3");

            var div = indices.Length / numPerPoly;
            var mod = indices.Length - numPerPoly * div;

            if (mod != 0) throw new Exception($"Cannot equally divide index buffer given into {numPerPoly} polygons");

            var polygons = new int[div][];

            for (var i = 0; i < div; ++i)
            {
                var poly = new int[numPerPoly];

                for (var k = 0; k < numPerPoly; ++k) poly[k] = indices[i * numPerPoly + k];

                polygons[i] = poly;
            }

            m_polygons.AddRange(polygons);
            return this;
        }

        public GeometryBuilder WithSubMesh(int polyStart, int polyCount, int matIndex = 0)
        {
            m_subMeshes.Add(new Geometry.SubMesh(polyStart, polyCount, matIndex));
            return this;
        }

        public GeometryBuilder WithSubMesh(in Geometry.SubMesh subMesh)
        {
            m_subMeshes.Add(subMesh);
            return this;
        }

        public GeometryBuilder WithBlendShape(BlendShape blendShape)
        {
            if (blendShape is null) return this;

            if (blendShape.Scene != m_scene)
                throw new ArgumentException("Blend shape should share same scene as the geometry");

            m_blendShapes.Add(blendShape);

            return this;
        }

        public GeometryBuilder WithSkin(Skin skin)
        {
            if (skin is null) return this;

            if (skin.Scene != m_scene) throw new ArgumentException("Skin should share same scene as the geometry");

            m_skins.Add(skin);

            return this;
        }

        public GeometryBuilder WithNormals(Vector3[] normals, int layer = 0, string name = "")
        {
            if (normals is null || normals.Length == 0 ||
                m_channels.FindIndex(_ => _.Layer == layer && _.Type == Geometry.ChannelType.Normal) >= 0) return this;

            m_channels.Add(new Geometry.Channel(layer, name, Geometry.ChannelType.Normal,
                Geometry.ComponentType.Double3, normals));

            return this;
        }

        public GeometryBuilder WithTangents(Vector4[] tangents, int layer = 0, string name = "")
        {
            if (tangents is null || tangents.Length == 0 ||
                m_channels.FindIndex(_ => _.Layer == layer && _.Type == Geometry.ChannelType.Tangent) >= 0) return this;

            m_channels.Add(new Geometry.Channel(layer, name, Geometry.ChannelType.Tangent,
                Geometry.ComponentType.Double4, tangents));

            return this;
        }

        public GeometryBuilder WithBinormals(Vector4[] binormals, int layer = 0, string name = "")
        {
            if (binormals is null || binormals.Length == 0 ||
                m_channels.FindIndex(_ => _.Layer == layer && _.Type == Geometry.ChannelType.Binormal) >= 0)
                return this;

            m_channels.Add(new Geometry.Channel(layer, name, Geometry.ChannelType.Binormal,
                Geometry.ComponentType.Double4, binormals));

            return this;
        }

        public GeometryBuilder WithColors(Vector4[] colors, int layer = 0, string name = "")
        {
            if (colors is null || colors.Length == 0 ||
                m_channels.FindIndex(_ => _.Layer == layer && _.Type == Geometry.ChannelType.Color) >= 0) return this;

            m_channels.Add(new Geometry.Channel(layer, name, Geometry.ChannelType.Color, Geometry.ComponentType.Double4,
                colors));

            return this;
        }

        public GeometryBuilder WithUVs(Vector2[] uvs, int layer = 0, string name = "")
        {
            if (uvs is null || uvs.Length == 0 ||
                m_channels.FindIndex(_ => _.Layer == layer && _.Type == Geometry.ChannelType.TexCoord) >= 0)
                return this;

            m_channels.Add(new Geometry.Channel(layer, name, Geometry.ChannelType.TexCoord,
                Geometry.ComponentType.Double2, uvs));

            return this;
        }
    }
}