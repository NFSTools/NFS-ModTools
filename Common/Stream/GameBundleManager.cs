using System.Collections.Generic;
using Common.Stream.Data;

namespace Common.Stream
{
    public abstract class GameBundleManager
    {
        public List<LocationBundle> Bundles { get; } = new List<LocationBundle>();

        public abstract void ReadFrom(string gameDirectory);

        public abstract void WriteLocationBundle(string outPath, LocationBundle bundle, string sectionsPath);

        public abstract void ExtractBundleSections(LocationBundle bundle, string outDirectory);

        public abstract void CombineSections(List<StreamSection> sections, string outFile);

        protected abstract LocationBundle ReadLocationBundle(string bundlePath);
    }
}
