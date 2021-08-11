using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using CommandLine;
using Common;
using Common.Geometry.Data;
using Common.Scenery.Data;
using Common.Textures.Data;
using Common.TrackStream;
using Common.TrackStream.Data;
using Vector3 = System.Numerics.Vector3;

namespace AssetDumper
{
    internal class Program
    {
        internal static bool IsVerbose = false;

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
            public string ModelCombine { get; set; }

            [Option('v', "verbose", Required = false, HelpText = "Enable verbose messages.")]
            public bool Verbose { get; set; }

            [Option('m', "map", Required = false,
                HelpText = "Load the input file(s) as a map stream(s) and export assets to section folders.")]
            public bool MapMode { get; set; }

            [Option('p', "positions", Required = false, HelpText = "Bakes location data into exported object files.")]
            public bool BakePositions { get; set; }

            [Option('f', "output-dir", Required = true, HelpText = "Set the base output directory for assets.")]
            public string OutputDirectory { get; set; }

            [Option("mfmt", Required = false, Default = ObjectOutputType.Wavefront,
                HelpText = "Change the object export format.")]
            public ObjectOutputType OutputType { get; set; }
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
                options.Game,
                ChunkManager.ChunkManagerOptions.IgnoreUnknownChunks | ChunkManager.ChunkManagerOptions.SkipNull);

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
            var cm = new ChunkManager(options.Game,
                ChunkManager.ChunkManagerOptions.IgnoreUnknownChunks | ChunkManager.ChunkManagerOptions.SkipNull);
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
            var modelsDir = Path.Combine(baseDir, "models");
            var texturesDir = Path.Combine(baseDir, "textures");

            if (!Directory.Exists(modelsDir))
            {
                Directory.CreateDirectory(modelsDir);
                LogVerbose("Created models directory");
            }

            if (!Directory.Exists(texturesDir))
            {
                Directory.CreateDirectory(texturesDir);
                LogVerbose("Created textures directory");
            }

            var textureHashes = new HashSet<uint>();

            // Process texture packs
            foreach (var resource in resources.Where(r => r is TexturePack).Cast<TexturePack>())
            {
                LogVerbose($"Processing TPK: {resource.PipelinePath} ({resource.Name})");

                foreach (var texture in resource.Textures)
                {
                    var texturePath = Path.Combine(texturesDir, $"0x{texture.TexHash:X8}.dds");

                    //if (File.Exists(texturePath))
                    //{
                    //    LogVerbose($"\tSkipping texture {texture.Name}");
                    //    continue;
                    //}

                    LogVerbose($"\tExporting texture {texture.Name} to {texturePath}");
                    texture.DumpToFile(texturePath);
                    textureHashes.Add(texture.TexHash);
                }
            }

            string objExtension;

            switch (options.OutputType)
            {
                case ObjectOutputType.Wavefront:
                    objExtension = ".obj";
                    break;
                case ObjectOutputType.Collada:
                    objExtension = ".dae";
                    break;
                case ObjectOutputType.Fbx:
                    objExtension = ".fbx";
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Invalid output type: {options.OutputType}");
            }

            if (!string.IsNullOrWhiteSpace(options.ModelCombine))
            {
                LogVerbose("Combining models");

                if (options.OutputType == ObjectOutputType.Wavefront)
                {
                    ExportCombinedModelsAsObj(
                        resources.Where(r => r is SolidList).Cast<SolidList>().SelectMany(l => l.Objects).ToList(),
                        modelsDir, options.ModelCombine);
                }
            }
            else
            {
                var solidObjects = resources.OfType<SolidList>().SelectMany(l => l.Objects)
                    .ToLookup(o => o.Hash, o => o)
                    .ToDictionary(o => o.Key, o => o.First());
                foreach (var scenerySection in resources.OfType<ScenerySection>().OrderBy(s => s.SectionNumber))
                {
                    // if (scenerySection.SectionNumber == 2600)

                    LogInfo(
                        $"ScenerySection: {scenerySection.SectionNumber} ({scenerySection.Infos.Count} info entries, {scenerySection.Instances.Count} instances)");
                    ExportScenerySection(solidObjects, scenerySection, options.OutputDirectory,
                        scenerySection.SectionNumber + ".dae");
                }

                // Process model packs
                // foreach (var resource in resources.Where(r => r is SolidList).Cast<SolidList>())
                // {
                //     LogVerbose(
                //         $"Processing object pack: {resource.PipelinePath} ({resource.ClassType}, {resource.ObjectCount} objects)");
                //
                //     foreach (var solidObject in resource.Objects)
                //     {
                //         var objectPath = Path.Combine(modelsDir, $"{solidObject.Name}{objExtension}");
                //
                //         //if (File.Exists(objectPath))
                //         //{
                //         //    LogVerbose($"\tSkipping object {solidObject.Name}");
                //         //    continue;
                //         //}
                //
                //         foreach (var material in solidObject.Materials.ToList())
                //         {
                //             // Ensure every material has a unique name
                //             var repCount = 0;
                //
                //             foreach (var t in solidObject.Materials)
                //             {
                //                 if (!string.Equals(t.Name, material.Name)) continue;
                //
                //                 if (repCount > 0)
                //                 {
                //                     t.Name += $"_{repCount + 1}";
                //                 }
                //
                //                 repCount++;
                //             }
                //         }
                //
                //         LogVerbose($"\tExporting object {solidObject.Name} to {objectPath}");
                //
                //         if (options.OutputType == ObjectOutputType.Wavefront)
                //         {
                //             ExportModelAsObj(solidObject, objectPath, options);
                //         }
                //     }
                // }
            }
        }

        private static void ExportScenerySection(Dictionary<uint, SolidObject> objects, ScenerySection scenerySection, string directory, string filename)
        {
            //if (scenerySection.SectionNumber != 101)
            //    return;
            Debug.WriteLine("Exporting scenery section {0} ({1} models, {2} instances)", scenerySection.SectionNumber,
                scenerySection.Infos.Count, scenerySection.Instances.Count);

            var collada = new COLLADA();
            collada.version = VersionType.Item141;

            var solidsToAdd = scenerySection.Infos.Select(i => i.SolidKey)
                .Distinct()
                .Where(objects.ContainsKey)
                .Select(k => objects[k]).ToList();

            var texturesToAdd = solidsToAdd.SelectMany(s => s.TextureHashes).ToList();

            // Build image library
            var images = new library_images();
            var imageList = new List<image>();

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
                    Item = Path.Combine("textures", $"0x{textureId:X8}.dds"),
                    depth = 1
                });
            }

            images.image = imageList.ToArray();

            // Build material library
            var materials = new library_materials();
            var materialList = new List<material>();

            foreach (var textureId in texturesToAdd)
            {
                /*
                symbol = $"material{materialIdx}",
                target = $"#texture-0x{material.TextureHash:X8}"
                 */
                materialList.Add(new material
                {
                    id = $"texture-0x{textureId:X8}",
                    name = $"TextureMTL-0x{textureId:X8}",
                    instance_effect = new instance_effect
                    {
                        url = $"#texture-0x{textureId:X8}-fx"
                    },
                });
            }

            materials.material = materialList.ToArray();

            // Build effect library
            var effects = new library_effects();
            var effectList = new List<effect>();

            foreach (var textureId in texturesToAdd)
            {
                /*
                symbol = $"material{materialIdx}",
                target = $"#texture-0x{material.TextureHash:X8}"
                 */
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

            effects.effect = effectList.ToArray();

            // Build geometry library

            var geometries = new library_geometries();
            var geometryList = new List<geometry>();
            var geometryIds = new Dictionary<uint, string>();

            foreach (var solidObject in solidsToAdd)
            {
                //Debug.WriteLine("Adding model '{0}' (0x{1:X8}) to COLLADA file", solidObject.Name, solidObject.Hash);
                var geometryId = $"{scenerySection.SectionNumber}-0x{solidObject.Hash:X8}";
                var positionsSourceId = $"{scenerySection.SectionNumber}-{solidObject.Name}_positions";
                var positionsDataId = $"{scenerySection.SectionNumber}-{solidObject.Name}_positions_array";
                var normalsSourceId = $"{scenerySection.SectionNumber}-{solidObject.Name}_normals";
                var normalsDataId = $"{scenerySection.SectionNumber}-{solidObject.Name}_normals_array";
                var uvSourceId = $"{scenerySection.SectionNumber}-{solidObject.Name}_texcoords";
                var uvDataId = $"{scenerySection.SectionNumber}-{solidObject.Name}_texcoords_array";
                var verticesId = $"{scenerySection.SectionNumber}-{solidObject.Name}_vertices";

                geometryIds.Add(solidObject.Hash, geometryId);

                geometryList.Add(new geometry
                {
                    name = solidObject.Name,
                    id = geometryId,
                    Item = new mesh
                    {
                        source = new[]
                        {
                            new source
                            {
                                name = "position",
                                id = positionsSourceId,
                                Item = new float_array
                                {
                                    Values = solidObject.Vertices.SelectMany(v => new double[] { v.X, v.Y, v.Z }).ToArray(),
                                    id = positionsDataId,
                                    count = (ulong) (solidObject.Vertices.Length * 3)
                                },
                                technique_common = new sourceTechnique_common
                                {
                                    accessor = new accessor
                                    {
                                        count = (ulong) solidObject.Vertices.Length,
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
                            },
                            new source
                            {
                                name = "normal",
                                id = normalsSourceId,
                                Item = new float_array
                                {
                                    Values = solidObject.Vertices.SelectMany(v => new double[] { v.NormalX, v.NormalY, v.NormalZ }).ToArray(),
                                    id = normalsDataId,
                                    count = (ulong) (solidObject.Vertices.Length * 3)
                                },
                                technique_common = new sourceTechnique_common
                                {
                                    accessor = new accessor
                                    {
                                        count = (ulong) solidObject.Vertices.Length,
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
                            },
                            new source
                            {
                                name = "texcoord",
                                id = uvSourceId,
                                Item = new float_array
                                {
                                    Values = solidObject.Vertices.SelectMany(v => new double[] { v.U, v.V }).ToArray(),
                                    id = uvDataId,
                                    count = (ulong) (solidObject.Vertices.Length * 2)
                                },
                                technique_common = new sourceTechnique_common
                                {
                                    accessor = new accessor
                                    {
                                        count = (ulong) solidObject.Vertices.Length,
                                        offset = 0,
                                        source = $"#{uvDataId}",
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
                            },
                        },
                        vertices = new vertices
                        {
                            input = new InputLocal[]
                            {
                                new InputLocal
                                {
                                    semantic = "POSITION",
                                    source = $"#{positionsSourceId}"
                                }
                            },
                            id = verticesId
                        },
                        Items = solidObject.Materials.Select((material, materialIdx) =>
                        {
                            var faces = solidObject.Faces.Where(f => f.MaterialIndex == materialIdx).ToList();
                            return new triangles
                            {
                                count = (ulong)faces.Count,
                                input = new[]
                                        {
                                            new InputLocalOffset
                                            {
                                                offset = 0,
                                                semantic = "VERTEX",
                                                source = $"#{verticesId}"
                                            },
                                            new InputLocalOffset
                                            {
                                                offset = 1,
                                                semantic = "NORMAL",
                                                source = $"#{normalsSourceId}",
                                            },
                                            new InputLocalOffset
                                            {
                                                offset = 2,
                                                semantic = "TEXCOORD",
                                                source = $"#{uvSourceId}",
                                                set = 0
                                            },
                                        },
                                p = string.Join(" ", faces.SelectMany(f => new[]
                                {
                                    f.Vtx1, f.Vtx1, f.Vtx1,
                                    f.Vtx2, f.Vtx2, f.Vtx2,
                                    f.Vtx3, f.Vtx3, f.Vtx3,
                                })),
                                material = $"material{materialIdx}"
                            };
                        }).Cast<object>().ToArray()
                    }
                });
            }

            geometries.geometry = geometryList.ToArray();

            library_visual_scenes visualScenes = new library_visual_scenes();

            var sceneNodes = new List<node>();

            for (int idx = 0; idx < scenerySection.Instances.Count; idx++)
            {
                var instance = scenerySection.Instances[idx];
                var info = scenerySection.Infos[instance.InfoIndex];
                var matrix = Matrix4x4.CreateScale(instance.Scale) * Matrix4x4.CreateFromQuaternion(instance.Rotation) *
                             Matrix4x4.CreateTranslation(instance.Position);

                sceneNodes.Add(new node
                {
                    name = info.Name,
                    id = $"scenery_instance_{scenerySection.SectionNumber}_{idx}",
                    Items = new object[]
                        {
                            new matrix
                            {
                                sid = "mat",
                                Values = new double[]
                                {
                                    matrix.M11,
                                    matrix.M21,
                                    matrix.M31,
                                    matrix.M41,
                                    matrix.M12,
                                    matrix.M22,
                                    matrix.M32,
                                    matrix.M42,
                                    matrix.M13,
                                    matrix.M23,
                                    matrix.M33,
                                    matrix.M43,
                                    matrix.M14,
                                    matrix.M24,
                                    matrix.M34,
                                    matrix.M44,
                                }
                            },
                        },
                    ItemsElementName = new[]
                        {
                            ItemsChoiceType2.matrix
                        },
                    instance_geometry = new[]
                        {
                            new instance_geometry
                            {
                                url = $"#{geometryIds[info.SolidKey]}",
                                bind_material = new bind_material
                                {
                                    technique_common = objects[info.SolidKey].Materials.Select((material, materialIdx) => new instance_material
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

            var sceneId = $"scenery_section_{scenerySection.SectionNumber}";
            visualScenes.visual_scene = new visual_scene[]
            {
                new visual_scene
                {
                    id = sceneId,
                    name = scenerySection.SectionNumber.ToString(),
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
                    url = $"#{sceneId}"
                }
            };
            collada.asset = new asset
            {
                up_axis = UpAxisType.Z_UP
            };

            collada.Save(Path.Combine(directory, filename));

            //if (scenerySection.SectionNumber==203)
            //    ExportModelAsObj(solidsToAdd.Find(s => s.Hash == 0x67DCA9C0), "0x67DCA9C0.obj", new Options { });
            //var fullPath = Path.Combine(directory, filename);
            //using (var sw = new StreamWriter(File.OpenWrite(fullPath)))
            //{
            //    sw.WriteLine($"obj COMBINED");
            //    var vertBaseOffset = 0;
            //    for (var index = 0; index < scenerySection.Instances.Count; index++)
            //    {
            //        var sceneryInstance = scenerySection.Instances[index];
            //        if (sceneryInstance.InfoIndex >= scenerySection.Infos.Count)
            //        {
            //            LogInfo($"WARN: Weirdness detected in ScenerySection - {sceneryInstance.InfoIndex} >= {scenerySection.Infos.Count}");
            //            continue;
            //        }
            //        var sceneryInfo = scenerySection.Infos[sceneryInstance.InfoIndex];
            //        var solidObject = solidObjects[sceneryInfo.SolidKey];

            //        // LogInfo(solidObject.Name);

            //        {
            //            sw.WriteLine($"g scenery_{index}_{solidObject.Name}");

            //            foreach (var vertex in solidObject.Vertices)
            //            {
            //                sw.WriteLine(
            //                    $"vt {BinaryUtil.FullPrecisionFloat(vertex.U)} {BinaryUtil.FullPrecisionFloat(vertex.V)}");
            //            }

            //            foreach (var vertex in solidObject.Vertices)
            //            {
            //                var position = Vector3.Transform(
            //                    Vector3.Multiply(Vector3.Add(new Vector3(vertex.X, vertex.Y, vertex.Z), sceneryInstance.Position), sceneryInstance.Scale),
            //                    sceneryInstance.Rotation);

            //                sw.WriteLine(
            //                    $"v {BinaryUtil.FullPrecisionFloat(position.X)} {BinaryUtil.FullPrecisionFloat(position.Y)} {BinaryUtil.FullPrecisionFloat(position.Z)}");
            //            }

            //            for (var i = 0; i < solidObject.Materials.Count; i++)
            //            {
            //                var faces = solidObject.Faces.Where(f => f.MaterialIndex == i).ToList();

            //                foreach (var face in faces)
            //                {
            //                    if (solidObject.MeshDescriptor.NumVerts > 0)
            //                    {
            //                        if (face.Vtx1 >= solidObject.MeshDescriptor.NumVerts
            //                            || face.Vtx2 >= solidObject.MeshDescriptor.NumVerts
            //                            || face.Vtx3 >= solidObject.MeshDescriptor.NumVerts) break;
            //                    }

            //                    sw.WriteLine(
            //                        $"f {vertBaseOffset + face.Vtx1 + 1}/{vertBaseOffset + face.Vtx1 + 1} {vertBaseOffset + face.Vtx2 + 1}/{vertBaseOffset + face.Vtx2 + 1} {vertBaseOffset + face.Vtx3 + 1}/{vertBaseOffset + face.Vtx3 + 1}");
            //                }
            //            }

            //            vertBaseOffset += solidObject.Vertices.Length;

            //            sw.Flush();
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Export multiple solid objects to a Wavefront .obj file.
        /// </summary>
        /// <param name="solidObjects"></param>
        /// <param name="directory"></param>
        /// <param name="filename"></param>
        private static void ExportCombinedModelsAsObj(List<SolidObject> solidObjects, string directory, string filename)
        {
            var fullPath = Path.Combine(directory, filename);
            var mtlPath = fullPath.Replace(".obj", ".mtl");

            foreach (var solidObject in solidObjects)
            {
                solidObject.Materials.ForEach(m => m.Name = $"{solidObject.Name}_{m.Name}");

                foreach (var material in solidObject.Materials.ToList())
                {
                    // Ensure every material has a unique name
                    var repCount = 0;

                    foreach (var t in solidObject.Materials)
                    {
                        if (!string.Equals(t.Name, material.Name)) continue;

                        if (repCount > 0)
                        {
                            t.Name += $"_{repCount + 1}";
                        }

                        repCount++;
                    }
                }
            }

            // Generate material library
            using (var sw = new StreamWriter(File.OpenWrite(mtlPath)))
            {
                sw.WriteLine("# Generated by AssetDumper - the toolkit that's not the toolkit.");

                foreach (var solidObject in solidObjects)
                {
                    foreach (var material in solidObject.Materials)
                    {
                        sw.WriteLine($"newmtl {material.Name.Replace(' ', '_')}");
                        sw.WriteLine("Ka 255 255 255");
                        sw.WriteLine("Kd 255 255 255");
                        sw.WriteLine("Ks 255 255 255");

                        var texPath = $"../textures/0x{material.TextureHash:X8}.dds";
                        sw.WriteLine($"map_Ka {texPath}");
                        sw.WriteLine($"map_Kd {texPath}");
                        sw.WriteLine($"map_Ks {texPath}");
                    }
                }
            }

            using (var sw = new StreamWriter(File.OpenWrite(fullPath)))
            {
                sw.WriteLine("# Generated by AssetDumper - the toolkit that's not the toolkit.");
                sw.WriteLine($"mtllib {Path.GetFileName(mtlPath)}");

                sw.WriteLine($"obj COMBINED");

                foreach (var solidObject in solidObjects)
                {
                    sw.WriteLine($"g {solidObject.Name}");

                    foreach (var vertex in solidObject.Vertices)
                    {
                        sw.WriteLine(
                            $"vt {BinaryUtil.FullPrecisionFloat(vertex.U)} {BinaryUtil.FullPrecisionFloat(vertex.V)}");
                    }

                    var transformX = solidObject.Transform.M41;
                    var transformY = solidObject.Transform.M42;
                    var transformZ = solidObject.Transform.M43;

                    foreach (var vertex in solidObject.Vertices)
                    {
                        sw.WriteLine(
                            $"v {BinaryUtil.FullPrecisionFloat(vertex.X + transformX)} {BinaryUtil.FullPrecisionFloat(vertex.Y + transformY)} {BinaryUtil.FullPrecisionFloat(vertex.Z + transformZ)}");
                    }

                    for (var i = 0; i < solidObject.Materials.Count; i++)
                    {
                        var material = solidObject.Materials[i];
                        var faces = solidObject.Faces.Where(f => f.MaterialIndex == i).ToList();

                        sw.WriteLine($"usemtl {material.Name.Replace(' ', '_')}");

                        foreach (var face in faces)
                        {
                            if (solidObject.MeshDescriptor.NumVerts > 0)
                            {
                                if (face.Vtx1 >= solidObject.MeshDescriptor.NumVerts
                                    || face.Vtx2 >= solidObject.MeshDescriptor.NumVerts
                                    || face.Vtx3 >= solidObject.MeshDescriptor.NumVerts) break;
                            }

                            sw.WriteLine(
                                $"f {face.Vtx1 + 1}/{face.Vtx1 + 1} {face.Vtx2 + 1}/{face.Vtx2 + 1} {face.Vtx3 + 1}/{face.Vtx3 + 1}");
                        }
                    }

                    sw.Flush();
                }
            }
        }

        /// <summary>
        /// Export a solid object as a Wavefront .obj file.
        /// </summary>
        /// <param name="solidObject"></param>
        /// <param name="filePath"></param>
        /// <param name="options"></param>
        private static void ExportModelAsObj(SolidObject solidObject, string filePath, Options options)
        {
            var mtlPath = filePath.Replace(".obj", ".mtl");

            // Generate material library
            using (var sw = new StreamWriter(File.OpenWrite(mtlPath)))
            {
                sw.WriteLine("# Generated by AssetDumper - the toolkit that's not the toolkit.");
                foreach (var material in solidObject.Materials)
                {
                    sw.WriteLine($"newmtl {material.Name.Replace(' ', '_')}");
                    sw.WriteLine("Ka 255 255 255");
                    sw.WriteLine("Kd 255 255 255");
                    sw.WriteLine("Ks 255 255 255");

                    var texPath = $"../textures/0x{material.TextureHash:X8}.dds";
                    sw.WriteLine($"map_Ka {texPath}");
                    sw.WriteLine($"map_Kd {texPath}");
                    sw.WriteLine($"map_Ks {texPath}");
                }
            }

            using (var fs = new FileStream(filePath, FileMode.Create))
            using (var sw = new StreamWriter(fs))
            {
                sw.WriteLine("# Generated by AssetDumper - the toolkit that's not the toolkit.");
                sw.WriteLine($"mtllib {Path.GetFileName(mtlPath)}");
                sw.WriteLine($"obj {solidObject.Name}");

                foreach (var vertex in solidObject.Vertices)
                {
                    sw.WriteLine(
                        $"vt {BinaryUtil.FullPrecisionFloat(vertex.U)} {BinaryUtil.FullPrecisionFloat(vertex.V)}");
                }

                var transformX = options.BakePositions ? solidObject.Transform.M41 : 0.0f;
                var transformY = options.BakePositions ? solidObject.Transform.M42 : 0.0f;
                var transformZ = options.BakePositions ? solidObject.Transform.M43 : 0.0f;

                foreach (var vertex in solidObject.Vertices)
                {
                    sw.WriteLine(
                        $"v {BinaryUtil.FullPrecisionFloat(vertex.X + transformX)} {BinaryUtil.FullPrecisionFloat(vertex.Y + transformY)} {BinaryUtil.FullPrecisionFloat(vertex.Z + transformZ)}");
                }

                for (var i = 0; i < solidObject.Materials.Count; i++)
                {
                    var material = solidObject.Materials[i];
                    var faces = solidObject.Faces.Where(f => f.MaterialIndex == i).ToList();

                    sw.WriteLine($"usemtl {material.Name.Replace(' ', '_')}");

                    foreach (var face in faces)
                    {
                        if (solidObject.MeshDescriptor.NumVerts > 0)
                        {
                            if (face.Vtx1 >= solidObject.MeshDescriptor.NumVerts
                                || face.Vtx2 >= solidObject.MeshDescriptor.NumVerts
                                || face.Vtx3 >= solidObject.MeshDescriptor.NumVerts) break;
                        }

                        sw.WriteLine(
                            $"f {face.Vtx1 + 1}/{face.Vtx1 + 1} {face.Vtx2 + 1}/{face.Vtx2 + 1} {face.Vtx3 + 1}/{face.Vtx3 + 1}");
                    }
                }
            }
        }

        private static void LogInfo(string message)
        {
            Console.WriteLine($"INFO:    {message}");
        }

        private static void LogVerbose(string message)
        {
            if (IsVerbose)
                Console.WriteLine($"VERBOSE: {message}");
        }
    }
}