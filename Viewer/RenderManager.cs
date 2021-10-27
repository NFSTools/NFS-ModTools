using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Common;
using Common.Geometry.Data;
using Common.Textures.Data;
using HelixToolkit.Wpf;
using Pfim;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Drawing.Color;

namespace Viewer
{
    /// <summary>
    /// Manages the 3D viewport.
    /// </summary>
    public class RenderManager
    {
        private readonly Dictionary<uint, SolidObject> _renderObjectDictionary = new Dictionary<uint, SolidObject>();
        private readonly Dictionary<uint, ModelVisual3D> _renderCache = new Dictionary<uint, ModelVisual3D>();
        private readonly Dictionary<uint, BitmapImage> _renderTextureDictionary = new Dictionary<uint, BitmapImage>();

        private long _textureUpdateTime;

        private static RenderManager _instance;
        private static readonly object InstanceLock = new object();

        public static RenderManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new RenderManager();
                        }
                    }
                }

                return _instance;
            }
        }

        private HelixViewport3D _viewport;
        private ModelVisual3D _modelVisualManager;

        private RenderManager() { }

        /// <summary>
        /// Set the 3D viewport.
        /// </summary>
        /// <param name="viewport"></param>
        public void SetViewport(HelixViewport3D viewport)
        {
            _viewport = viewport ?? throw new ArgumentNullException(nameof(viewport));
        }

        public void SetModelVisualManager(ModelVisual3D modelVisual3D)
        {
            _modelVisualManager = modelVisual3D ?? throw new ArgumentNullException(nameof(modelVisual3D));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="solidObject"></param>
        public void AddRenderObject(SolidObject solidObject)
        {
            _renderObjectDictionary[solidObject.Hash] = solidObject;
        }

        /// <summary>
        /// Generate DDS data for a texture. This does not re-render the scene!
        /// </summary>
        /// <remarks>Does nothing if there is already data for the texture.</remarks>
        /// <param name="texture"></param>
        public void AddRenderTexture(Texture texture)
        {
            if (!_renderTextureDictionary.ContainsKey(texture.TexHash))
            {
                //using (var ms = new MemoryStream(bytes))
                //{
                //    return DevIL.DevIL.LoadBitmap(ms).BitmapToBitmapImage();
                //}
                using (var ms = new MemoryStream())
                {
                    texture.GenerateImage(ms);
                    _renderTextureDictionary[texture.TexHash] = DevIL.DevIL.LoadBitmap(ms).ToBitmapImage();
                }
                //_renderTextureDictionary[texture.TexHash] = new DDSImage(texture.GenerateImage())
                //    .BitmapImage
                //    .ToBitmapImage();
                _textureUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
        }

        public void RemoveRenderObject(SolidObject solidObject) => _renderObjectDictionary.Remove(solidObject.Hash);
        public void RemoveRenderTexture(Texture texture) => _renderTextureDictionary.Remove(texture.TexHash);

        /// <summary>
        /// Re-render the 3D scene.
        /// </summary>
        public void UpdateScene()
        {
            _modelVisualManager.Children.Clear();

            foreach (var solidObject in _renderObjectDictionary)
            {
                if (_textureUpdateTime != DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    _renderCache[solidObject.Value.Hash] = GenerateModel(solidObject.Value);
                }

                _modelVisualManager.Children.Add(_renderCache[solidObject.Value.Hash]);
                //_modelVisualManager.Children.Add();
            }
        }

        /// <summary>
        /// Reset everything.
        /// </summary>
        public void Reset()
        {
            _modelVisualManager.Children.Clear();
            _renderTextureDictionary.Clear();
            _renderCache.Clear();
            _renderObjectDictionary.Clear();
        }

        private ModelVisual3D GenerateModel(SolidObject solidObject)
        {
            var meshBuilders = new List<MeshBuilder>();
            var materials = new List<Material>();

            foreach (var material in solidObject.Materials)
            {
                var faces = new List<ushort[]>();

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

                    faces.Add(new[] { idx1, idx2, idx3 });
                }

                var meshBuilder = new MeshBuilder(generateNormals: material.Vertices.Any(v => v.Normal.HasValue));

                foreach (var face in faces)
                {
                    meshBuilder.AddTriangle(new List<int> {
                        face[0],
                        face[1],
                        face[2]
                    });
                }

                foreach (var vertex in material.Vertices)
                {
                    meshBuilder.Positions.Add(new Point3D(vertex.Position.X, vertex.Position.Y, vertex.Position.Z));
                    if (vertex.Normal.HasValue)
                        meshBuilder.Normals.Add(new Vector3D(vertex.Normal.Value.X, vertex.Normal.Value.Y, vertex.Normal.Value.Z));
                    meshBuilder.TextureCoordinates.Add(new System.Windows.Point(vertex.TexCoords.X, -vertex.TexCoords.Y));
                }

                if (!meshBuilder.HasNormals)
                {
                    meshBuilder.ComputeNormalsAndTangents(MeshFaces.Default);
                }

                meshBuilders.Add(meshBuilder);

                if (_renderTextureDictionary.ContainsKey(material.TextureHash))
                {
                    materials.Add(MaterialHelper.CreateMaterial(
                        CreateTextureBrush(_renderTextureDictionary[material.TextureHash])));
                }
                else
                {
                    Console.Error.WriteLine($"WARN: {solidObject.Name}::{material.Name} references unknown texture 0x{material.TextureHash:X8}");
                    materials.Add(new DiffuseMaterial(Brushes.Gray));
                }
            }

            var mvd = new ModelVisual3D();
            var modelGroup = new Model3DGroup();
            var modelList = meshBuilders.Select((mb, i) =>
            {
                var material = materials[i];
                var mesh = mb.ToMesh();
                var model = new GeometryModel3D
                {
                    Geometry = mesh,
                    Material = material,
                    BackMaterial = material,
                    Transform = new Transform3DGroup
                    {
                        Children =
                        {
                            new MatrixTransform3D(new Matrix3D(
                                solidObject.Transform.M11, solidObject.Transform.M12, solidObject.Transform.M13, solidObject.Transform.M14,
                                solidObject.Transform.M21, solidObject.Transform.M22, solidObject.Transform.M23, solidObject.Transform.M24,
                                solidObject.Transform.M31, solidObject.Transform.M32, solidObject.Transform.M33, solidObject.Transform.M34,
                                solidObject.Transform.M41, solidObject.Transform.M42, solidObject.Transform.M43, solidObject.Transform.M44))
                        }
                    }
                };
                return model;
            }).ToList();

            foreach (var gm in modelList)
            {
                modelGroup.Children.Add(gm);
            }

            mvd.Content = modelGroup;

            return mvd;
        }

        /// <summary>
        /// Creates a texture brush.
        /// </summary>
        /// <returns>The brush.</returns>
        private ImageBrush CreateTextureBrush(ImageSource bmi)
        {
            return new ImageBrush(bmi)
            {
                Opacity = 1.0,
                ViewportUnits = BrushMappingMode.Absolute,
                TileMode = TileMode.Tile
            };
        }
    }
}
