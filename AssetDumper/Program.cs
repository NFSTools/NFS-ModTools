using System;
using System.Collections.Generic;
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

            if (game != "world")
            {
                Console.Error.WriteLine($"ERROR: Invalid game [{game}]. Valid options: world");
                Console.ReadKey();
                Environment.Exit(1);
            }

            if (!File.Exists(inputFile))
            {
                Console.Error.WriteLine($"ERROR: Cannot find input file: {inputFile}");
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
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var chunkManager = new ChunkManager(realGame);
            chunkManager.Read(inputFile);

            var results = ProcessResults(chunkManager.Chunks, Path.GetFullPath(outDirectory));

            Console.WriteLine($"[INFO] Finished dumping! Assets: {results[0]} ({results[1]} sub-assets)");
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

            foreach (var chunk in chunks.Where(c => c.Resource != null))
            {
                switch (chunk.Resource)
                {
                    case TexturePack tpk:
                        {
                            foreach (var texture in tpk.Textures)
                            {
                                ExportTexture(texture, Path.Combine(texturesDir, $"0x{texture.TexHash:X8}.dds"));
                                subAssetCount++;
                            }

                            assetCount++;

                            break;
                        }
                    case SolidList solidList:
                        {
                            foreach (var solidObject in solidList.Objects)
                            {
                                var mtlFileName = $"{solidObject.Name}.mtl";
                                var objName = $"{solidObject.Name}.obj";
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
                                        sw.WriteLine($"vt {BinaryUtil.FullPrecisionFloat(vertex.U)} {BinaryUtil.FullPrecisionFloat(-vertex.V)}");
                                    }

                                    foreach (var vertex in solidObject.Vertices)
                                    {
                                        sw.WriteLine($"v {BinaryUtil.FullPrecisionFloat(vertex.X)} {BinaryUtil.FullPrecisionFloat(vertex.Y)} {BinaryUtil.FullPrecisionFloat(vertex.Z)}");
                                    }

                                    var lastMaterial = -1;

                                    foreach (var face in solidObject.Faces)
                                    {
                                        var foundPair = new KeyValuePair<int, List<ushort[]>>();

                                        foreach (var pair in solidObject.MaterialFaces.Where(p => p.Value.Any(f => f.SequenceEqual(face.ShiftedArray()))))
                                        {
                                            foundPair = pair;
                                            break;
                                        }

                                        if (foundPair.Value != null && foundPair.Value.Count > 0)
                                        {
                                            if (foundPair.Key != lastMaterial)
                                            {
                                                lastMaterial = foundPair.Key;
                                                sw.WriteLine($"usemtl {solidObject.Materials[foundPair.Key].Name.Replace(" ", "_")}");
                                            }
                                        }
                                        else
                                        {
                                            lastMaterial = -1;
                                            sw.WriteLine("usemtl EMPTY");
                                        }

                                        if (face.Vtx1 >= solidObject.MeshDescriptor.NumVerts
                                            || face.Vtx2 >= solidObject.MeshDescriptor.NumVerts
                                            || face.Vtx3 >= solidObject.MeshDescriptor.NumVerts) break;
                                        sw.WriteLine($"f {face.Shift1 + 1}/{face.Shift1 + 1} {face.Shift2 + 1}/{face.Shift2 + 1} {face.Shift3 + 1}/{face.Shift3 + 1}");
                                    }
                                }

                                subAssetCount++;
                            }

                            assetCount++;

                            break;
                        }
                }
            }

            return new[] {assetCount, subAssetCount};
        }

        /// <summary>
        /// Export the given texture to the given path.
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="path"></param>
        internal static void ExportTexture(Texture texture, string path)
        {
            using (var fs = File.OpenWrite(path))
            using (var bw = new BinaryWriter(fs))
            {
                var ddsHeader = new DDSHeader();
                ddsHeader.Init(texture);

                BinaryUtil.WriteStruct(bw, ddsHeader);
                bw.Write(texture.Data);
            }
        }
    }
}
