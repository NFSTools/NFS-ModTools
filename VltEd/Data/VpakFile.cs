using System;
using VltEd.Structs;

namespace VltEd.Data
{
    public class VpakFile
    {
        /// <summary>
        /// The internal header structure.
        /// </summary>
        public VpakFileHeader FileHeader { get; }

        /// <summary>
        /// The name of the file.
        /// </summary>
        public string Name { get; set; }

        public VpakFile(VpakFileHeader fileHeader, string name)
        {
            FileHeader = fileHeader;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
