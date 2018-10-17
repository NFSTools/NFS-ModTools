using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarCompiler
{
    public abstract class CompilerBase
    {
        public abstract void CompileGeometry(string file, CollectedData collectedData);

        public abstract void CompileTextures(string file, CollectedData collectedData);
    }
}
