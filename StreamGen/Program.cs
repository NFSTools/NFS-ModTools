using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Common;
using Common.TrackStream;
using Common.TrackStream.Data;
using Newtonsoft.Json;

namespace StreamGen
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("===============================");
            Console.WriteLine($"StreamGen v{Assembly.GetExecutingAssembly().GetName().Version} by heyitsleo");
            Console.WriteLine("===============================");

            if (args.Length != 3)
            {
                Console.Error.WriteLine("Usage: StreamGen.exe <game> <sections folder> <output file>");
                Console.Error.WriteLine(@"       ex: StreamGen.exe world C:\Users\username\Desktop\nfsw-custsections L5RA.BUN");
                Console.ReadKey();
                Environment.Exit(1);
            }

            var game = args[0];
            var sectionsDir = args[1];
            var outputFile = args[2];

            if (game != "world")
            {
                Console.Error.WriteLine($"Invalid game [{game}]. Valid options: world");
                Console.ReadKey();
                Environment.Exit(1);
            }

            if (!Directory.Exists(sectionsDir))
            {
                Console.Error.WriteLine($"Invalid sections directory [{sectionsDir}]: Does not exist");
                Console.ReadKey();
                Environment.Exit(1);
            }

            var manifestPath = Path.Combine(sectionsDir, "manifest.json");

            if (!File.Exists(manifestPath))
            {
                Console.Error.WriteLine($"Invalid sections directory [{sectionsDir}]: Cannot find manifest file!");
                Console.ReadKey();
                Environment.Exit(1);
            }

            var manifest = JsonConvert.DeserializeObject<StreamManifest>(File.ReadAllText(manifestPath));

            if (string.IsNullOrWhiteSpace(manifest.Name))
            {
                Console.Error.WriteLine($"Invalid stream manifest [{manifestPath}]: No name!");
                Console.ReadKey();
                Environment.Exit(1);
            }

            if (manifest.Sections.Count == 0)
            {
                Console.Error.WriteLine($"Invalid stream manifest [{manifestPath}]: No sections!");
                Console.ReadKey();
                Environment.Exit(1);
            }

            var finalPath = Path.Combine(Directory.GetCurrentDirectory(), outputFile);
            var bundle = new LocationBundle
            {
                File = finalPath,
                Name = manifest.Name,
                Sections = new List<StreamSection>()
            };

            var alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToLower();

            foreach (var manifestSection in manifest.Sections)
            {
                var sectionNumber = (uint) (alphabet.IndexOf(manifestSection.Name.ToLower()[0]) * 1000u);

                if (manifestSection.Name.Length > 1)
                {
                    sectionNumber += uint.Parse(manifestSection.Name.Substring(1));
                }

                var sectionFile = Path.Combine(sectionsDir, $"STREAM{manifest.Name}_{sectionNumber}.BUN");

                if (!File.Exists(sectionFile))
                {
                    Console.Error.WriteLine($"Invalid manifest section [{manifestPath} | {manifestSection.Name}]: Could not find file ({sectionFile})");
                    Console.ReadKey();
                    Environment.Exit(1);
                }

                bundle.Sections.Add(new StreamSection
                {
                    Hash = (uint) Hasher.BinHash(manifestSection.Name),
                    Position = manifestSection.Position,
                    Name = manifestSection.Name,
                    Number = sectionNumber
                });
            }

            var bundleManager = new World15Manager();
            bundleManager.WriteLocationBundle(finalPath, bundle, sectionsDir);

            Console.WriteLine($"[INFO] Saved file to: {finalPath}");

            Console.ReadKey();
        }
    }
}
