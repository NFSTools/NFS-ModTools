using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using CommandLine;
using Common;
using Common.Geometry.Data;
using Common.Scenery.Data;
using Common.Textures.Data;
using Common.TrackStream;
using Common.TrackStream.Data;

namespace AssetDumper
{
    internal class Program
    {
        private static bool IsVerbose;

        internal enum ObjectOutputType
        {
            Wavefront,
            Collada,
            Fbx
        }

        internal class Options
        {
            [Value(0, Required = true, HelpText = "The files to read.")]
            public IEnumerable<string> InputFiles { get; set; }

            [Option("game", Required = true, HelpText = "The ID of the game you are using.")]
            public GameDetector.Game Game { get; set; }

            [Option('o', "overwrite", Required = false, HelpText = "Dump asset files, regardless of their existence.")]
            public bool OverwriteAssets { get; set; }

            [Option('c', "combine-models", Required = false, HelpText = "Combine all models into one file.")]
            public bool ModelCombine { get; set; }

            [Option('v', "verbose", Required = false, HelpText = "Enable verbose messages.")]
            public bool Verbose { get; set; }

            [Option('m', "map", Required = false,
                HelpText = "Load the input file(s) as a map stream(s) and export assets to section folders.")]
            public bool MapMode { get; set; }

            [Option('p', "positions", Required = false, HelpText = "Bakes location data into exported object files.")]
            public bool BakePositions { get; set; }

            [Option('f', "output-dir", Required = true, HelpText = "Set the base output directory for assets.")]
            public string OutputDirectory { get; set; }
        }

        /// <summary>
        /// The entry point of the application.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(opts =>
                {
                    IsVerbose = opts.Verbose;
                    ProcessInputFiles(opts.InputFiles, opts);
                });
        }

        /// <summary>
        /// Read a list of files.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="options"></param>
        private static void ProcessInputFiles(IEnumerable<string> files, Options options)
        {
            var fileList = files.ToList();

            Console.WriteLine($"INFO: Reading {fileList.Count} file(s)");

            if (options.MapMode)
            {
                fileList.ForEach(f => ReadMapFile(f, options));
            }
            else
            {
                fileList.ForEach(f => ReadInputFile(f, Path.GetFullPath(options.OutputDirectory), options));
            }
        }

        /// <summary>
        /// Read a single file.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="outDirectory"></param>
        /// <param name="options"></param>
        private static void ReadInputFile(string file, string outDirectory, Options options)
        {
            var cm = new ChunkManager(
                options.Game);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            cm.Read(file);
            stopwatch.Stop();

            LogVerbose($"Read file in {stopwatch.ElapsedMilliseconds}ms");

            var resources = cm.Chunks.Where(c => c.Resource != null).Select(c => c.Resource).ToList();

            ProcessResources(resources, outDirectory, options);
        }

        /// <summary>
        /// Read a map file.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="options"></param>
        private static void ReadMapFile(string file, Options options)
        {
            GameBundleManager gbm;

            switch (options.Game)
            {
                case GameDetector.Game.MostWanted:
                    gbm = new MostWantedManager();
                    break;
                case GameDetector.Game.Carbon:
                    gbm = new CarbonManager();
                    break;
                case GameDetector.Game.ProStreet:
                case GameDetector.Game.ProStreetTest:
                    gbm = new ProStreetManager();
                    break;
                case GameDetector.Game.Undercover:
                    gbm = new UndercoverManager();
                    break;
                case GameDetector.Game.World:
                    gbm = new World15Manager();
                    break;
                default:
                    gbm = null;
                    break;
            }

            if (gbm == null)
            {
                Console.Error.WriteLine($"ERROR: Cannot load file [{file}] due to the game being unsupported.");
                return;
            }

            var bundle = gbm.ReadLocationBundle(file);
            var cm = new ChunkManager(options.Game);
            BinaryReader masterStream = null;

            if (options.Game != GameDetector.Game.World)
            {
                masterStream = new BinaryReader(
                    File.OpenRead(
                        Path.Combine(
                            Path.GetDirectoryName(file) ?? throw new InvalidOperationException(),
                            $"STREAM{bundle.Name}.BUN")));
            }

            foreach (var streamSection in bundle.Sections)
            {
                cm.Reset();

                LogVerbose($"Processing section {streamSection.Number} ({streamSection.Name})");

                if (masterStream == null)
                {
                    var ws = (WorldStreamSection)streamSection;
                    var sectionPath = Path.Combine(
                        Path.GetDirectoryName(file) ?? throw new InvalidOperationException(),
                        $"STREAM{bundle.Name}_" +
                        (ws.FragmentFileId == 0 ? $"{ws.Number}" : $"{ws.FragmentFileId:X8}"));
                    var sectionStream = File.OpenRead(sectionPath);

                    LogVerbose($"\tOpening {sectionPath}");
                    var br = new BinaryReader(sectionStream);

                    cm.Read(br);

                    br.Dispose();
                }
                else
                {
                    masterStream.BaseStream.Position = streamSection.Offset;

                    LogVerbose($"\tReading {streamSection.Size} bytes from offset {streamSection.Offset}");
                    var data = masterStream.ReadBytesRequired((int)streamSection.Size);
                    var br = new BinaryReader(new MemoryStream(data));

                    cm.Read(br);

                    br.Dispose();
                }

                ProcessResources(
                    cm.Chunks.Where(c => c.Resource != null).Select(c => c.Resource).ToList(),
                    options.OutputDirectory,
                    options
                );
            }

            masterStream?.Dispose();
        }

        /// <summary>
        /// Process resources and create files.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="baseDir"></param>
        /// <param name="options"></param>
        private static void ProcessResources(IReadOnlyCollection<ChunkManager.BasicResource> resources,
            string baseDir,
            Options options)
        {
            var texturesDir = Path.Combine(baseDir, "textures");

            if (!Directory.Exists(texturesDir))
            {
                Directory.CreateDirectory(texturesDir);
            }

            LogInfo("Processing texture packs...");

            // Process texture packs
            foreach (var resource in resources.OfType<TexturePack>())
            {
                LogVerbose($"Processing TPK: {resource.PipelinePath} ({resource.Name})");

                foreach (var texture in resource.Textures)
                {
                    var texturePath = Path.Combine(texturesDir, $"0x{texture.TexHash:X8}.dds");

                    LogVerbose($"\tExporting texture {texture.Name} to {texturePath}");
                    texture.DumpToFile(texturePath);
                }
            }

            var solidLists = resources.OfType<SolidList>().ToList();
            var scenerySections = resources.OfType<ScenerySection>().ToList();

            if (scenerySections.Any())
            {
                LogInfo("Processing scenery sections...");
                var solidObjects = solidLists.SelectMany(l => l.Objects)
                        .ToLookup(o => o.Hash, o => o)
                        .ToDictionary(o => o.Key, o => o.First());
                foreach (var scenerySection in scenerySections.OrderBy(s => s.SectionNumber))
                {
                    ExportScenerySection(solidObjects, scenerySection, Path.Combine(options.OutputDirectory,
                        scenerySection.SectionNumber + ".dae"));
                }
            }
            else if (solidLists.Any())
            {
                LogInfo("Processing model packs...");

                if (options.ModelCombine)
                {
                    LogInfo("Exporting all models to <output>/combined.dae");
                    ExportMultipleSolids(solidLists.SelectMany(s => s.Objects).ToList(),
                        Path.Combine(options.OutputDirectory, "combined.dae"));
                }
                else
                {
                    LogInfo("Exporting individual models");

                    var modelsDir = Path.Combine(baseDir, "models");
                    if (!Directory.Exists(modelsDir))
                    {
                        Directory.CreateDirectory(modelsDir);
                    }

                    foreach (var solidObject in solidLists.SelectMany(l => l.Objects))
                    {
                        ExportSingleSolid(solidObject,
                            Path.Combine(modelsDir, $"{solidObject.Name}.dae"));
                    }
                }
            }
            else
            {
                LogInfo("No models or scenery sections were found.");
            }
        }

        private class SceneExportNode
        {
            public SolidObject SolidObject { get; }
            public string Name { get; }
            public Matrix4x4 Transform { get; }

            public SceneExportNode(SolidObject solidObject, string name, Matrix4x4 transform)
            {
                SolidObject = solidObject;
                Name = name;
                Transform = transform;
            }
        }

        private class SceneExport
        {
            public List<SceneExportNode> Nodes { get; }
            public string SceneName { get; }

            public SceneExport(List<SceneExportNode> nodes, string sceneName)
            {
                Nodes = nodes;
                SceneName = sceneName;
            }
        }

        private static void ExportScenerySection(IReadOnlyDictionary<uint, SolidObject> objects,
            ScenerySection scenerySection, string outputPath)
        {
            LogInfo(
                $"Exporting scenery section {scenerySection.SectionNumber} ({scenerySection.Infos.Count} models, {scenerySection.Instances.Count} instances)");

            var sceneNodes = new List<SceneExportNode>();

            foreach (var instance in scenerySection.Instances)
            {
                var info = scenerySection.Infos[instance.InfoIndex];

                if (!objects.TryGetValue(info.SolidKey, out var solid))
                    continue;

                var instanceMatrix =
                    Matrix4x4.CreateScale(instance.Scale) * Matrix4x4.CreateFromQuaternion(instance.Rotation)
                                                          * Matrix4x4.CreateTranslation(instance.Position);
                sceneNodes.Add(new SceneExportNode(solid, info.Name, instanceMatrix));
            }

            var scene = new SceneExport(sceneNodes, $"ScenerySection_{scenerySection.SectionNumber}");
            ExportScene(scene, outputPath, "textures");
        }

        private static void ExportMultipleSolids(List<SolidObject> solidObjects, string outputPath)
        {
            var sceneNodes = new List<SceneExportNode>();

            foreach (var solid in solidObjects)
            {
                sceneNodes.Add(new SceneExportNode(solid, solid.Name, solid.Transform));
            }

            var scene = new SceneExport(sceneNodes, "MultiSolidExport");
            ExportScene(scene, outputPath, "textures");
        }

        private static void ExportSingleSolid(SolidObject solidObject, string outputPath)
        {
            var sceneNodes = new List<SceneExportNode>
            {
                new SceneExportNode(solidObject, solidObject.Name, solidObject.Transform)
            };

            var scene = new SceneExport(sceneNodes, solidObject.Name);
            ExportScene(scene, outputPath, "../textures");
        }

        private static void ExportScene(SceneExport scene, string outputPath, string texturesDirectory)
        {
            var collada = new COLLADA();
            collada.version = VersionType.Item141;

            var solidsToAdd = scene.Nodes.Select(n => n.SolidObject)
                .Distinct(SolidObject.HashComparer)
                .ToList();
            var texturesToAdd = solidsToAdd
                .SelectMany(s => s.TextureHashes)
                .Distinct()
                .ToList();

            var images = new library_images();
            var imageList = new List<image>();
            var materials = new library_materials();
            var materialList = new List<material>();
            var effects = new library_effects();
            var effectList = new List<effect>();

            foreach (var textureId in texturesToAdd)
            {
                /*
                symbol = $"material{materialIdx}",
                target = $"#texture-0x{material.TextureHash:X8}"
                 */
                imageList.Add(new image
                {
                    id = $"texture-0x{textureId:X8}-img",
                    name = $"TextureIMG-0x{textureId:X8}",
                    Item = Path.Combine(texturesDirectory, $"0x{textureId:X8}.dds"),
                    depth = 1
                });
                materialList.Add(new material
                {
                    id = $"texture-0x{textureId:X8}",
                    name = $"TextureMTL-0x{textureId:X8}",
                    instance_effect = new instance_effect
                    {
                        url = $"#texture-0x{textureId:X8}-fx"
                    },
                });
                effectList.Add(new effect
                {
                    id = $"texture-0x{textureId:X8}-fx",
                    name = $"TextureFX-0x{textureId:X8}",
                    Items = new[]
                    {
                        new effectFx_profile_abstractProfile_COMMON
                        {
                            Items = new object[]
                            {
                                new common_newparam_type
                                {
                                    sid = $"texture-0x{textureId:X8}-fx-surface",
                                    Item = new fx_surface_common
                                    {
                                        type = fx_surface_type_enum.Item2D,
                                        init_from = new []
                                        {
                                            new fx_surface_init_from_common { Value = $"texture-0x{textureId:X8}-img"}
                                        },
                                    },
                                    ItemElementName = ItemChoiceType.surface
                                },
                                new common_newparam_type
                                {
                                    sid = $"texture-0x{textureId:X8}-fx-sampler",
                                    Item = new fx_sampler2D_common()
                                    {
                                        source = $"texture-0x{textureId:X8}-fx-surface",
                                        minfilter = fx_sampler_filter_common.LINEAR_MIPMAP_LINEAR,
                                        magfilter = fx_sampler_filter_common.LINEAR
                                    },
                                    ItemElementName = ItemChoiceType.sampler2D
                                }
                            },
                            technique = new effectFx_profile_abstractProfile_COMMONTechnique
                            {
                                sid = "common",
                                Item = new effectFx_profile_abstractProfile_COMMONTechniqueBlinn
                                {
                                    diffuse = new common_color_or_texture_type
                                    {
                                        Item = new common_color_or_texture_typeTexture
                                        {
                                            texture = $"texture-0x{textureId:X8}-fx-sampler",
                                            texcoord = "TEX0"
                                        }
                                    }
                                }
                            }
                        }
                    }
                });
            }

            images.image = imageList.ToArray();
            materials.material = materialList.ToArray();
            effects.effect = effectList.ToArray();

            // Build geometry library

            var geometries = new library_geometries();
            var geometryList = new List<geometry>();
            var geometryIds = new Dictionary<uint, string>();

            foreach (var solidObject in solidsToAdd)
            {
                var geometryId = $"0x{solidObject.Hash:X8}";
                geometryList.Add(SolidToGeometry(solidObject, geometryId));
                geometryIds.Add(solidObject.Hash, geometryId);
            }

            geometries.geometry = geometryList.ToArray();

            library_visual_scenes visualScenes = new library_visual_scenes();

            var sceneNodes = new List<node>();

            for (int idx = 0; idx < scene.Nodes.Count; idx++)
            {
                var node = scene.Nodes[idx];
                var instanceMatrix = node.Transform;
                sceneNodes.Add(new node
                {
                    name = node.Name,
                    id = $"scene_{scene.SceneName}_node_{idx}",
                    Items = new object[]
                        {
                            new matrix
                            {
                                Values = new double[]
                                {
                                    instanceMatrix.M11,
                                    instanceMatrix.M21,
                                    instanceMatrix.M31,
                                    instanceMatrix.M41,

                                    instanceMatrix.M12,
                                    instanceMatrix.M22,
                                    instanceMatrix.M32,
                                    instanceMatrix.M42,

                                    instanceMatrix.M13,
                                    instanceMatrix.M23,
                                    instanceMatrix.M33,
                                    instanceMatrix.M43,

                                    instanceMatrix.M14,
                                    instanceMatrix.M24,
                                    instanceMatrix.M34,
                                    instanceMatrix.M44,
                                }
                            }
                        },
                    ItemsElementName = new[]
                        {
                            ItemsChoiceType2.matrix
                        },
                    instance_geometry = new[]
                        {
                            new instance_geometry
                            {
                                url = $"#{geometryIds[node.SolidObject.Hash]}",
                                bind_material = new bind_material
                                {
                                    technique_common = node.SolidObject.Materials.Select((material, materialIdx) => new instance_material
                                    {
                                        symbol = $"material{materialIdx}",
                                        target = $"#texture-0x{material.TextureHash:X8}",
                                        bind_vertex_input = new []
                                        {
                                            new instance_materialBind_vertex_input
                                            {
                                                semantic = "TEX0",
                                                input_semantic = "TEXCOORD",
                                                input_set = 0,
                                            }
                                        }
                                    }).ToArray()
                                }
                            }
                        }
                });
            }

            visualScenes.visual_scene = new[]
            {
                new visual_scene
                {
                    id = scene.SceneName,
                    name = scene.SceneName,
                    node = sceneNodes.ToArray()
                }
            };

            collada.Items = new object[]
            {
                images,
                materials,
                effects,
                geometries,
                visualScenes
            };
            collada.scene = new COLLADAScene
            {
                instance_visual_scene = new InstanceWithExtra
                {
                    url = $"#{scene.SceneName}"
                }
            };
            collada.asset = new asset
            {
                up_axis = UpAxisType.Z_UP
            };

            collada.Save(outputPath);
        }

        private static geometry SolidToGeometry(SolidObject solidObject, string geometryId)
        {
            var verticesId = $"{geometryId}_vertices";

            var mesh = new mesh();
            var sources = new List<source>();

            var normalsSourceIds = new Dictionary<int, string>();
            var colorsSourceIds = new Dictionary<int, string>();

            var positionsSrcId = $"{geometryId}_positions";
            var positionsDataId = $"{positionsSrcId}_array";

            var uvsSrcId = $"{geometryId}_texcoords";
            var uvsDataId = $"{uvsSrcId}_array";

            var allVertices = solidObject.Materials.SelectMany(m => m.Vertices).ToList();

            sources.Add(new source
            {
                name = "position",
                id = positionsSrcId,
                Item = new float_array
                {
                    Values = allVertices
                        .SelectMany(v => new double[] { v.Position.X, v.Position.Y, v.Position.Z }).ToArray(),
                    id = positionsDataId,
                    count = (ulong)(allVertices.Count * 3)
                },
                technique_common = new sourceTechnique_common
                {
                    accessor = new accessor
                    {
                        count = (ulong)allVertices.Count,
                        offset = 0,
                        source = $"#{positionsDataId}",
                        stride = 3,
                        param = new[]
                        {
                                new param
                                {
                                    name = "X",
                                    type = "float"
                                },
                                new param
                                {
                                    name = "Y",
                                    type = "float"
                                },
                                new param
                                {
                                    name = "Z",
                                    type = "float"
                                },
                                }
                    }
                }
            });

            // TODO: Why can't we have multiple sources for texture coordinates?
            sources.Add(new source
            {
                name = $"texcoords",
                id = uvsSrcId,
                Item = new float_array
                {
                    Values = allVertices
                        .SelectMany(v => new double[] { v.TexCoords.X, v.TexCoords.Y }).ToArray(),
                    id = uvsDataId,
                    count = (ulong)(allVertices.Count * 2)
                },
                technique_common = new sourceTechnique_common
                {
                    accessor = new accessor
                    {
                        count = (ulong)allVertices.Count,
                        offset = 0,
                        source = $"#{uvsDataId}",
                        stride = 2,
                        param = new[]
                        {
                                new param
                                {
                                    name = "S",
                                    type = "float"
                                },
                                new param
                                {
                                    name = "T",
                                    type = "float"
                                },
                            }
                    }
                }
            });

            for (var i = 0; i < solidObject.Materials.Count; i++)
            {
                var material = solidObject.Materials[i];

                // Try to generate normals source

                if (material.Vertices.All(v => v.Normal != null))
                {
                    var normalsSrcId = $"{geometryId}_mat{i}_normals";
                    var normalsDataId = $"{normalsSrcId}_array";

                    sources.Add(new source
                    {
                        name = "normal",
                        id = normalsSrcId,
                        Item = new float_array
                        {
                            Values = material.Vertices
                                .SelectMany(v =>
                                {
                                    Debug.Assert(v.Normal != null, "v.Normal != null");
                                    var normal = v.Normal.Value;

                                    return new double[] { normal.X, normal.Y, normal.Z };
                                }).ToArray(),
                            id = normalsDataId,
                            count = (ulong)(material.Vertices.Length * 3)
                        },
                        technique_common = new sourceTechnique_common
                        {
                            accessor = new accessor
                            {
                                count = (ulong)material.Vertices.Length,
                                offset = 0,
                                source = $"#{normalsDataId}",
                                stride = 3,
                                param = new[]
                                {
                                        new param
                                        {
                                            name = "X",
                                            type = "float"
                                        },
                                        new param
                                        {
                                            name = "Y",
                                            type = "float"
                                        },
                                        new param
                                        {
                                            name = "Z",
                                            type = "float"
                                        },
                                    }
                            }
                        }
                    });

                    normalsSourceIds[i] = normalsSrcId;
                }

                // Try to generate colors source

                if (material.Vertices.All(v => v.Color != null))
                {
                    var colorsSrcId = $"{geometryId}_mat{i}_colors";
                    var colorsDataId = $"{colorsSrcId}_array";

                    sources.Add(new source
                    {
                        name = "color",
                        id = colorsSrcId,
                        Item = new float_array
                        {
                            Values = material.Vertices
                                .SelectMany(v =>
                                {
                                    Debug.Assert(v.Color != null, "v.Color != null");
                                    var color = v.Color.Value;

                                    var r = (color >> 16) & 0xFF;
                                    var g = (color >> 8) & 0xFF;
                                    var b = (color >> 0) & 0xFF;

                                    return new double[] { r / 255f, g / 255f, b / 255f };

                                    //return new double[] {normal.X, normal.Y, normal.Z};
                                }).ToArray(),
                            id = colorsDataId,
                            count = (ulong)(material.Vertices.Length * 3)
                        },
                        technique_common = new sourceTechnique_common
                        {
                            accessor = new accessor
                            {
                                count = (ulong)material.Vertices.Length,
                                offset = 0,
                                source = $"#{colorsDataId}",
                                stride = 3,
                                param = new[]
                                {
                                        new param
                                        {
                                            name = "R",
                                            type = "float"
                                        },
                                        new param
                                        {
                                            name = "G",
                                            type = "float"
                                        },
                                        new param
                                        {
                                            name = "B",
                                            type = "float"
                                        },
                                    }
                            }
                        }
                    });

                    colorsSourceIds[i] = colorsSrcId;
                }
            }

            mesh.source = sources.ToArray();
            mesh.vertices = new vertices
            {
                input = new[]
                {
                        new InputLocal
                        {
                            semantic = "POSITION",
                            source = $"#{positionsSrcId}",
                        }
                    },
                id = verticesId
            };

            var items = new List<object>();
            var vertexOffset = 0;

            for (var materialIndex = 0; materialIndex < solidObject.Materials.Count; materialIndex++)
            {
                var material = solidObject.Materials[materialIndex];
                var faces = new List<ushort[]>();

                for (int i = 0; i < material.Indices.Length; i += 3)
                {
                    var idx1 = material.Indices[i];
                    var idx2 = material.Indices[i + 1];
                    var idx3 = material.Indices[i + 2];

                    if (idx1 != idx2 && idx1 != idx3 && idx2 != idx3)
                    {
                        idx2 = idx3;
                        idx3 = material.Indices[i + 1];
                    }

                    faces.Add(new[] { idx1, idx2, idx3 });
                }

                var curVertexOffset = vertexOffset;
                var inputOffset = 0u;

                var inputs = new List<InputLocalOffset>
                    {
                        new InputLocalOffset
                        {
                            offset = inputOffset++, semantic = "VERTEX", source = $"#{verticesId}",
                        },
                        new InputLocalOffset
                        {
                            offset = inputOffset++, semantic = "TEXCOORD", source = $"#{uvsSrcId}"
                        }
                    };

                var hasNormals = false;
                var hasColors = false;

                if (normalsSourceIds.TryGetValue(materialIndex, out var normalsId))
                {
                    inputs.Add(new InputLocalOffset
                    {
                        semantic = "NORMAL",
                        source = $"#{normalsId}",
                        offset = inputOffset++,
                    });
                    hasNormals = true;
                }

                if (colorsSourceIds.TryGetValue(materialIndex, out var colorsId))
                {
                    inputs.Add(new InputLocalOffset
                    {
                        semantic = "COLOR",
                        source = $"#{colorsId}",
                        offset = inputOffset++,
                    });
                    hasColors = true;
                }

                items.Add(new triangles
                {
                    count = (ulong)faces.Count,
                    input = inputs.ToArray(),
                    material = $"material{materialIndex}",
                    p = string.Join(" ", faces.SelectMany(f =>
                    {
                        var index_list = new List<int>();

                        foreach (var idx in f)
                        {
                            index_list.Add(curVertexOffset + idx); // position
                            index_list.Add(curVertexOffset + idx); // UV

                            if (hasNormals)
                                index_list.Add(idx);
                            if (hasColors)
                                index_list.Add(idx);
                        }

                        return index_list;
                    }))
                });

                vertexOffset += material.Vertices.Length;
            }

            mesh.Items = items.ToArray();

            return (new geometry
            {
                name = solidObject.Name,
                id = geometryId,
                Item = mesh
            });
        }

        private static void LogInfo(string message)
        {
            Console.WriteLine($"INFO: {message}");
        }

        private static void LogVerbose(string message)
        {
            if (IsVerbose)
                Console.WriteLine($"VERBOSE: {message}");
        }
    }
}