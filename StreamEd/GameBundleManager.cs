using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StreamEd.Data;

namespace StreamEd
{
    public abstract class GameBundleManager
    {
        public List<LocationBundle> Bundles { get; } = new List<LocationBundle>();

        public abstract void ReadFrom(string gameDirectory);

        public abstract void WriteLocationBundle(string path, LocationBundle bundle);

        public abstract void ExtractBundleSections(LocationBundle bundle, string outDirectory);

        public abstract void CombineSections(List<StreamSection> sections, string outFile);

        protected abstract LocationBundle ReadLocationBundle(string bundlePath);
    }
}
