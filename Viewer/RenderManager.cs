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
        private readonly Color4 _missingTextureColor = new(0xffff03e6);
        private readonly Dictionary<uint, DiffuseMaterial> _textureMaterials = new();

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
                            solid.PivotMatrix.M11, solid.PivotMatrix.M12, solid.PivotMatrix.M13, solid.PivotMatrix.M14,
                            solid.PivotMatrix.M21, solid.PivotMatrix.M22, solid.PivotMatrix.M23, solid.PivotMatrix.M24,
                            solid.PivotMatrix.M31, solid.PivotMatrix.M32, solid.PivotMatrix.M33, solid.PivotMatrix.M34,
                            solid.PivotMatrix.M41, solid.PivotMatrix.M42, solid.PivotMatrix.M43, solid.PivotMatrix.M44))
                    }
                },
                ItemsSource = new List<Element3D>(solid.Materials.Select(mat => CreateMeshElement(solid, mat)))
            };
        }

        private Element3D CreateMeshElement(SolidObject solidObject, SolidObjectMaterial material)
        {
            var model = new MeshGeometryModel3D();
            model.Geometry = CreateMeshGeometry(solidObject, material);

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

        private static Geometry3D CreateMeshGeometry(SolidObject solidObject, SolidObjectMaterial material)
        {
            var faces = new List<int[]>();

            for (var j = 0; j < material.Indices.Length; j += 3)
            {
                var idx1 = material.Indices[j];
                var idx2 = material.Indices[j + 1];
                var idx3 = material.Indices[j + 2];

                faces.Add(new int[] { idx1, idx2, idx3 });
            }

            var materialVerts = solidObject.VertexSets[material.VertexSetIndex];
            var meshBuilder = new MeshBuilder();

            foreach (var face in faces)
            {
                meshBuilder.AddTriangle(face);
            }

            foreach (var vertex in materialVerts)
            {
                meshBuilder.Positions.Add(new Vector3(vertex.Position.X, vertex.Position.Y, vertex.Position.Z));
                meshBuilder.TextureCoordinates.Add(new Vector2(vertex.TexCoords.X, vertex.TexCoords.Y));
            }

            meshBuilder.ComputeNormalsAndTangents(MeshFaces.Default);

            return meshBuilder.ToMesh();
        }
    }
}