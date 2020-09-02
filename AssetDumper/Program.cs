using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using Common;
using Common.Geometry.Data;
using Common.Textures.Data;
using Common.TrackStream;
using Common.TrackStream.Data;

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

            [Option('m', "map", Required = false, HelpText = "Load the input file(s) as a map stream(s) and export assets to section folders.")]
            public bool MapMode { get; set; }

            [Option('p', "positions", Required = false, HelpText = "Bakes location data into exported object files.")]
            public bool BakePositions { get; set; }

            [Option('f', "output-dir", Required = true, HelpText = "Set the base output directory for assets.")]
            public string OutputDirectory { get; set; }

            [Option("mfmt", Required = false, Default = ObjectOutputType.Wavefront, HelpText = "Change the object export format.")]
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
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
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
            var cm = new ChunkManager(options.Game, ChunkManager.ChunkManagerOptions.IgnoreUnknownChunks | ChunkManager.ChunkManagerOptions.SkipNull);
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
                    var ws = (WorldStreamSection) streamSection;
                    var sectionPath = Path.Combine(
                        Path.GetDirectoryName(file) ?? throw new InvalidOperationException(), $"STREAM{bundle.Name}_" + (ws.FragmentFileId == 0 ? $"{ws.Number}" : $"{ws.FragmentFileId:X8}"));
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
                    var data = masterStream.ReadBytesRequired((int) streamSection.Size);
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
                        resources.Where(r => r is SolidList).Cast<SolidList>().SelectMany(l => l.Objects).ToList(), modelsDir, options.ModelCombine);
                }
            }
            else
            {
                // Process model packs
                foreach (var resource in resources.Where(r => r is SolidList).Cast<SolidList>())
                {
                    LogVerbose(
                        $"Processing object pack: {resource.PipelinePath} ({resource.ClassType}, {resource.ObjectCount} objects)");

                    foreach (var solidObject in resource.Objects)
                    {
                        var objectPath = Path.Combine(modelsDir, $"{solidObject.Name}{objExtension}");

                        //if (File.Exists(objectPath))
                        //{
                        //    LogVerbose($"\tSkipping object {solidObject.Name}");
                        //    continue;
                        //}

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

                        LogVerbose($"\tExporting object {solidObject.Name} to {objectPath}");

                        if (options.OutputType == ObjectOutputType.Wavefront)
                        {
                            ExportModelAsObj(solidObject, objectPath, options);
                        }
                    }
                }
            }
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
                        sw.WriteLine($"vt {BinaryUtil.FullPrecisionFloat(vertex.U)} {BinaryUtil.FullPrecisionFloat(vertex.V)}");
                    }

                    var transformX = solidObject.Transform[3, 0];
                    var transformY = solidObject.Transform[3, 1];
                    var transformZ = solidObject.Transform[3, 2];

                    foreach (var vertex in solidObject.Vertices)
                    {
                        sw.WriteLine($"v {BinaryUtil.FullPrecisionFloat(vertex.X + transformX)} {BinaryUtil.FullPrecisionFloat(vertex.Y + transformY)} {BinaryUtil.FullPrecisionFloat(vertex.Z + transformZ)}");
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

                            sw.WriteLine($"f {face.Vtx1 + 1}/{face.Vtx1 + 1} {face.Vtx2 + 1}/{face.Vtx2 + 1} {face.Vtx3 + 1}/{face.Vtx3 + 1}");
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
                    sw.WriteLine($"vt {BinaryUtil.FullPrecisionFloat(vertex.U)} {BinaryUtil.FullPrecisionFloat(vertex.V)}");
                }

                var transformX = options.BakePositions ? solidObject.Transform[3, 0] : 0.0f;
                var transformY = options.BakePositions ? solidObject.Transform[3, 1] : 0.0f;
                var transformZ = options.BakePositions ? solidObject.Transform[3, 2] : 0.0f;

                foreach (var vertex in solidObject.Vertices)
                {
                    sw.WriteLine($"v {BinaryUtil.FullPrecisionFloat(vertex.X + transformX)} {BinaryUtil.FullPrecisionFloat(vertex.Y + transformY)} {BinaryUtil.FullPrecisionFloat(vertex.Z + transformZ)}");
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

                        sw.WriteLine($"f {face.Vtx1 + 1}/{face.Vtx1 + 1} {face.Vtx2 + 1}/{face.Vtx2 + 1} {face.Vtx3 + 1}/{face.Vtx3 + 1}");
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
