using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using CommandLine;
using Common;
using Common.Geometry.Data;
using Common.Lights.Data;
using Common.Scenery.Data;
using Common.Textures.Data;
using FBXSharp;
using FBXSharp.Core;
using FBXSharp.Objective;
using FBXSharp.ValueTypes;
using JetBrains.Annotations;
using Serilog;
using Matrix4x4 = System.Numerics.Matrix4x4;
using Quaternion = System.Numerics.Quaternion;
using Texture = Common.Textures.Data.Texture;
using Vector3 = System.Numerics.Vector3;

namespace AssetDumper;

public enum ModelExportMode
{
    ExportAll,
    ExportPacks,
    ExportSceneryInstances,
    ExportScenerySections,
}

public enum SceneFormat
{
    Collada,
    Fbx
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

    [Option("export-lights",
        HelpText =
            "When enabled, lights will be exported to the COLLADA scene. ONLY VALID WITH ExportScenerySections.")]
    public bool ExportLights { get; [UsedImplicitly] set; }

    [Option("scene-format", HelpText = "The format (Collada or Fbx) to export scenes in")]
    public SceneFormat SceneFormat { get; [UsedImplicitly] set; } = SceneFormat.Collada;

    public override int Execute()
    {
        if (OutputDirectory != null)
            Directory.CreateDirectory(OutputDirectory);
        else
            Log.Information("Operating in dry-run mode: no files will be exported");

        var realFileList = new List<string>();

        foreach (var file in Files)
            if (!File.Exists(file))
            {
                if (Directory.Exists(file))
                {
                    var files = Directory.GetFiles(file);
                    Log.Information("Adding {NumFiles} file(s) from directory {Directory} to file list", files.Length,
                        file);
                    realFileList.AddRange(files);
                }
                else
                {
                    throw new FileNotFoundException("Can't find file", file);
                }
            }
            else
            {
                realFileList.Add(file);
            }

        var resources = new List<BasicResource>();

        var sw = Stopwatch.StartNew();

        foreach (var file in realFileList)
        {
            Log.Information("Reading {FilePath}", file);
            var cm = new ChunkManager(Game);
            sw.Restart();

            try
            {
                cm.Read(file);
                sw.Stop();
                var fileResources = (from c in cm.Chunks
                    where c.Resource != null
                    select c.Resource).ToList();
                resources.AddRange(fileResources);
                Log.Information("Read {NumResources} resource(s) from {FilePath} in {ElapsedDurationMS}ms",
                    fileResources.Count,
                    file, sw.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
#if DEBUG
                throw;
#endif
                Log.Error(e, "Failed to read file");
            }
        }

        if (OutputDirectory != null && resources.Count > 0)
        {
            Log.Information("Exporting {NumResources} resource(s) from {NumFiles} file(s)", resources.Count,
                realFileList.Count);
            sw.Restart();
            ProcessResources(resources, OutputDirectory);
            sw.Stop();
            Log.Information("Exported in {ElapsedDurationMS}ms", sw.ElapsedMilliseconds);
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

        var texturePaths = new Dictionary<uint, string>();

        // dictionary of (key, data length)
        // the purpose of this is basically to deal with Undercover
        // being incredibly weird and having multiple versions of the
        // same texture in different resolutions
        var seenTextures = new Dictionary<uint, int>();
        // bookkeeping so we don't spam the user with annoying messages about
        // the same texture having a low-quality version skipped
        var notedSkippedTextures = new HashSet<uint>();

        var textureInfos = new Dictionary<uint, Texture>();

        // Process texture packs
        foreach (var resource in resources.OfType<TexturePack>())
        {
            foreach (var texture in resource.Textures)
            {
                var textureFileName = GetTextureFileName(texture);
                texturePaths[texture.TexHash] = Path.Combine(texturesBaseDir, textureFileName);

                if (!seenTextures.TryGetValue(texture.TexHash, out var dataSize) || dataSize < texture.Data.Length)
                {
                    seenTextures[texture.TexHash] = texture.Data.Length;
                    texture.DumpToFile(Path.Combine(texturesDir, textureFileName));
                    textureInfos[texture.TexHash] = texture;
                }
                else if (notedSkippedTextures.Add(texture.TexHash))
                {
                    Log.Information(
                        "Skipping lower quality version of texture {TexName} (hash 0x{TexKey:X8}) - further messages about this texture will be skipped",
                        texture.Name, texture.TexHash);
                }
            }
        }

        var sceneFileExtension = SceneFormat switch
        {
            SceneFormat.Collada => "dae",
            SceneFormat.Fbx => "fbx",
            _ => throw new NotImplementedException(SceneFormat.ToString())
        };

        var solidLists = resources.OfType<SolidList>().ToList();
        var scenerySections = resources.OfType<ScenerySection>().ToList();
        var lightPacks = resources.OfType<LightPack>().ToList();

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
                    ExportScenerySection(solidObjectLookup, textureInfos, scenerySection, Path.Combine(outputDir,
                            $"{scenerySection.SectionNumber}.{sceneFileExtension}"), SceneFormat, texturePaths,
                        ExportLights
                            ? lightPacks.Where(lp => lp.ScenerySectionNumber == scenerySection.SectionNumber).ToList()
                            : null);
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
                                solidKey, sceneryInfo.Name ?? "(none)");
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
                            transform = solid.PivotMatrix;
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
                    ExportSingleSolid(solid, Path.Combine(outputDir, $"{solid.Name}.{sceneFileExtension}"),
                        SceneFormat, textureInfos, texturePaths,
                        solidObjectLookup);

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
        else if (solidLists.Any() && exportMode is ModelExportMode.ExportPacks or ModelExportMode.ExportAll)
        {
            Log.Information("Processing model packs...");

            var allSolidObjects = solidLists.SelectMany(l => l.Objects).ToList();
            var morphTargetObjectHashes =
                allSolidObjects.OfType<IMorphableSolid>().SelectMany(ms => ms.MorphTargets).ToHashSet();
            var solidObjectsToExport = allSolidObjects.Where(o => !morphTargetObjectHashes.Contains(o.Hash)).ToList();

            var solidObjectLookup = allSolidObjects
                .ToLookup(o => o.Hash, o => o)
                .ToDictionary(o => o.Key, o => o.First());

            if (exportMode == ModelExportMode.ExportPacks)
            {
                Log.Information("Exporting {NumSolidLists} solid packs", solidLists.Count);

                var outputPath = Path.Combine(outputDir, $"combined.{sceneFileExtension}");
                Log.Information("Exporting all models to {OutputPath}", outputPath);
                ExportMultipleSolids(solidObjectsToExport, outputPath, SceneFormat, textureInfos, texturePaths,
                    solidObjectLookup);
            }
            else
            {
                Log.Information("Exporting individual models");
                foreach (var solidObject in solidObjectsToExport)
                {
                    ExportSingleSolid(solidObject,
                        Path.Combine(outputDir, $"{solidObject.Name}.{sceneFileExtension}"), SceneFormat, textureInfos,
                        texturePaths,
                        solidObjectLookup);
                }
            }
        }
    }

    private static void ExportScenerySection(IReadOnlyDictionary<uint, SolidObject> objects,
        Dictionary<uint, Texture> textureInfos,
        ScenerySection scenerySection, string outputPath, SceneFormat sceneFormat,
        Dictionary<uint, string> texturePaths,
        List<LightPack> lightPacks)
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

            sceneNodes.Add(new SceneExportNode(solid, info.Name ?? solid.Name, instance.Transform));
        }

        var scene = new SceneExport(sceneNodes, $"ScenerySection_{scenerySection.SectionNumber}", objects);
        ExportScene(scene, outputPath, sceneFormat, textureInfos, texturePaths, lightPacks);
    }

    private static void ValidateMorphTarget(SolidObject baseObject, SolidObject targetObject)
    {
        if (baseObject.Materials.Count != targetObject.Materials.Count)
            throw new InvalidDataException(
                $"Mesh count mismatch: base has {baseObject.Materials.Count}, target has {targetObject.Materials.Count}");
        if (baseObject.VertexSets.Count != targetObject.VertexSets.Count)
            throw new InvalidDataException(
                $"Vertex set count mismatch: base has {baseObject.VertexSets.Count}, target has {targetObject.VertexSets.Count}");
        for (var i = 0; i < baseObject.Materials.Count; i++)
        {
            if (baseObject.Materials[i].NumVerts != targetObject.Materials[i].NumVerts)
                throw new InvalidDataException(
                    $"Vertex count mismatch: base mesh {i} has {baseObject.Materials[i].NumVerts}, target mesh has {targetObject.Materials[i].NumVerts}");
            if (baseObject.Materials[i].NumIndices != targetObject.Materials[i].NumIndices)
                throw new InvalidDataException(
                    $"Index count mismatch: base mesh {i} has {baseObject.Materials[i].NumIndices}, target mesh has {targetObject.Materials[i].NumIndices}");
        }
    }

    private static void ExportMultipleSolids(IReadOnlyCollection<SolidObject> solidObjects, string outputPath,
        SceneFormat sceneFormat, Dictionary<uint, Texture> textureInfos,
        Dictionary<uint, string> texturePaths, IReadOnlyDictionary<uint, SolidObject> solidObjectLookup)
    {
        var sceneNodes = solidObjects.Select(solid => new SceneExportNode(solid, solid.Name, Matrix4x4.Identity))
            .ToList();

        foreach (var solid in solidObjects)
        {
            if (solid is not IMorphableSolid morphableSolid) continue;
            foreach (var targetHash in morphableSolid.MorphTargets)
            {
                if (!solidObjectLookup.TryGetValue(targetHash, out var targetSolid))
                {
                    Log.Warning(
                        "Solid {SolidName} references nonexistent solid [0x{TargetSolidHash:X8}] as morph target",
                        solid.Name, targetHash);
                    continue;
                }

                ValidateMorphTarget(solid, targetSolid);
                sceneNodes.Add(new SceneExportNode(targetSolid, targetSolid.Name, Matrix4x4.Identity, false));
            }
        }

        var scene = new SceneExport(sceneNodes, "MultiSolidExport", solidObjectLookup);
        ExportScene(scene, outputPath, sceneFormat, textureInfos, texturePaths, null);
    }

    private static void ExportSingleSolid(SolidObject solid, string outputPath, SceneFormat sceneFormat,
        Dictionary<uint, Texture> textureInfos,
        Dictionary<uint, string> texturePaths, IReadOnlyDictionary<uint, SolidObject> solidObjectLookup)
    {
        var sceneNodes = new List<SceneExportNode>
        {
            new(solid, solid.Name, Matrix4x4.Identity)
        };

        if (solid is IMorphableSolid morphableSolid)
            foreach (var targetHash in morphableSolid.MorphTargets)
            {
                if (!solidObjectLookup.TryGetValue(targetHash, out var targetSolid))
                {
                    Log.Warning(
                        "Solid {SolidName} references nonexistent solid [0x{TargetSolidHash:X8}] as morph target",
                        solid.Name, targetHash);
                    continue;
                }

                ValidateMorphTarget(solid, targetSolid);
                sceneNodes.Add(new SceneExportNode(targetSolid, targetSolid.Name, Matrix4x4.Identity, false));
            }

        var scene = new SceneExport(sceneNodes, solid.Name, solidObjectLookup);
        ExportScene(scene, outputPath, sceneFormat, textureInfos, texturePaths, null);
    }

    private static string GetMaterialId(SolidObject solidObject, int materialIndex, SolidObjectMaterial material)
    {
        return $"_mesh{materialIndex}_0x{solidObject.Hash:X8}";
    }

    private static string GetMaterialName(SolidObjectMaterial material)
    {
        return material.Name ?? $"TextureMTL-0x{material.TextureHash:X8}";
    }

    private static string GetTextureFileName(Texture texture)
    {
        return $"0x{texture.TexHash:X8}_{texture.Name}.dds";
    }

    private static void ExportScene(SceneExport scene, string outputPath, SceneFormat sceneFormat,
        Dictionary<uint, Texture> textureInfos,
        Dictionary<uint, string> texturePaths,
        List<LightPack> lightPacks)
    {
        switch (sceneFormat)
        {
            case SceneFormat.Fbx:
                ExportSceneFbx(scene, outputPath, textureInfos, texturePaths, lightPacks);
                break;
            case SceneFormat.Collada:
                ExportSceneCollada(scene, outputPath, texturePaths, lightPacks);
                break;
            default:
                throw new NotImplementedException(sceneFormat.ToString());
        }
    }

    private static void ExportSceneFbx(SceneExport scene, string outputPath, Dictionary<uint, Texture> textureInfos,
        Dictionary<uint, string> texturePaths,
        List<LightPack> lightPacks)
    {
        var fbxScene = new Scene();

        _ = fbxScene.CreatePredefinedTemplate(FBXClassType.Video, TemplateCreationType.NewOverrideAnyExisting);
        _ = fbxScene.CreatePredefinedTemplate(FBXClassType.Texture, TemplateCreationType.NewOverrideAnyExisting);
        _ = fbxScene.CreatePredefinedTemplate(FBXClassType.Geometry, TemplateCreationType.NewOverrideAnyExisting);
        _ = fbxScene.CreatePredefinedTemplate(FBXClassType.Material, TemplateCreationType.NewOverrideAnyExisting);
        _ = fbxScene.CreatePredefinedTemplate(FBXClassType.Model, TemplateCreationType.NewOverrideAnyExisting);
        _ = fbxScene.CreatePredefinedTemplate(FBXClassType.NodeAttribute, TemplateCreationType.NewOverrideAnyExisting);

        fbxScene.Settings.CoordAxis = 0;
        fbxScene.Settings.CoordAxisSign = 1;
        fbxScene.Settings.FrontAxis = (int)UpVector.AxisY;
        fbxScene.Settings.FrontAxisSign = 1;
        fbxScene.Settings.Name = "GlobalSettings";
        fbxScene.Settings.OriginalUnitScaleFactor = 100;
        fbxScene.Settings.OriginalUpAxis = -1;
        fbxScene.Settings.OriginalUpAxisSign = 1;
        fbxScene.Settings.TimeMode = new Enumeration((int)FrameRate.Rate60);
        fbxScene.Settings.TimeSpanStart = new TimeBase(0.0);
        fbxScene.Settings.TimeSpanStop = new TimeBase(1.0);
        fbxScene.Settings.UnitScaleFactor = 100;
        fbxScene.Settings.UpAxis = (int)UpVector.AxisZ;
        fbxScene.Settings.UpAxisSign = 1;

        var solidsToAdd = scene.Nodes.Select(n => n.SolidObject)
            .Distinct(SolidObject.HashComparer)
            .ToList();
        var texturesToAdd = solidsToAdd
            .SelectMany(s => s.Materials.Select(m => m.TextureHash))
            .Distinct()
            .ToList();
        var textureKeyToFbxTexture = new Dictionary<uint, FBXSharp.Objective.Texture>();
        var solidKeyToSolid = new Dictionary<uint, SolidObject>();

        foreach (var textureHash in texturesToAdd)
        {
            if (!texturePaths.TryGetValue(textureHash, out var texturePath))
            {
                Log.Warning("Texture 0x{TextureHash:X8} will not be linked to materials", textureHash);
                continue;
            }

            var textureBuilder = new TextureBuilder(fbxScene)
                .WithRelativePath(texturePath)
                .WithName(textureInfos[textureHash].Name);

            var texture = textureBuilder.BuildTexture();
            textureKeyToFbxTexture.Add(textureHash, texture);
        }

        var solidKeyToFbxGeometry = new Dictionary<uint, Geometry>();
        var solidMatToFbxMat = new Dictionary<(uint, int), Material>();

        foreach (var solidObject in solidsToAdd)
        {
            solidKeyToSolid.Add(solidObject.Hash, solidObject);
            var geometryBuilder = new GeometryBuilder(fbxScene).WithName(solidObject.Name)
                .WithFBXProperty("Hash", solidObject.Hash);
            var allVertices = solidObject.VertexSets.SelectMany(s => s).ToList();
            var vertexPositions = new FBXSharp.ValueTypes.Vector3[allVertices.Count];

            var numIndices = solidObject.Materials.Sum(m => m.Indices.Length);
            var allIndices = new int[numIndices];
            // var allIndices = solidObject.Materials.SelectMany(m => m.Indices.Select(i => (int)i)).ToArray();

            for (var i = 0; i < allVertices.Count; i++)
            {
                var solidMeshVertex = allVertices[i];
                vertexPositions[i] = new FBXSharp.ValueTypes.Vector3(solidMeshVertex.Position.X,
                    solidMeshVertex.Position.Y, solidMeshVertex.Position.Z);
            }

            var lastVertexSet = 0;
            var vertexOffset = 0;
            var indexOffset = 0;
            foreach (var material in solidObject.Materials)
            {
                if (material.VertexSetIndex != lastVertexSet)
                    vertexOffset += solidObject.VertexSets[lastVertexSet].Count;

                for (var i = 0; i < material.Indices.Length; i++)
                    allIndices[indexOffset + i] = material.Indices[i] + vertexOffset;

                indexOffset += material.Indices.Length;
                lastVertexSet = material.VertexSetIndex;
            }

            var subMeshStartIndex = 0;

            geometryBuilder = geometryBuilder.WithVertices(vertexPositions)
                .WithIndices(allIndices, GeometryBuilder.PolyType.Tri);

            for (var i = 0; i < solidObject.Materials.Count; i++)
            {
                var material = solidObject.Materials[i];
                var numMaterialIndices = material.Indices.Length;
                geometryBuilder = geometryBuilder.WithSubMesh(subMeshStartIndex / 3, numMaterialIndices / 3, i);
                subMeshStartIndex += numMaterialIndices;
            }

            var vertexUVs = new Vector2[allIndices.Length];
            for (var i = 0; i < allIndices.Length; i++)
            {
                var vertex = allVertices[allIndices[i]];
                vertexUVs[i] = new Vector2(vertex.TexCoords.X, 1 - vertex.TexCoords.Y);
            }

            geometryBuilder = geometryBuilder.WithUVs(vertexUVs);

            if (allVertices.All(v => v.Normal != null))
            {
                var vertexNormals = new FBXSharp.ValueTypes.Vector3[allIndices.Length];
                for (var i = 0; i < allIndices.Length; i++)
                {
                    var vertex = allVertices[allIndices[i]];
                    // ReSharper disable once PossibleInvalidOperationException
                    var normal = vertex.Normal.Value;
                    vertexNormals[i] = new FBXSharp.ValueTypes.Vector3(normal.X, normal.Y, normal.Z);
                }

                geometryBuilder = geometryBuilder.WithNormals(vertexNormals);
            }

            if (allVertices.Any(v => v.Color != null))
            {
                var vertexBaseColors = new Vector4[allIndices.Length];
                for (var i = 0; i < allIndices.Length; i++)
                {
                    var vertex = allVertices[allIndices[i]];
                    if (vertex.Color is { } color)
                    {
                        var a = (color >> 24) & 0xFF;
                        var r = (color >> 16) & 0xFF;
                        var g = (color >> 8) & 0xFF;
                        var b = (color >> 0) & 0xFF;

                        vertexBaseColors[i] = new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);
                    }
                    else
                    {
                        vertexBaseColors[i] = new Vector4(1, 1, 1);
                    }
                }

                geometryBuilder = geometryBuilder.WithColors(vertexBaseColors, 0, "BaseColors");
            }

            if (allVertices.Any(v => v.Color2 != null))
            {
                var vertexExtraColors = new Vector4[allIndices.Length];
                for (var i = 0; i < allIndices.Length; i++)
                {
                    var vertex = allVertices[allIndices[i]];
                    if (vertex.Color2 is { } color)
                    {
                        var a = (color >> 24) & 0xFF;
                        var r = (color >> 16) & 0xFF;
                        var g = (color >> 8) & 0xFF;
                        var b = (color >> 0) & 0xFF;

                        vertexExtraColors[i] = new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);
                    }
                    else
                    {
                        vertexExtraColors[i] = new Vector4(1, 1, 1);
                    }
                }

                geometryBuilder = geometryBuilder.WithColors(vertexExtraColors, 1, "ExtraColors");
            }

            // var geometry = geometryBuilder.WithUVs(vertexUVs).BuildGeometry();
            solidKeyToFbxGeometry.Add(solidObject.Hash, geometryBuilder.BuildGeometry());

            for (var iMaterial = 0; iMaterial < solidObject.Materials.Count; iMaterial++)
            {
                var material = solidObject.Materials[iMaterial];
                // var fbxMaterial = fbxScene.CreateMaterial();
                var fbxMaterialBuilder = new MaterialBuilder(fbxScene).WithName(material.Name);

                if (textureKeyToFbxTexture.TryGetValue(material.TextureHash, out var fbxTexture))
                    fbxMaterialBuilder =
                        fbxMaterialBuilder.WithChannel(MaterialBuilder.ChannelType.DiffuseColor, fbxTexture);
                else
                    fbxMaterialBuilder = fbxMaterialBuilder.WithColor(MaterialBuilder.ColorType.DiffuseColor,
                        new ColorRGB(239 / 255.0, 66 / 255.0, 245 / 255.0));

                // fbxMaterialBuilder = fbxMaterialBuilder.WithFBXProperty()
                solidMatToFbxMat[(solidObject.Hash, iMaterial)] = fbxMaterialBuilder.BuildMaterial();
            }
        }

        // create blend shapes
        foreach (var solidObject in solidsToAdd)
        {
            var geometry = solidKeyToFbxGeometry[solidObject.Hash];
            if (solidObject is IMorphableSolid morphableSolid)
                if (morphableSolid.MorphTargets.Count > 0)
                {
                    var legitMorphTargets =
                        morphableSolid.MorphTargets.Where(solidKeyToFbxGeometry.ContainsKey).ToList();
                    if (morphableSolid.MorphTargets.Count != legitMorphTargets.Count)
                        Log.Warning("Solid '{SolidName}' [0x{SolidHash:X8}] has at least one invalid morph target...",
                            solidObject.Name, solidObject.Hash);

                    var startRawVertices = ExtractPositions(solidObject);

                    foreach (var morphTarget in legitMorphTargets)
                    {
                        var morphTargetSolid = solidKeyToSolid[morphTarget];
                        var morphShape = fbxScene.CreateShape();
                        var targetIndices = TranslateIndices(morphTargetSolid);
                        var targetRawVertices = ExtractPositions(morphTargetSolid);
                        var targetRawNormals = ExtractNormals(morphTargetSolid);

                        var vertices = new FBXSharp.ValueTypes.Vector3[targetIndices.Length];
                        var normals = new FBXSharp.ValueTypes.Vector3[targetIndices.Length];

                        for (var i = 0; i < targetIndices.Length; i++)
                        {
                            var realIndex = targetIndices[i];
                            var translatedVertex = targetRawVertices[realIndex] - startRawVertices[realIndex];
                            vertices[i] = new FBXSharp.ValueTypes.Vector3(translatedVertex.X,
                                translatedVertex.Y,
                                translatedVertex.Z);
                            normals[i] = new FBXSharp.ValueTypes.Vector3(targetRawNormals[realIndex].X,
                                targetRawNormals[realIndex].Y,
                                targetRawNormals[realIndex].Z);
                        }

                        morphShape.SetupMorphTarget(targetIndices, vertices, normals);


                        var blendShape = fbxScene.CreateBlendShape();
                        var blendShapeChannel = fbxScene.CreateBlendShapeChannel();
                        blendShapeChannel.FullWeights = new double[1];
                        blendShapeChannel.DeformPercent = 0;
                        blendShapeChannel.AddShape(morphShape);

                        blendShape.AddChannel(blendShapeChannel);
                        geometry.AddBlendShape(blendShape);
                    }
                }
        }

        foreach (var sceneNode in scene.Nodes)
        {
            if (!sceneNode.IncludeInVisualScene) continue;
            var solidObject = sceneNode.SolidObject;
            var geometry = solidKeyToFbxGeometry[solidObject.Hash];

            var mesh = fbxScene.CreateMesh();
            mesh.Geometry = geometry;

            for (var i = 0; i < solidObject.Materials.Count; i++)
                mesh.AddMaterial(solidMatToFbxMat[(solidObject.Hash, i)]);

            if (!Matrix4x4.Decompose(sceneNode.Transform, out var scale, out var rotation, out var translation))
                Log.Warning(
                    "Failed to decompose matrix for node '{NodeName}' (solid '{SolidName}' [0x{SolidHash:X8}]). It may be sheared.",
                    sceneNode.Name, solidObject.Name, solidObject.Hash);

            var rotationAngles = ToEulerAngles(rotation);

            mesh.LocalScale = new FBXSharp.ValueTypes.Vector3(scale.X, scale.Y, scale.Z);
            mesh.LocalRotation = new FBXSharp.ValueTypes.Vector3(RadiansToDegrees(rotationAngles.X),
                RadiansToDegrees(rotationAngles.Y), RadiansToDegrees(rotationAngles.Z));
            mesh.LocalTranslation = new FBXSharp.ValueTypes.Vector3(translation.X, translation.Y, translation.Z);
            mesh.Name = sceneNode.Name;

            fbxScene.RootNode.AddChild(mesh);
        }

        var exporter = new FBXExporter7400(fbxScene);
        var options = new FBXExporter7400.Options
        {
            AppVendor = "NFSTools",
            AppName = "AssetDumper",
            AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
            SaveTime = DateTime.Now
        };

        using var fs = File.Create(outputPath);
        exporter.Save(fs, options);
    }

    private static Vector3[] ExtractPositions(SolidObject solidObject)
    {
        return solidObject.VertexSets.SelectMany(vs => vs).Select(v => v.Position).ToArray();
    }

    private static Vector3[] ExtractNormals(SolidObject solidObject)
    {
        return solidObject.VertexSets.SelectMany(vs => vs).Select(v =>
            v.Normal ?? throw new Exception($"Some normals are missing from solid '{solidObject.Name}'")).ToArray();
    }

    private static int[] TranslateIndices(SolidObject solidObject)
    {
        var vertexOffset = 0;
        var indexOffset = 0;
        var lastVertexSet = 0;
        var allIndices = new int[solidObject.Materials.Sum(m => m.Indices.Length)];
        foreach (var material in solidObject.Materials)
        {
            if (material.VertexSetIndex != lastVertexSet)
                vertexOffset += solidObject.VertexSets[lastVertexSet].Count;

            for (var i = 0; i < material.Indices.Length; i++)
                allIndices[indexOffset + i] = material.Indices[i] + vertexOffset;

            indexOffset += material.Indices.Length;
            lastVertexSet = material.VertexSetIndex;
        }

        return allIndices;
    }

    private static float RadiansToDegrees(float radians)
    {
        return radians * (180f / MathF.PI);
    }

    // https://stackoverflow.com/a/70462919
    private static Vector3 ToEulerAngles(Quaternion q)
    {
        Vector3 angles = new();

        // roll / x
        double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
        double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
        angles.X = (float)Math.Atan2(sinr_cosp, cosr_cosp);

        // pitch / y
        double sinp = 2 * (q.W * q.Y - q.Z * q.X);
        if (Math.Abs(sinp) >= 1)
            angles.Y = (float)Math.CopySign(Math.PI / 2, sinp);
        else
            angles.Y = (float)Math.Asin(sinp);

        // yaw / z
        double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
        double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
        angles.Z = (float)Math.Atan2(siny_cosp, cosy_cosp);

        return angles;
    }

    private static void ExportSceneCollada(SceneExport scene, string outputPath, Dictionary<uint, string> texturePaths,
        List<LightPack> lightPacks)
    {
        var collada = new COLLADA
        {
            version = VersionType.Item141
        };

        var solidsToAdd = scene.Nodes.Select(n => n.SolidObject)
            .Distinct(SolidObject.HashComparer)
            .ToList();
        var texturesToAdd = solidsToAdd
            .SelectMany(s => s.Materials.Select(m => m.TextureHash))
            .Distinct()
            .ToList();

        var images = new library_images();
        var imageList = new List<image>();
        var materials = new library_materials();
        var effects = new library_effects();
        var effectList = new List<effect>();

        foreach (var textureId in texturesToAdd)
            if (texturePaths.TryGetValue(textureId, out var texturePath))
                imageList.Add(new image
                {
                    id = $"texture-0x{textureId:X8}-img",
                    name = $"TextureIMG-0x{textureId:X8}",
                    Item = texturePath,
                    depth = 1
                });

        foreach (var solid in solidsToAdd)
            for (var materialIndex = 0; materialIndex < solid.Materials.Count; materialIndex++)
            {
                var material = solid.Materials[materialIndex];
                var textureId = material.TextureHash;
                var effectIdBase = GetMaterialEffectId(solid, materialIndex);
                effectList.Add(new effect
                {
                    id = effectIdBase,
                    // name = $"TextureFX-0x{textureId:X8}",
                    Items = new[]
                    {
                        new effectFx_profile_abstractProfile_COMMON
                        {
                            Items = new object[]
                            {
                                new common_newparam_type
                                {
                                    sid = $"{effectIdBase}-surface",
                                    Item = new fx_surface_common
                                    {
                                        type = fx_surface_type_enum.Item2D,
                                        init_from = new[]
                                        {
                                            new fx_surface_init_from_common { Value = $"texture-0x{textureId:X8}-img" }
                                        }
                                    },
                                    ItemElementName = ItemChoiceType.surface
                                },
                                new common_newparam_type
                                {
                                    sid = $"{effectIdBase}-sampler",
                                    Item = new fx_sampler2D_common
                                    {
                                        source = $"{effectIdBase}-surface",
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
                                            texture = $"{effectIdBase}-sampler",
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
            from pair in solidObject.Materials.Select((mat, idx) => new { idx, mat })
            let materialId = GetMaterialId(solidObject, pair.idx, pair.mat)
            let materialName = GetMaterialName(pair.mat)
            select new material
            {
                id = materialId,
                name = materialName,
                instance_effect = new instance_effect { url = $"#{GetMaterialEffectId(solidObject, pair.idx)}" }
            }).ToArray();
        effects.effect = effectList.ToArray();

        // Build geometry library

        var geometries = new library_geometries();
        var geometryList = new List<geometry>();
        var geometryIds = new Dictionary<uint, string>();

        foreach (var solidObject in solidsToAdd)
        {
            var geometryId = $"geometry-0x{solidObject.Hash:X8}";
            geometryList.Add(SolidToGeometry(solidObject, geometryId));
            geometryIds.Add(solidObject.Hash, geometryId);
        }

        geometries.geometry = geometryList.ToArray();

        var visualScenes = new library_visual_scenes();

        var sceneNodes = new List<node>();

        for (var idx = 0; idx < scene.Nodes.Count; idx++)
        {
            var node = scene.Nodes[idx];
            if (!node.IncludeInVisualScene) continue;
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
                                    target = $"#{GetMaterialId(node.SolidObject, materialIdx, material)}",
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

        var controllers = new library_controllers();
        var controllerList = new List<controller>();

        foreach (var solidObject in solidsToAdd)
        {
            if (solidObject is not IMorphableSolid morphableSolid) continue;

            if (morphableSolid.MorphTargets.Count > 0)
            {
                var legitMorphTargets = morphableSolid.MorphTargets.Where(mt => geometryIds.ContainsKey(mt)).ToList();

                var morph = new morph();
                var baseSolidGeometryId = geometryIds[solidObject.Hash];
                morph.source1 = $"#{baseSolidGeometryId}";
                morph.method = MorphMethodType.RELATIVE;

                var morphTargetsSource = new source
                {
                    id = $"{baseSolidGeometryId}-targets"
                };
                morphTargetsSource.Item = new IDREF_array
                {
                    id = $"{morphTargetsSource.id}-array",
                    count = (ulong)legitMorphTargets.Count,
                    Value = string.Join(" ", legitMorphTargets.Select(targetHash => geometryIds[targetHash]))
                };
                morphTargetsSource.technique_common = new sourceTechnique_common
                {
                    accessor = new accessor
                    {
                        source = $"#{morphTargetsSource.id}-array",
                        count = (ulong)legitMorphTargets.Count,
                        stride = 1,
                        param = new[]
                        {
                            new param { name = "IDREF", type = "IDREF" }
                        }
                    }
                };

                var morphWeightsSource = new source();
                morphWeightsSource.id = $"{baseSolidGeometryId}-weights";
                morphWeightsSource.Item = new float_array
                {
                    id = $"{morphWeightsSource.id}-array",
                    count = (ulong)legitMorphTargets.Count,
                    Values = new double[legitMorphTargets.Count]
                };
                morphWeightsSource.technique_common = new sourceTechnique_common
                {
                    accessor = new accessor
                    {
                        source = $"#{morphWeightsSource.id}-array",
                        count = (ulong)legitMorphTargets.Count,
                        stride = 1,
                        param = new[]
                        {
                            new param { name = "MORPH_WEIGHT", type = "float" }
                        }
                    }
                };

                morph.source = new[]
                {
                    morphTargetsSource,
                    morphWeightsSource
                };

                morph.targets = new morphTargets
                {
                    input = new[]
                    {
                        new InputLocal { semantic = "MORPH_TARGET", source = $"#{morphTargetsSource.id}" },
                        new InputLocal { semantic = "MORPH_WEIGHT", source = $"#{morphWeightsSource.id}" }
                    }
                };

                var controllerName = $"{baseSolidGeometryId}_morph";
                var controller = new controller
                {
                    id = controllerName,
                    name = controllerName,
                    Item = morph
                };

                controllerList.Add(controller);
            }
        }

        controllers.controller = controllerList.ToArray();

        var lights = new library_lights();
        var lightList = new List<light>();

        if (lightPacks != null)
            foreach (var lightPack in lightPacks)
                for (var index = 0; index < lightPack.Lights.Count; index++)
                {
                    var light = lightPack.Lights[index];
                    var exportLight = new light();
                    exportLight.id = $"light_{lightPack.ScenerySectionNumber}_{index}";
                    exportLight.name = light.Name;

                    var color = light.Color;
                    var a = (color >> 24) & 0xFF;
                    var b = (float)((color >> 16) & 0xFF) / 255;
                    var g = (float)((color >> 8) & 0xFF) / 255;
                    var r = (float)((color >> 0) & 0xFF) / 255;

                    exportLight.technique_common = new lightTechnique_common
                    {
                        Item = new lightTechnique_commonPoint
                        {
                            color = new TargetableFloat3
                            {
                                Values = new double[] { r, g, b }
                            }
                        }
                    };

                    var extraRadiusElement = new XmlDocument().CreateElement("radius");
                    extraRadiusElement.InnerText = light.Size.ToString(CultureInfo.InvariantCulture);
                    var extraEnergyElement = new XmlDocument().CreateElement("energy");
                    // This is insanely stupid, but Blender's "power" system is also stupid.
                    // 100,000W is a good enough maximum, probably...
                    extraEnergyElement.InnerText =
                        (100_000.0f * light.Intensity).ToString(CultureInfo.InvariantCulture);
                    var extraRedElement = new XmlDocument().CreateElement("red");
                    extraRedElement.InnerText = r.ToString(CultureInfo.InvariantCulture);
                    var extraGreenElement = new XmlDocument().CreateElement("green");
                    extraGreenElement.InnerText = g.ToString(CultureInfo.InvariantCulture);
                    var extraBlueElement = new XmlDocument().CreateElement("blue");
                    extraBlueElement.InnerText = b.ToString(CultureInfo.InvariantCulture);
                    exportLight.extra = new[]
                    {
                        new extra
                        {
                            technique = new[]
                            {
                                new technique
                                {
                                    profile = "blender",
                                    Any = new[]
                                    {
                                        extraRadiusElement,
                                        extraEnergyElement,
                                        extraRedElement,
                                        extraGreenElement,
                                        extraBlueElement
                                    }
                                }
                            }
                        }
                    };

                    lightList.Add(exportLight);

                    // add node
                    var lightMatrix = Matrix4x4.CreateTranslation(light.Position);
                    var lightNode = new node();
                    lightNode.id = $"{scene.SceneName}_inst_{exportLight.id}";
                    lightNode.name = exportLight.name;
                    lightNode.instance_light = new[]
                    {
                        new InstanceWithExtra
                        {
                            url = $"#{exportLight.id}"
                        }
                    };
                    lightNode.Items = new object[]
                    {
                        new matrix
                        {
                            Values = new double[]
                            {
                                lightMatrix.M11,
                                lightMatrix.M21,
                                lightMatrix.M31,
                                lightMatrix.M41,

                                lightMatrix.M12,
                                lightMatrix.M22,
                                lightMatrix.M32,
                                lightMatrix.M42,

                                lightMatrix.M13,
                                lightMatrix.M23,
                                lightMatrix.M33,
                                lightMatrix.M43,

                                lightMatrix.M14,
                                lightMatrix.M24,
                                lightMatrix.M34,
                                lightMatrix.M44
                            }
                        }
                    };
                    lightNode.ItemsElementName = new[]
                    {
                        ItemsChoiceType2.matrix
                    };
                    sceneNodes.Add(lightNode);
                }

        lights.light = lightList.ToArray();

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
            controllers,
            lights,
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

    private static string GetMaterialEffectId(SolidObject solid, int materialIndex)
    {
        return $"solid-{solid.Hash:X8}-mat{materialIndex}-fx";
    }

    private static geometry SolidToGeometry(SolidObject solidObject, string geometryId)
    {
        var mesh = new mesh();
        var sources = new List<source>();

        var allVertices = new List<SolidMeshVertex>();

        foreach (var vertexSet in solidObject.VertexSets) allVertices.AddRange(vertexSet);

        var positionsName = $"{geometryId}_positions";
        var positionSrcId = $"{positionsName}_src";
        var positionsDataId = $"{positionSrcId}_data";

        sources.Add(new source
        {
            name = positionsName,
            id = positionSrcId,
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

        var uvSrcName = $"{geometryId}_uv";
        var uvSrcId = $"{uvSrcName}_src";
        var uvDataId = $"{uvSrcId}_data";

        sources.Add(new source
        {
            name = $"texcoords",
            id = uvSrcId,
            Item = new float_array
            {
                Values = allVertices
                    .SelectMany(v => new double[] { v.TexCoords.X, 1 - v.TexCoords.Y }).ToArray(),
                id = uvDataId,
                count = (ulong)(allVertices.Count * 2)
            },
            technique_common = new sourceTechnique_common
            {
                accessor = new accessor
                {
                    count = (ulong)allVertices.Count,
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
        });

        var colorSrcIds = new List<string>();

        {
            var colorsName = $"{geometryId}_color";
            var colorSrcId = $"{colorsName}_src";
            var colorsDataId = $"{colorsName}_data";

            sources.Add(new source
            {
                name = "color",
                id = colorSrcId,
                Item = new float_array
                {
                    Values = allVertices
                        .SelectMany(v =>
                        {
                            var color = v.Color ?? 0xFFFFFFFF;

                            var a = (color >> 24) & 0xFF;
                            var r = (color >> 16) & 0xFF;
                            var g = (color >> 8) & 0xFF;
                            var b = (color >> 0) & 0xFF;

                            return new double[] { r / 255f, g / 255f, b / 255f, a / 255f };
                        }).ToArray(),
                    id = colorsDataId,
                    count = (ulong)(allVertices.Count * 4)
                },
                technique_common = new sourceTechnique_common
                {
                    accessor = new accessor
                    {
                        count = (ulong)allVertices.Count,
                        offset = 0,
                        source = $"#{colorsDataId}",
                        stride = 4,
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
                            new param
                            {
                                name = "A",
                                type = "float"
                            }
                        }
                    }
                }
            });

            colorSrcIds.Add(colorSrcId);

            if (allVertices.Any(v => v.Color2 != null))
            {
                var color2Name = $"{geometryId}_color2";
                var color2SrcId = $"{color2Name}_src";
                var color2DataId = $"{color2Name}_data";
                sources.Add(new source
                {
                    name = "color2",
                    id = color2SrcId,
                    Item = new float_array
                    {
                        Values = allVertices
                            .SelectMany(v =>
                            {
                                var color = v.Color2 ?? 0xFFFFFFFF;

                                var a = (color >> 24) & 0xFF;
                                var r = (color >> 16) & 0xFF;
                                var g = (color >> 8) & 0xFF;
                                var b = (color >> 0) & 0xFF;

                                return new double[] { r / 255f, g / 255f, b / 255f, a / 255f };
                            }).ToArray(),
                        id = color2DataId,
                        count = (ulong)(allVertices.Count * 4)
                    },
                    technique_common = new sourceTechnique_common
                    {
                        accessor = new accessor
                        {
                            count = (ulong)allVertices.Count,
                            offset = 0,
                            source = $"#{color2DataId}",
                            stride = 4,
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
                                new param
                                {
                                    name = "A",
                                    type = "float"
                                }
                            }
                        }
                    }
                });
                colorSrcIds.Add(color2SrcId);
            }
        }

        var normalsSrcIds = new string[solidObject.VertexSets.Count];

        for (var index = 0; index < solidObject.VertexSets.Count; index++)
        {
            var vertexSet = solidObject.VertexSets[index];
            if (!vertexSet.All(v => v.Normal.HasValue)) continue;

            var normalSrcName = normalsSrcIds[index] = $"{geometryId}_normal{index}";
            var normalDataId = $"{normalSrcName}_data";

            sources.Add(new source
            {
                id = normalSrcName,
                Item = new float_array
                {
                    Values = vertexSet
                        .SelectMany(v =>
                        {
                            var normal = v.Normal ?? throw new Exception("impossible");
                            return new double[] { normal.X, normal.Y, normal.Z };
                        }).ToArray(),
                    id = normalDataId,
                    count = (ulong)(allVertices.Count * 3)
                },
                technique_common = new sourceTechnique_common
                {
                    accessor = new accessor
                    {
                        count = (ulong)vertexSet.Count,
                        offset = 0,
                        source = $"#{normalDataId}",
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
                            }
                        }
                    }
                }
            });
        }

        mesh.source = sources.ToArray();

        var vertexSrcId = $"{geometryId}_vertices";

        mesh.vertices = new vertices
        {
            input = new[]
            {
                new InputLocal
                {
                    semantic = "POSITION",
                    source = $"#{positionSrcId}"
                }
            },
            id = vertexSrcId
        };

        var items = new List<object>();
        var vertexOffset = 0;
        var lastVertexSet = 0;

        for (var materialIndex = 0; materialIndex < solidObject.Materials.Count; materialIndex++)
        {
            var material = solidObject.Materials[materialIndex];
            var faces = new List<ushort[]>();

            if (material.VertexSetIndex != lastVertexSet) vertexOffset += solidObject.VertexSets[lastVertexSet].Count;

            var hasNormals = normalsSrcIds[material.VertexSetIndex] != null;

            for (var i = 0; i < material.Indices.Length; i += 3)
            {
                var idx1 = material.Indices[i];
                var idx2 = material.Indices[i + 1];
                var idx3 = material.Indices[i + 2];

                faces.Add(new[] { idx1, idx2, idx3 });
            }

            var inputs = new List<InputLocalOffset>
            {
                new()
                {
                    semantic = "VERTEX", source = $"#{vertexSrcId}"
                },
                new()
                {
                    semantic = "TEXCOORD", source = $"#{uvSrcId}"
                }
            };
            inputs.AddRange(colorSrcIds.Select(colorSrcId => new InputLocalOffset
                { semantic = "COLOR", source = $"#{colorSrcId}" }));

            if (hasNormals)
                inputs.Add(new InputLocalOffset
                {
                    semantic = "NORMAL",
                    source = $"#{normalsSrcIds[material.VertexSetIndex]}",
                    offset = 1
                });

            var indexList = new List<ushort>();

            foreach (var index in faces.SelectMany(face => face))
            {
                indexList.Add((ushort)(vertexOffset + index));
                if (hasNormals)
                    indexList.Add(index);
            }

            items.Add(new triangles
            {
                count = (ulong)faces.Count,
                input = inputs.ToArray(),
                material = $"material{materialIndex}",
                p = string.Join(" ", indexList)
            });

            lastVertexSet = material.VertexSetIndex;
        }

        mesh.Items = items.ToArray();

        return new geometry
        {
            name = solidObject.Name,
            id = geometryId,
            Item = mesh
        };
    }

    private class SceneExport
    {
        public SceneExport(List<SceneExportNode> nodes, string sceneName,
            IReadOnlyDictionary<uint, SolidObject> solidObjects)
        {
            Nodes = nodes;
            SceneName = sceneName;
            SolidObjects = solidObjects;
        }

        public List<SceneExportNode> Nodes { get; }
        public string SceneName { get; }
        public IReadOnlyDictionary<uint, SolidObject> SolidObjects { get; }
    }
}

internal class SceneExportNode
{
    public SceneExportNode(SolidObject solidObject, string name, Matrix4x4 transform, bool includeInVisualScene = true)
    {
        SolidObject = solidObject;
        Name = name;
        Transform = transform;
        IncludeInVisualScene = includeInVisualScene;
    }

    public SolidObject SolidObject { get; }
    public string Name { get; }
    public Matrix4x4 Transform { get; }
    public bool IncludeInVisualScene { get; }
}