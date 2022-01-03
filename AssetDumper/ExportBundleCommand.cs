using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using CommandLine;
using Common;
using Common.Geometry.Data;
using Common.Scenery.Data;
using Common.Textures.Data;
using JetBrains.Annotations;
using Serilog;

namespace AssetDumper;

public enum ModelExportMode
{
    ExportAll,
    ExportPacks,
    ExportSceneryInstances,
    ExportScenerySections,
}

[Verb("export", HelpText = "Export assets from one or more asset bundles")]
public class ExportBundleCommand : BaseCommand
{
    [Value(0, Required = true, HelpText = "The files to read.")]
    public IEnumerable<string> Files { get; [UsedImplicitly] set; }

    [Option('g', "game", HelpText = "The game that the bundle files come from.", Required = true)]
    public GameDetector.Game Game { get; [UsedImplicitly] set; }

    [Option('o', "output", HelpText = "The directory to export files to.", Required = false)]
    public string OutputDirectory { get; [UsedImplicitly] set; }

    [Option("obj-mode",
        HelpText =
            $"Object export mode ({nameof(ModelExportMode.ExportAll)}, {nameof(ModelExportMode.ExportPacks)}, {nameof(ModelExportMode.ExportScenerySections)})")]
    public ModelExportMode ModelExportMode { get; [UsedImplicitly] set; } = ModelExportMode.ExportAll;

    public override int Execute()
    {
        if (OutputDirectory != null)
            Directory.CreateDirectory(OutputDirectory);
        else
            Log.Information("Operating in dry-run mode: no files will be exported");

        foreach (var file in Files)
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException("Can't find file", file);
            }

            Log.Information("Reading {FilePath}", file);
            var cm = new ChunkManager(Game);
            var sw = Stopwatch.StartNew();
            cm.Read(file);
            sw.Stop();
            var resources = (from c in cm.Chunks
                where c.Resource != null
                select c.Resource).ToList();
            Log.Information("Read {NumResources} resource(s) from {FilePath} in {ElapsedDurationMS}ms", resources.Count,
                file, sw.ElapsedMilliseconds);
            if (OutputDirectory != null)
            {
                sw.Restart();
                ProcessResources(resources, OutputDirectory);
                sw.Stop();
                Log.Information("Exported resources in {ElapsedDurationMS}ms", sw.ElapsedMilliseconds);
            }
        }

        return 0;
    }

    private void ProcessResources(IReadOnlyCollection<BasicResource> resources, string outputDir)
    {
        const string texturesBaseDir = "textures";
        var texturesDir = Path.Combine(outputDir, texturesBaseDir);

        if (!Directory.Exists(texturesDir))
        {
            Directory.CreateDirectory(texturesDir);
        }

        Log.Information("Processing texture packs...");

        // Process texture packs
        foreach (var resource in resources.OfType<TexturePack>())
        {
            foreach (var texture in resource.Textures)
            {
                var texturePath = Path.Combine(texturesDir, $"0x{texture.TexHash:X8}.dds");
                texture.DumpToFile(texturePath);
            }
        }

        var solidLists = resources.OfType<SolidList>().ToList();
        var scenerySections = resources.OfType<ScenerySection>().ToList();

        var exportMode = ModelExportMode;
        if (scenerySections.Any() &&
            exportMode is ModelExportMode.ExportScenerySections or ModelExportMode.ExportSceneryInstances)
        {
            Log.Information("Processing scenery sections...");
            var solidObjectLookup = solidLists.SelectMany(l => l.Objects)
                .ToLookup(o => o.Hash, o => o)
                .ToDictionary(o => o.Key, o => o.First());
            if (exportMode == ModelExportMode.ExportScenerySections)
            {
                foreach (var scenerySection in scenerySections.OrderBy(s => s.SectionNumber))
                {
                    ExportScenerySection(solidObjectLookup, scenerySection, Path.Combine(outputDir,
                        scenerySection.SectionNumber + ".dae"), texturesBaseDir);
                }
            }
            else
            {
                var groupedInstances = new Dictionary<uint, List<Matrix4x4>>();

                foreach (var scenerySection in scenerySections)
                {
                    foreach (var sceneryInstance in scenerySection.Instances)
                    {
                        var sceneryInfo = scenerySection.Infos[sceneryInstance.InfoIndex];
                        var solidKey = sceneryInfo.SolidKey;

                        if (!solidObjectLookup.TryGetValue(solidKey, out var solid))
                        {
                            Log.Warning("Can't find object with key 0x{MissingSolidKey:X8} for scenery {SceneryName}",
                                solidKey, sceneryInfo.Name);
                            continue;
                        }

                        var solidName = solid.Name.ToUpperInvariant();
                        if (solidName.StartsWith("RFL_") || solidName.StartsWith("SHD_") ||
                            solidName.StartsWith("SHADOW"))
                            // Skip reflections and shadow maps
                            continue;

                        if (!groupedInstances.TryGetValue(solidKey, out var matrixList))
                        {
                            matrixList = groupedInstances[solidKey] = new List<Matrix4x4>();
                        }

                        Matrix4x4 transform;

                        if (sceneryInfo.IsDeinstanced)
                        {
                            Debug.Assert(sceneryInstance.Transform == Matrix4x4.Identity);
                            transform = solid.Transform;
                        }
                        else
                        {
                            transform = sceneryInstance.Transform;
                        }

                        matrixList.Add(transform);
                    }
                }

                Log.Information("Exporting {NumObjects} objects with {NumTransforms} total transformations",
                    groupedInstances.Count, groupedInstances.Select(gi => gi.Value).Sum(tl => tl.Count));

                foreach (var (solidKey, solidTransforms) in groupedInstances)
                {
                    var solid = solidObjectLookup[solidKey];
                    ExportSingleSolid(solid, Path.Combine(outputDir, $"{solid.Name}.dae"), texturesBaseDir);

                    using var solidTransformWriter = new StreamWriter(Path.Combine(outputDir, $"{solid.Name}.txt"));
                    var sb = new StringBuilder();

                    foreach (var solidTransform in solidTransforms)
                    {
                        var position = new Vector3(solidTransform.M41, solidTransform.M42, solidTransform.M43);
                        var rotation = Quaternion.CreateFromRotationMatrix(solidTransform);
                        sb.AppendLine(CultureInfo.InvariantCulture,
                            $"{position.X} {position.Y} {position.Z}\t{rotation.W} {rotation.X} {rotation.Y} {rotation.Z}");
                    }

                    solidTransformWriter.Write(sb);
                }
            }
        }
        else if (solidLists.Any())
        {
            Log.Information("Processing model packs...");

            if (exportMode == ModelExportMode.ExportPacks)
            {
                Log.Information("Exporting {NumSolidLists} solid packs", solidLists.Count);

                var outputPath = Path.Combine(outputDir, "combined.dae");
                Log.Information("Exporting all models to {OutputPath}", outputPath);
                ExportMultipleSolids(solidLists.SelectMany(s => s.Objects).ToList(),
                    outputPath, texturesBaseDir);
            }
            else
            {
                Log.Information("Exporting individual models");

                foreach (var solidObject in solidLists.SelectMany(l => l.Objects))
                {
                    ExportSingleSolid(solidObject,
                        Path.Combine(outputDir, $"{solidObject.Name}.dae"), texturesBaseDir);
                }
            }
        }
    }

    private static void ExportScenerySection(IReadOnlyDictionary<uint, SolidObject> objects,
        ScenerySection scenerySection, string outputPath, string texturesDirectory)
    {
        Log.Information(
            "Exporting scenery section {ScenerySectionNumber} ({SceneryInfoCount} models, {SceneryInstanceCount} instances)",
            scenerySection.SectionNumber, scenerySection.Infos.Count, scenerySection.Instances.Count);

        var sceneNodes = new List<SceneExportNode>();

        foreach (var instance in scenerySection.Instances)
        {
            var info = scenerySection.Infos[instance.InfoIndex];

            if (!objects.TryGetValue(info.SolidKey, out var solid))
                continue;

            var solidName = solid.Name.ToUpperInvariant();
            if (solidName.StartsWith("RFL_") || solidName.StartsWith("SHD_") ||
                solidName.StartsWith("SHADOW"))
                // Skip reflections and shadow maps
                continue;

            sceneNodes.Add(new SceneExportNode(solid, info.Name, instance.Transform));
        }

        var scene = new SceneExport(sceneNodes, $"ScenerySection_{scenerySection.SectionNumber}");
        ExportScene(scene, outputPath, texturesDirectory);
    }

    private static void ExportMultipleSolids(IEnumerable<SolidObject> solidObjects, string outputPath,
        string texturesDirectory)
    {
        var sceneNodes = solidObjects.Select(solid => new SceneExportNode(solid, solid.Name, solid.Transform)).ToList();

        var scene = new SceneExport(sceneNodes, "MultiSolidExport");
        ExportScene(scene, outputPath, texturesDirectory);
    }

    private static void ExportSingleSolid(SolidObject solidObject, string outputPath, string texturesDirectory)
    {
        var sceneNodes = new List<SceneExportNode>
        {
            new(solidObject, solidObject.Name, solidObject.Transform)
        };

        var scene = new SceneExport(sceneNodes, solidObject.Name);
        ExportScene(scene, outputPath, texturesDirectory);
    }

    private static string GetMaterialId(SolidObjectMaterial material)
    {
        return $"texture-0x{material.TextureHash:X8}";
    }

    private static string GetMaterialName(SolidObjectMaterial material)
    {
        return material.Name ?? $"TextureMTL-0x{material.TextureHash:X8}";
    }

    private static void ExportScene(SceneExport scene, string outputPath, string texturesDirectory)
    {
        var collada = new COLLADA
        {
            version = VersionType.Item141
        };

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
                                    init_from = new[]
                                    {
                                        new fx_surface_init_from_common { Value = $"texture-0x{textureId:X8}-img" }
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
        materials.material = (from solidObject in solidsToAdd
            from solidObjectMaterial in solidObject.Materials
            let materialId = GetMaterialId(solidObjectMaterial)
            let materialName = GetMaterialName(solidObjectMaterial)
            select new material
            {
                id = materialId, name = materialName,
                instance_effect = new instance_effect { url = $"#texture-0x{solidObjectMaterial.TextureHash:X8}-fx" }
            }).ToArray();
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

        var visualScenes = new library_visual_scenes();

        var sceneNodes = new List<node>();

        for (var idx = 0; idx < scene.Nodes.Count; idx++)
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
                            technique_common = node.SolidObject.Materials.Select((material, materialIdx) =>
                                new instance_material
                                {
                                    symbol = $"material{materialIdx}",
                                    target = $"#{GetMaterialId(material)}",
                                    bind_vertex_input = new[]
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
                    .SelectMany(v => new double[] { v.TexCoords.X, -v.TexCoords.Y }).ToArray(),
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

            for (var i = 0; i < material.Indices.Length; i += 3)
            {
                var idx1 = material.Indices[i];
                var idx2 = material.Indices[i + 1];
                var idx3 = material.Indices[i + 2];

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

    private class SceneExport
    {
        public SceneExport(List<SceneExportNode> nodes, string sceneName)
        {
            Nodes = nodes;
            SceneName = sceneName;
        }

        public List<SceneExportNode> Nodes { get; }
        public string SceneName { get; }
    }
}

internal class SceneExportNode
{
    public SceneExportNode(SolidObject solidObject, string name, Matrix4x4 transform)
    {
        SolidObject = solidObject;
        Name = name;
        Transform = transform;
    }

    public SolidObject SolidObject { get; }
    public string Name { get; }
    public Matrix4x4 Transform { get; }
}