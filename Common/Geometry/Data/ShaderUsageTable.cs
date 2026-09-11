using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Common
{
    /// <summary>
    /// Resolves a material's raw shader-identity hash (Undercover's MaterialAttribKey, aka the
    /// "vault shader usage name" hash) back to its human-readable shader name (e.g.
    /// "rd_asphalt_reflective"), using the embedded shaderusages reference data harvested from UCGT.
    ///
    /// This is a finer-grained identifier than the existing EffectId/UndercoverEffectId mapping:
    /// many distinct named shaders can share one coarse EffectId technique, so this table is not
    /// expected to have 1:1 coverage against EffectIdMapping - it resolves the specific shader name,
    /// not the broad rendering technique.
    /// </summary>
    public static class ShaderUsageTable
    {
        private const string CurrentGenResourceName = "Common.data.shaderusages.txt";

        private static readonly Lazy<Dictionary<uint, string>> HashToName =
            new(() => Build(CurrentGenResourceName));

        /// <summary>
        /// Resolves a shader-identity hash to its readable name, or null if the hash isn't
        /// present in the embedded shaderusages table (either because it's a legacy-only shader,
        /// a car shader not covered by the world-material vocabulary, or genuinely unknown).
        /// </summary>
        public static string Resolve(uint hash)
        {
            return HashToName.Value.TryGetValue(hash, out var name) ? name : null;
        }

        /// <summary>
        /// Same as <see cref="Resolve"/>, but returns a readable placeholder instead of null when
        /// the hash isn't recognized - convenient for logging/dump output where you always want a
        /// printable string.
        /// </summary>
        public static string ResolveOrPlaceholder(uint hash)
        {
            return Resolve(hash) ?? $"unknown(0x{hash:X8})";
        }

        private static Dictionary<uint, string> Build(string embeddedResourceName)
        {
            var result = new Dictionary<uint, string>();

            foreach (var line in ReadEmbeddedLines(embeddedResourceName))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("#"))
                    continue;

                // The vault shader usage name is always the first whitespace-delimited token on
                // the line, whether it's a readable name ("rd_asphalt_reflective") or a raw hash
                // used as a placeholder name where the real string isn't known ("0x01747d6f").
                var name = trimmed
                    .Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();

                if (string.IsNullOrEmpty(name))
                    continue;

                // Skip the raw-hash placeholder entries themselves - they're not a name we could
                // ever re-derive by hashing, they're already the hash. Everything else gets hashed
                // and added to the reverse lookup.
                if (name.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Last one wins on a hash collision; shouldn't happen in practice, but this way a
                // collision just means "some name wins" rather than a crash.
                result[Hasher.BinHash(name)] = name;
            }

            return result;
        }

        private static IEnumerable<string> ReadEmbeddedLines(string resourceName)
        {
            var assembly = typeof(ShaderUsageTable).Assembly;

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                var available = string.Join(", ", assembly.GetManifestResourceNames());
                throw new InvalidOperationException(
                    $"Embedded resource '{resourceName}' was not found in assembly " +
                    $"'{assembly.GetName().Name}'. Available embedded resources: {available}. " +
                    "Check that shaderusages.txt is marked as an EmbeddedResource in Common.csproj " +
                    "and that its folder path matches the resource name (RootNamespace + folder " +
                    "path with dots instead of slashes).");
            }

            using var reader = new StreamReader(stream);
            string line;
            while ((line = reader.ReadLine()) != null)
                yield return line;
        }
    }
}