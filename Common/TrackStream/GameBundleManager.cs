using System.Collections.Generic;
using Common.TrackStream.Data;

namespace Common.TrackStream
{
    public abstract class GameBundleManager
    {
        public List<LocationBundle> Bundles { get; } = new List<LocationBundle>();

        public abstract void ReadFrom(string gameDirectory);

        public abstract void WriteLocationBundle(string outPath, LocationBundle bundle, List<StreamSection> sections);

        public abstract void ExtractBundleSections(LocationBundle bundle, string outDirectory);

        public abstract void CombineSections(List<StreamSection> sections, string outFile);

        public abstract LocationBundle ReadLocationBundle(string bundlePath);
    }
}
