using System;
using System.Collections.Generic;
using System.Linq;
using Common.Geometry;

namespace AssetDumper
{
    public enum UndercoverLayerCombo
    {
        Unknown,
        Single,
        SingleWithDataMap,
        TwoLayerWithDataMap,
        TerrainBlend,
    }

    /// <summary>
    /// Single shared source of truth for how an Undercover material's real
    /// diffuse hash gets resolved, how its structural layer combination gets
    /// classified, and how its mergeable export name gets built. Used by both
    /// ExportSceneCollada (what actually gets bound/named in the .dae) and
    /// SidecarManifestBuilder (what the manifest is keyed by) so the two can
    /// never disagree on the same material - previously these were two
    /// separate, drifting implementations of the same ideas.
    /// </summary>
    public static class UndercoverLayerClassifier
    {
        private static readonly string[] DiffuseExceptionPrefixes = { "TRN_TRANSPARENT_" };

        public static bool IsExcepted(string name) =>
            name != null && DiffuseExceptionPrefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Pick (1) the blend-mask texture if present, else (2) the first real
        /// diffuse-suffixed texture (skipping excepted name families), else
        /// (3) fall back to the raw Diffuse slot. This is the exact rule
        /// ExportSceneCollada already used inline - moved here so
        /// SidecarManifestBuilder resolves the identical hash for the
        /// identical material, rather than a second, possibly-differing copy.
        /// </summary>
        public static uint ResolveDiffuseHash(UndercoverMaterial material, Func<uint, string> resolveTextureName)
        {
            foreach (var hash in material.MaterialTextureHashes ?? Array.Empty<uint>())
            {
                var name = resolveTextureName(hash);
                if (name != null && !IsExcepted(name) && name.EndsWith("_B", StringComparison.OrdinalIgnoreCase))
                    return hash;
            }

            foreach (var hash in material.MaterialTextureHashes ?? Array.Empty<uint>())
            {
                var name = resolveTextureName(hash);
                if (name != null && !IsExcepted(name) && name.EndsWith("_D", StringComparison.OrdinalIgnoreCase))
                    return hash;
            }

            return material.DiffuseTextureHash;
        }

        public static UndercoverLayerCombo Classify(UndercoverMaterial material, Func<uint, string> resolveTextureName)
        {
            var named = (material.MaterialTextureHashes ?? Array.Empty<uint>())
                .Select(resolveTextureName)
                .Where(n => n != null && !IsExcepted(n))
                .ToList();

            var hasBlendMask = named.Any(n => n.EndsWith("_B", StringComparison.OrdinalIgnoreCase));
            var diffuseCount = named.Count(n => n.EndsWith("_D", StringComparison.OrdinalIgnoreCase));
            var hasDataMap = named.Any(n => n.EndsWith("_M", StringComparison.OrdinalIgnoreCase));

            if (hasBlendMask && diffuseCount >= 2) return UndercoverLayerCombo.TerrainBlend;
            if (diffuseCount >= 2 && hasDataMap) return UndercoverLayerCombo.TwoLayerWithDataMap;
            if (diffuseCount == 1 && hasDataMap) return UndercoverLayerCombo.SingleWithDataMap;
            if (diffuseCount == 1) return UndercoverLayerCombo.Single;
            return UndercoverLayerCombo.Unknown;
        }

        public static string ToTag(UndercoverLayerCombo combo) => combo switch
        {
            UndercoverLayerCombo.Single => "single",
            UndercoverLayerCombo.SingleWithDataMap => "single_M",
            UndercoverLayerCombo.TwoLayerWithDataMap => "layer_M",
            UndercoverLayerCombo.TerrainBlend => "terrain_B",
            _ => "unknown",
        };

        /// <summary>
        /// The one place the final mergeable export name gets built - called
        /// identically from ExportSceneCollada (for the .dae) and
        /// SidecarManifestBuilder (for the manifest key), so they can never
        /// produce different names for the same material. Falls back to
        /// fallbackName (the pre-existing GetMaterialName behavior) only if
        /// no diffuse could be resolved at all.
        /// </summary>
        public static string BuildExportName(UndercoverMaterial material, Func<uint, string> resolveTextureName,
            string effectName, string fallbackName)
        {
            var diffuseHash = ResolveDiffuseHash(material, resolveTextureName);
            var baseName = resolveTextureName(diffuseHash);
            if (string.IsNullOrEmpty(baseName) || baseName == "-")
                return fallbackName;

            var comboTag = ToTag(Classify(material, resolveTextureName));
            return $"{baseName}_{effectName}_{comboTag}";
        }
    }
}
