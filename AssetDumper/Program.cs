using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Common;
using Common.Geometry.Data;
using Common.Textures.Data;

namespace AssetDumper
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("===============================");
            Console.WriteLine($"AssetDumper v{Assembly.GetExecutingAssembly().GetName().Version} by heyitsleo");
            Console.WriteLine("===============================");

            if (args.Length != 3)
            {
                Console.Error.WriteLine("Usage: AssetDumper.exe <game> <input file> <output directory>");
                Console.ReadKey();
                Environment.Exit(1);
            }

            var game = args[0].ToLowerInvariant();
            var inputFile = args[1];
            var outDirectory = args[2];

            if (game != "world"
                && game != "mw"
                && game != "carbon"
                && game != "prostreet"
                && game != "pstest"
                && game != "ug2"
                && game != "ug"
                && game != "undercover")
            {
                Console.Error.WriteLine($"ERROR: Invalid game [{game}]. Valid options: world, mw, carbon, prostreet, pstest, undercover, ug2, ug");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                Environment.Exit(1);
            }

            if (!File.Exists(inputFile))
            {
                Console.Error.WriteLine($"ERROR: Cannot find input file: {inputFile}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                Environment.Exit(1);
            }

            if (!Directory.Exists(outDirectory))
            {
                Console.WriteLine($"INFO: Creating directory [{outDirectory}]");
                Directory.CreateDirectory(outDirectory);
            }

            GameDetector.Game realGame;

            switch (game)
            {
                case "world":
                    {
                        realGame = GameDetector.Game.World;
                        break;
                    }
                case "mw":
                    {
                        realGame = GameDetector.Game.MostWanted;
                        break;
                    }
                case "carbon":
                    {
                        realGame = GameDetector.Game.Carbon;
                        break;
                    }
                case "prostreet":
                    {
                        realGame = GameDetector.Game.ProStreet;
                        break;
                    }
                case "pstest":
                    {
                        realGame = GameDetector.Game.ProStreetTest;
                        break;
                    }
                case "undercover":
                    {
                        realGame = GameDetector.Game.Undercover;
                        break;
                    }
                case "ug2":
                    {
                        realGame = GameDetector.Game.Underground2;
                        break;
                    }
                case "ug":
                    {
                        realGame = GameDetector.Game.Underground;
                        break;
                    }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Console.WriteLine("INFO: Reading data... please wait");

            var stopwatch = new Stopwatch();
            var chunkManager = new ChunkManager(realGame);
            stopwatch.Start();

#if !DEBUG
            try
            {
#endif
            chunkManager.Read(inputFile);
#if !DEBUG
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"ERROR: {e.Message}");
                Console.Error.WriteLine(e.StackTrace);
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }
#endif
            stopwatch.Stop();
            Console.WriteLine($"INFO: Read data in {stopwatch.ElapsedMilliseconds}ms");

            stopwatch.Reset();
            stopwatch.Start();
            var results = ProcessResults(chunkManager.Chunks, Path.GetFullPath(outDirectory));
            stopwatch.Stop();

            Console.WriteLine();
            Console.WriteLine($"INFO: Finished dumping! Assets: {results[0]} ({results[1]} sub-assets)");
            Console.WriteLine($"INFO: Dumped data in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        internal static uint[] ProcessResults(List<ChunkManager.Chunk> chunks, string outDir)
        {
            var assetCount = 0u;
            var subAssetCount = 0u;

            var texturesDir = Path.Combine(outDir, "textures");
            var modelsDir = Path.Combine(outDir, "models");

            if (!Directory.Exists(texturesDir))
            {
                Directory.CreateDirectory(texturesDir);
            }

            if (!Directory.Exists(modelsDir))
            {
                Directory.CreateDirectory(modelsDir);
            }

            Console.WriteLine("INFO: Processing texture packs...");

            var textureHashes = new List<uint>();

            // Process TPKs
            foreach (var chunk in chunks)
            {
                if (chunk.Resource is TexturePack tpk)
                {
                    foreach (var texture in tpk.Textures)
                    {
                        var path = Path.Combine(texturesDir, $"0x{texture.TexHash:X8}.dds");
                        if (!File.Exists(path))
                        {
                            ExportTexture(texture, path);
                        }
                        subAssetCount++;

                        if (!textureHashes.Contains(texture.TexHash))
                        {
                            textureHashes.Add(texture.TexHash);
                        }
                    }

                    assetCount++;
                }
            }

            Console.WriteLine("INFO: Processing object packs...");

            // Process solid lists
            foreach (var chunk in chunks)
            {
                if (chunk.Resource is SolidList solidList)
                {
                    foreach (var solidObject in solidList.Objects)
                    {
                        foreach (var material in solidObject.Materials)
                        {
                            var matNameHash = Hasher.BinHash(material.Name.ToUpper());
                            if (textureHashes.Contains(matNameHash))
                            {
                                //Console.WriteLine($"DEBUG: Migrating material {material.Name} to texture 0x{matNameHash:X8}");
                                material.TextureHash = matNameHash;
                            }
                        }

                        var mtlFileName = $"{solidObject.Name}.mtl";
                        var objName = $"{solidList.ClassType}-{solidList.PipelinePath.Replace(".bin", "").Replace('\\', '_')}-{solidObject.Name}.obj";
                        var objPath = Path.Combine(modelsDir, objName);
                        var materialLibPath = Path.Combine(modelsDir, mtlFileName);

                        using (var ms = new FileStream(materialLibPath, FileMode.Create))
                        using (var sw = new StreamWriter(ms))
                        {
                            foreach (var material in solidObject.Materials.ToList())
                            {
                                var repCount = 0;
                                for (var j = 0; j < solidObject.Materials.Count; j++)
                                {
                                    if (!string.Equals(solidObject.Materials[j].Name, material.Name)) continue;
                                    var mat = solidObject.Materials[j];

                                    if (repCount > 0)
                                    {
                                        mat.Name += $"_{repCount + 1}";
                                    }

                                    solidObject.Materials[j] = mat;

                                    repCount++;
                                }
                            }

                            foreach (var material in solidObject.Materials)
                            {
                                sw.WriteLine($"newmtl {material.Name.Replace(' ', '_')}");
                                sw.WriteLine("Ka 255 255 255");
                                sw.WriteLine("Kd 255 255 255");
                                sw.WriteLine("Ks 255 255 255");

                                var texPath = Path.Combine(texturesDir, $"0x{material.TextureHash:X8}.dds");
                                sw.WriteLine($"map_Ka {texPath}");
                                sw.WriteLine($"map_Kd {texPath}");
                                sw.WriteLine($"map_Ks {texPath}");
                            }
                        }

                        using (var fs = new FileStream(objPath, FileMode.Create))
                        using (var sw = new StreamWriter(fs))
                        {
                            sw.WriteLine($"mtllib {mtlFileName}");
                            sw.WriteLine($"obj {solidObject.Name}");

                            foreach (var vertex in solidObject.Vertices)
                            {
                                sw.WriteLine($"vt {BinaryUtil.FullPrecisionFloat(vertex.U)} {BinaryUtil.FullPrecisionFloat(vertex.V)}");
                            }

                            foreach (var vertex in solidObject.Vertices)
                            {
                                sw.WriteLine($"v {BinaryUtil.FullPrecisionFloat(vertex.X)} {BinaryUtil.FullPrecisionFloat(vertex.Y)} {BinaryUtil.FullPrecisionFloat(vertex.Z)}");
                            }

                            //var lastMaterial = -1;

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

                        subAssetCount++;
                    }

                    assetCount++;
                }
            }

            return new[] { assetCount, subAssetCount };
        }

        /// <summary>
        /// Export the given texture to the given path.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="path"></param>
        internal static void ExportTexture(Texture texture, string path)
        {
            texture.DumpToFile(path);
        }
    }
}
