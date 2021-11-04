using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Media3D;
using Common.Geometry.Data;
using Common.Textures.Data;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using DiffuseMaterial = HelixToolkit.Wpf.SharpDX.DiffuseMaterial;
using Geometry3D = HelixToolkit.SharpDX.Core.Geometry3D;

namespace Viewer
{
    public class RenderManager
    {
        private readonly Dictionary<uint, DiffuseMaterial> _textureMaterials = new();
        private readonly Color4 _missingTextureColor = new(0xffff03e6);

        public ObservableConcurrentDictionary<SolidObject, Element3D> CurrentElements { get; } =
            new();

        public void Reset()
        {
            _textureMaterials.Clear();
            foreach (var keyValuePair in CurrentElements.ToList())
            {
                CurrentElements.Remove(keyValuePair.Key);
            }
        }

        public void EnableTexture(Texture texture)
        {
            var stream = new MemoryStream();
            texture.GenerateImage(stream);

#if DEBUG
            File.WriteAllBytes($"RenderTexDebug_{texture.Name}_0x{texture.TexHash:X8}.dds", stream.ToArray());
#endif

            if (!_textureMaterials.TryGetValue(texture.TexHash, out var material))
            {
                _textureMaterials[texture.TexHash] =
                    new DiffuseMaterial { DiffuseMap = TextureModel.Create(stream) };
            }
            else
            {
                material.DiffuseMap = TextureModel.Create(stream);
                material.DiffuseColor = Color4.White;
            }
        }

        public void DisableTexture(Texture texture)
        {
            _textureMaterials[texture.TexHash].DiffuseMap = null;
            _textureMaterials[texture.TexHash].DiffuseColor = _missingTextureColor;
        }

        public void EnableSolid(SolidObject solidObject)
        {
            CurrentElements.Add(solidObject, CreateSolidElement(solidObject));
        }

        public void DisableSolid(SolidObject solidObject)
        {
            CurrentElements.Remove(solidObject);
        }

        private Element3D CreateSolidElement(SolidObject solid)
        {
            return new ItemsModel3D
            {
                Transform = new Transform3DGroup
                {
                    Children = new Transform3DCollection
                    {
                        new MatrixTransform3D(new Matrix3D(
                            solid.Transform.M11, solid.Transform.M12, solid.Transform.M13, solid.Transform.M14,
                            solid.Transform.M21, solid.Transform.M22, solid.Transform.M23, solid.Transform.M24,
                            solid.Transform.M31, solid.Transform.M32, solid.Transform.M33, solid.Transform.M34,
                            solid.Transform.M41, solid.Transform.M42, solid.Transform.M43, solid.Transform.M44)),
                    }
                },
                ItemsSource = new List<Element3D>(solid.Materials.Select(CreateMeshElement))
            };
        }

        private Element3D CreateMeshElement(SolidObjectMaterial material)
        {
            var model = new MeshGeometryModel3D();
            model.Geometry = CreateMeshGeometry(material);

            // If we already have a material for the texture, use it
            if (_textureMaterials.TryGetValue(material.TextureHash, out var textureMaterial))
            {
                model.Material = textureMaterial;
            }
            else
            {
                model.Material = _textureMaterials[material.TextureHash] = new DiffuseMaterial
                {
                    DiffuseColor = _missingTextureColor
                };
            }

            return model;
        }

        private Geometry3D CreateMeshGeometry(SolidObjectMaterial material)
        {
            var faces = new List<int[]>();

            for (var j = 0; j < material.Indices.Length; j += 3)
            {
                var idx1 = material.Indices[j];
                var idx2 = material.Indices[j + 1];
                var idx3 = material.Indices[j + 2];

                if (idx1 != idx2 && idx1 != idx3 && idx2 != idx3)
                {
                    idx2 = idx3;
                    idx3 = material.Indices[j + 1];
                }

                faces.Add(new int[] { idx1, idx2, idx3 });
            }

            var meshBuilder = new MeshBuilder(generateNormals: material.Vertices.Any(v => v.Normal.HasValue));

            foreach (var face in faces)
            {
                meshBuilder.AddTriangle(face);
            }

            foreach (var vertex in material.Vertices)
            {
                meshBuilder.Positions.Add(new Vector3(vertex.Position.X, vertex.Position.Y, vertex.Position.Z));
                if (vertex.Normal.HasValue)
                    meshBuilder.Normals.Add(new Vector3(vertex.Normal.Value.X, vertex.Normal.Value.Y,
                        vertex.Normal.Value.Z));
                meshBuilder.TextureCoordinates.Add(new Vector2(vertex.TexCoords.X, -vertex.TexCoords.Y));
            }

            if (!meshBuilder.HasNormals)
            {
                meshBuilder.ComputeNormalsAndTangents(MeshFaces.Default);
            }

            return meshBuilder.ToMesh();
        }
    }
}